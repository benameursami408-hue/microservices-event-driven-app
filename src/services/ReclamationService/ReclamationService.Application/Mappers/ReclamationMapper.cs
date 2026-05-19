using ReclamationService.Application.DTOs;
using ReclamationService.Domain.Entities;
using ReclamationService.Domain.Enums;
using System.Text.Json;

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
            Category = reclamation.Category,
            CategoryReason = reclamation.CategoryReason,
            CategoryUpdatedAt = reclamation.CategoryUpdatedAt,
            Priority = reclamation.Priority,
            Severity = reclamation.Severity,
            PriorityScore = reclamation.PriorityScore,
            PriorityReasons = DeserializeReasons(reclamation.PriorityReasons),
            PrioritySource = reclamation.PrioritySource,
            PriorityUpdatedAt = reclamation.PriorityUpdatedAt,
            ManualPriorityOverride = reclamation.ManualPriorityOverride,
            ManualPriorityOverrideReason = reclamation.ManualPriorityOverrideReason,
            IsBlocking = reclamation.IsBlocking,
            FollowUpCount = reclamation.FollowUpCount,
            FirstResponseDeadline = reclamation.FirstResponseDeadline,
            PlanningDeadline = reclamation.PlanningDeadline,
            ResolutionDeadline = reclamation.ResolutionDeadline,
            SlaStatus = reclamation.SlaStatus,
            SlaBreachedAt = reclamation.SlaBreachedAt,
            Status = reclamation.Status,
            CreatedAt = reclamation.CreatedAt,
            UpdatedAt = reclamation.UpdatedAt,
            ClientId = reclamation.ClientId,
            ClientName = reclamation.ClientName,
            SavId = reclamation.SAVId,
            SavName = reclamation.SAVName,
            AssignedAt = reclamation.AssignedAt,
            ClaimedBySavId = reclamation.ClaimedBySavId,
            ClaimedBySavName = reclamation.ClaimedBySavName,
            ClaimedAt = reclamation.ClaimedAt,
            ReleasedAt = reclamation.ReleasedAt,
            IsClaimed = reclamation.ClaimedBySavId.HasValue,
            TechnicianId = reclamation.TechnicianId,
            TechnicianName = reclamation.TechnicianName,
            PlannedStartAt = reclamation.PlannedStartAt,
            PlannedEndAt = reclamation.PlannedEndAt,
            NextAppointmentAt = reclamation.PlannedStartAt,
            NextAppointmentEndAt = reclamation.PlannedEndAt,
            PlanningNote = reclamation.PlanningNote,
            PlanningRequestedAt = reclamation.PlanningRequestedAt,
            RequiresReplanning = reclamation.RequiresReplanning,
            LastInterventionOutcome = reclamation.LastInterventionOutcome,
            LastInterventionReportSummary = reclamation.LastInterventionReportSummary,
            ResolutionNote = reclamation.ResolutionNote,
            ResolvedAt = reclamation.ResolvedAt,
            ClosedAt = reclamation.ClosedAt,
            CancelledAt = reclamation.CancelledAt,
            RejectedAt = reclamation.RejectedAt,
            RejectionReason = reclamation.RejectionReason,
            ProductName = reclamation.ProductName,
            Barcode = reclamation.Barcode,
            ProductImageUrl = reclamation.ProductImageUrl,
            PurchaseDate = reclamation.PurchaseDate,
            Brand = reclamation.Brand,
            Model = reclamation.Model,
            SerialNumber = reclamation.SerialNumber,
            ProductReference = reclamation.ProductReference,
            SellerName = reclamation.SellerName,
            PurchaseProofUrl = reclamation.PurchaseProofUrl
        };
    }

    public static Reclamation ToEntity(this CreateReclamationDto dto, long clientId, string clientName)
    {
        if (dto == null)
        {
            return null!;
        }

        var hasPriority = dto.Priority.HasValue;
        var priority = dto.Priority ?? NamePriority.LOW;
        var priorityReason = hasPriority
            ? "Priority selected by SAV/Admin at creation."
            : "Priority not selected by client; awaiting AI/SAV review.";

        return new Reclamation
        {
            Reference = GenerateReference(),
            Description = string.IsNullOrWhiteSpace(dto.Description) ? "No description provided." : dto.Description.Trim(),
            Priority = priority,
            Severity = priority,
            Status = ReclamationService.Domain.Enums.ReclamationStatus.Open,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            ClientId = clientId,
            ClientName = clientName,
            PriorityScore = 0,
            PriorityReasons = JsonSerializer.Serialize(new[] { priorityReason }),
            PrioritySource = hasPriority ? PrioritySource.ManualOverride : PrioritySource.PendingReview,
            PriorityUpdatedAt = hasPriority ? DateTime.UtcNow : null,
            ManualPriorityOverride = hasPriority,
            ManualPriorityOverrideReason = hasPriority ? priorityReason : null,
            IsBlocking = dto.IsBlocking,
            FollowUpCount = dto.FollowUpCount,
            ProductName = dto.ProductName,
            Barcode = dto.Barcode,
            ProductImageUrl = dto.ProductImageUrl,
            PurchaseDate = dto.PurchaseDate,
            Brand = dto.Brand,
            Model = dto.Model,
            SerialNumber = dto.SerialNumber,
            ProductReference = dto.ProductReference,
            SellerName = dto.SellerName,
            PurchaseProofUrl = dto.PurchaseProofUrl
        };
    }

    public static void ApplyUpdate(this Reclamation reclamation, UpdateReclamationDto dto)
    {
        reclamation.Description = string.IsNullOrWhiteSpace(dto.Description) ? "No description provided." : dto.Description.Trim();
        reclamation.Severity = dto.Priority;
        reclamation.IsBlocking = dto.IsBlocking;
        reclamation.FollowUpCount = dto.FollowUpCount;
        reclamation.ProductName = dto.ProductName;
        reclamation.Barcode = dto.Barcode;
        reclamation.ProductImageUrl = dto.ProductImageUrl;
        reclamation.PurchaseDate = dto.PurchaseDate;
        reclamation.Brand = dto.Brand;
        reclamation.Model = dto.Model;
        reclamation.SerialNumber = dto.SerialNumber;
        reclamation.ProductReference = dto.ProductReference;
        reclamation.SellerName = dto.SellerName;
        reclamation.PurchaseProofUrl = dto.PurchaseProofUrl;
        reclamation.UpdatedAt = DateTime.UtcNow;
    }

    private static string GenerateReference()
    {
        return $"REC-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";
    }

    private static List<string> DeserializeReasons(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new List<string>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(value) ?? new List<string>();
        }
        catch
        {
            return new List<string> { value };
        }
    }
}
