using ReclamationService.Application.DTOs;
using ReclamationService.Application.Exceptions;
using ReclamationService.Application.Security;
using ReclamationService.Domain.Entities;
using ReclamationService.Domain.Enums;

namespace ReclamationService.Application.Services;

public partial class ReclamationsService
{
    public async Task<ReclamationDto> ClaimAsync(long id, CurrentUser actor, CancellationToken cancellationToken = default)
    {
        EnsureRole(actor, "SAV", "ADMIN");

        var role = NormalizeRole(actor.Role);
        var savName = GetActorDisplayName(actor);
        var claimedAt = DateTime.UtcNow;

        var affected = await _reclamationRepository.ClaimIfAvailableAsync(id, actor.UserId, savName, claimedAt, cancellationToken);
        if (affected == 0)
        {
            var current = _reclamationRepository.GetById(id)
                ?? throw new NotFoundException($"Reclamation with id {id} not found.");

            if (current.Status is ReclamationStatus.Closed or ReclamationStatus.Cancelled or ReclamationStatus.Rejected)
            {
                throw new ConflictException("This reclamation cannot be taken because it is already closed, cancelled, or rejected.");
            }

            if (current.ClaimedBySavId == actor.UserId)
            {
                return ToDtoWithActions(current, actor);
            }

            if (current.ClaimedBySavId.HasValue)
            {
                throw new ConflictException($"This reclamation is already taken by {current.ClaimedBySavName ?? $"SAV#{current.ClaimedBySavId.Value}"}.");
            }

            throw new ConflictException("This reclamation could not be taken. Refresh and try again.");
        }

        var updated = GetByIdInternal(id);
        AlignLegacySavAssignmentWhenNeeded(updated, actor.UserId, savName, claimedAt);

        _historyRepository.Add(new ReclamationHistory
        {
            ReclamationId = updated.Id,
            FromStatus = updated.Status,
            ToStatus = updated.Status,
            ActorUserId = actor.UserId,
            ActorRole = role,
            Comment = $"Reclamation taken by SAV {savName}.",
            OccurredAt = claimedAt
        });

        return ToDtoWithActions(updated, actor);
    }

    public Task<ReclamationDto> ReleaseAsync(long id, CurrentUser actor, CancellationToken cancellationToken = default)
    {
        EnsureRole(actor, "SAV", "ADMIN");
        var reclamation = GetByIdVisible(id, actor);
        var role = NormalizeRole(actor.Role);

        if (role == "SAV" && reclamation.ClaimedBySavId != actor.UserId)
        {
            throw new UnauthorizedAccessException();
        }

        var releasedBy = GetActorDisplayName(actor);
        reclamation.ClaimedBySavId = null;
        reclamation.ClaimedBySavName = null;
        reclamation.ClaimedAt = null;
        reclamation.ReleasedAt = DateTime.UtcNow;
        reclamation.UpdatedAt = DateTime.UtcNow;

        var updated = _reclamationRepository.Update(reclamation);
        _historyRepository.Add(new ReclamationHistory
        {
            ReclamationId = updated.Id,
            FromStatus = updated.Status,
            ToStatus = updated.Status,
            ActorUserId = actor.UserId,
            ActorRole = role,
            Comment = $"Reclamation ownership released by {releasedBy}.",
            OccurredAt = DateTime.UtcNow
        });

        return Task.FromResult(ToDtoWithActions(updated, actor));
    }

    public Task<ReclamationDto> ReassignSavAsync(long id, ReassignSavDto dto, CurrentUser actor, CancellationToken cancellationToken = default)
    {
        EnsureRole(actor, "ADMIN");
        var target = _serviceUserRepository.GetById(dto.SavUserId);
        if (target is null || NormalizeRole(target.Role) != "SAV")
        {
            throw new BadRequestException("Target user must exist and have the SAV role.");
        }

        var reclamation = GetByIdInternal(id);
        var oldSav = reclamation.ClaimedBySavName
            ?? reclamation.SAVName
            ?? (reclamation.ClaimedBySavId.HasValue ? $"SAV#{reclamation.ClaimedBySavId.Value}" : "unassigned");
        var newSavName = string.IsNullOrWhiteSpace(dto.SavUserName)
            ? target.FullName
            : dto.SavUserName.Trim();
        if (string.IsNullOrWhiteSpace(newSavName))
        {
            newSavName = target.Email;
        }

        reclamation.ClaimedBySavId = target.Id;
        reclamation.ClaimedBySavName = newSavName;
        reclamation.ClaimedAt = DateTime.UtcNow;
        reclamation.ReleasedAt = null;
        reclamation.UpdatedAt = DateTime.UtcNow;

        if (reclamation.Status != ReclamationStatus.Open || reclamation.SAVId.HasValue)
        {
            reclamation.SAVId = target.Id;
            reclamation.SAVName = newSavName;
            reclamation.AssignedAt ??= DateTime.UtcNow;
        }

        var updated = _reclamationRepository.Update(reclamation);
        _historyRepository.Add(new ReclamationHistory
        {
            ReclamationId = updated.Id,
            FromStatus = updated.Status,
            ToStatus = updated.Status,
            ActorUserId = actor.UserId,
            ActorRole = NormalizeRole(actor.Role),
            Comment = $"Reclamation reassigned from {oldSav} to {newSavName} by admin {GetActorDisplayName(actor)}.",
            OccurredAt = DateTime.UtcNow
        });

        return Task.FromResult(ToDtoWithActions(updated, actor));
    }

    public void EnsureCanWorkOnReclamation(long id, CurrentUser actor)
    {
        var reclamation = GetByIdVisible(id, actor);
        EnsureCanWork(actor, reclamation);
    }

    private void EnsureCanWork(CurrentUser actor, Reclamation reclamation)
    {
        var role = NormalizeRole(actor.Role);
        if (role == "ADMIN")
        {
            return;
        }

        if (role == "SAV" && reclamation.ClaimedBySavId == actor.UserId)
        {
            return;
        }

        throw new UnauthorizedAccessException();
    }

    private static string GetActorDisplayName(CurrentUser actor)
    {
        if (!string.IsNullOrWhiteSpace(actor.FullName))
        {
            return actor.FullName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(actor.Email))
        {
            return actor.Email.Trim();
        }

        return $"User#{actor.UserId}";
    }

    private Reclamation AlignLegacySavAssignmentWhenNeeded(Reclamation reclamation, long savId, string savName, DateTime assignedAt)
    {
        if (reclamation.Status == ReclamationStatus.Open && !reclamation.SAVId.HasValue)
        {
            return reclamation;
        }

        if (reclamation.SAVId == savId && string.Equals(reclamation.SAVName, savName, StringComparison.Ordinal))
        {
            return reclamation;
        }

        reclamation.SAVId = savId;
        reclamation.SAVName = savName;
        reclamation.AssignedAt ??= assignedAt;
        reclamation.UpdatedAt = DateTime.UtcNow;
        return _reclamationRepository.Update(reclamation);
    }
}
