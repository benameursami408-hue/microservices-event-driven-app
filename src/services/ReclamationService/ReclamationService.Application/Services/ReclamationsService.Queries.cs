using ReclamationService.Application.Outbox;
using ReclamationService.Application.DTOs;
using ReclamationService.Application.Exceptions;
using ReclamationService.Application.Mappers;
using ReclamationService.Application.Security;
using ReclamationService.Domain.Entities;
using ReclamationService.Domain.Enums;
using ReclamationService.Domain.Interfaces;
using SharedEvents.Events;
using System.Text.Json;

namespace ReclamationService.Application.Services;

public partial class ReclamationsService
{
    public List<ReclamationDto> GetVisible(
        CurrentUser actor,
        ReclamationStatus? status = null,
        TicketCategory? category = null)
    {
        var role = NormalizeRole(actor.Role);
        List<Reclamation> items;

        if (role == "ADMIN")
        {
            items = status.HasValue
                ? _reclamationRepository.GetByStatus(status.Value)
                : _reclamationRepository.GetAll();
        }
        else if (role == "SAV")
        {
            var backlog = _reclamationRepository.GetOpenBacklog();
            var mine = _reclamationRepository.GetForSav(actor.UserId);
            items = backlog.Concat(mine)
                .GroupBy(r => r.Id)
                .Select(g => g.First())
                .OrderByDescending(r => r.CreatedAt)
                .ToList();

            if (status.HasValue)
            {
                items = items.Where(r => r.Status == status.Value).ToList();
            }
        }
        else if (role == "ST")
        {
            items = _reclamationRepository.GetForTechnician(actor.UserId);
            if (status.HasValue)
            {
                items = items.Where(r => r.Status == status.Value).ToList();
            }
        }
        else
        {
            items = _reclamationRepository.GetForClient(actor.UserId);
            if (status.HasValue)
            {
                items = items.Where(r => r.Status == status.Value).ToList();
            }
        }

        if (category.HasValue)
        {
            items = items.Where(r => r.Category == category.Value).ToList();
        }

        return items.Select(r => ToDtoWithActions(r, actor)).ToList();
    }

    public PagedResult<ReclamationDto> QueryVisible(
        CurrentUser actor,
        ReclamationStatus? status = null,
        TicketCategory? category = null,
        NamePriority? priority = null,
        string? search = null,
        int page = 1,
        int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        IEnumerable<ReclamationDto> query = GetVisible(actor, status, category);

        if (priority.HasValue)
        {
            query = query.Where(x => x.Priority == priority.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = search.Trim().ToLowerInvariant();
            query = query.Where(x =>
                (x.Reference ?? string.Empty).ToLowerInvariant().Contains(normalized)
                || (x.ClientName ?? string.Empty).ToLowerInvariant().Contains(normalized)
                || (x.Description ?? string.Empty).ToLowerInvariant().Contains(normalized)
                || x.Category.ToString().ToLowerInvariant().Contains(normalized)
                || (x.CategoryReason ?? string.Empty).ToLowerInvariant().Contains(normalized)
                || x.Status.ToString().ToLowerInvariant().Contains(normalized));
        }

        var total = query.Count();
        var items = query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<ReclamationDto>
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
            Items = items
        };
    }

    public void Delete(long id, CurrentUser actor)
    {
        EnsureRole(actor, "ADMIN");

        var existing = GetByIdInternal(id);
        if (existing.Status is not (ReclamationStatus.Open or ReclamationStatus.Cancelled))
        {
            throw new BadRequestException("Only OPEN or CANCELLED reclamations can be deleted.");
        }

        _reclamationRepository.Delete(id);
    }

    public ReclamationDto GetById(long id, CurrentUser actor)
    {
        var reclamation = GetByIdVisible(id, actor);
        return ToDtoWithActions(reclamation, actor);
    }

    public List<ReclamationDto> GetByPriority(NamePriority priority, CurrentUser actor)
    {
        return GetVisible(actor)
            .Where(r => r.Priority == priority)
            .ToList();
    }

    public List<ReclamationDto> GetByCategory(TicketCategory category, CurrentUser actor)
    {
        return GetVisible(actor, category: category);
    }

    public ReclamationDto GetByReference(string reference, CurrentUser actor)
    {
        var reclamation = _reclamationRepository.GetByRefernce(reference);
        if (reclamation == null)
            throw new NotFoundException($"Reclamation with reference '{reference}' not found.");

        EnsureCanView(actor, reclamation);
        return ToDtoWithActions(reclamation, actor);
    }

}
