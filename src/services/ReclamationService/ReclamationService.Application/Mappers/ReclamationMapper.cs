using ReclamationService.Application.DTOs;
using ReclamationService.Domain.Entities;

namespace ReclamationService.Application.Mappers;

public static class ReclamationMapper
{
    public static ReclamationDto ToDto(this Reclamation reclamation)
    {
        if (reclamation == null)
        {
            return null!;
        }

        return new ReclamationDto
        {
            Id = reclamation.Id,
            Reference = reclamation.Reference,
            Description = reclamation.Description,
            Priority = reclamation.Priority,
            Status = reclamation.Status,
            CreatedAt = reclamation.CreatedAt,
            UpdatedAt = reclamation.UpdatedAt,
            ClientId = reclamation.ClientId,
            ClientName = reclamation.ClientName,
            SavId = reclamation.SAVId,
            SavName = reclamation.SAVName,
            AssignedAt = reclamation.AssignedAt,
            TechnicianId = reclamation.TechnicianId,
            TechnicianName = reclamation.TechnicianName,
            PlannedStartAt = reclamation.PlannedStartAt,
            PlannedEndAt = reclamation.PlannedEndAt,
            PlanningNote = reclamation.PlanningNote,
            ResolutionNote = reclamation.ResolutionNote,
            ResolvedAt = reclamation.ResolvedAt,
            ClosedAt = reclamation.ClosedAt,
            CancelledAt = reclamation.CancelledAt,
            RejectedAt = reclamation.RejectedAt,
            RejectionReason = reclamation.RejectionReason
        };
    }

    public static Reclamation ToEntity(this CreateReclamationDto dto, long clientId, string clientName)
    {
        if (dto == null)
        {
            return null!;
        }

        return new Reclamation
        {
            Reference = GenerateReference(),
            Description = dto.Description,
            Priority = dto.Priority,
            Status = ReclamationService.Domain.Enums.ReclamationStatus.Open,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            ClientId = clientId,
            ClientName = clientName
        };
    }

    public static void ApplyUpdate(this Reclamation reclamation, UpdateReclamationDto dto)
    {
        reclamation.Description = dto.Description;
        reclamation.Priority = dto.Priority;
        reclamation.UpdatedAt = DateTime.UtcNow;
    }

    private static string GenerateReference()
    {
        return $"REC-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";
    }
}
