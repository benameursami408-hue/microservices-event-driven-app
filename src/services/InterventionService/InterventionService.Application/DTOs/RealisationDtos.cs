using InterventionService.Domain.Enums;

namespace InterventionService.Application.DTOs;

public class InterventionDto
{
    public Guid Id { get; set; }
    public Guid AppointmentId { get; set; }
    public long ReclamationId { get; set; }
    public string Reference { get; set; } = string.Empty;
    public long TechnicianId { get; set; }
    public string TechnicianName { get; set; } = string.Empty;
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public InterventionStatus Status { get; set; }
    public InterventionOutcome? Outcome { get; set; }
    public bool NeedsReplanning { get; set; }
    public string? LatestReportSummary { get; set; }
    public List<string> AllowedActions { get; set; } = new();
}

public class RecordDiagnosticDto
{
    public string Category { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string? RootCause { get; set; }
    public bool RequiresParts { get; set; }
    public bool RequiresFollowUp { get; set; }
}

public class AddRepairActionDto
{
    public string ActionType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Success { get; set; }
}

public class AddPartUsedDto
{
    public string PartCode { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public string AvailabilityStatus { get; set; } = "Used";
}

public class AddEvidenceDto
{
    public string Kind { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}

public class CompleteInterventionDto
{
    public InterventionOutcome Outcome { get; set; }
    public bool NeedsReplanning { get; set; }
}

public class PublishVisitReportDto
{
    public string Summary { get; set; } = string.Empty;
    public InterventionOutcome Outcome { get; set; }
    public bool CustomerPresent { get; set; }
    public string? NextStep { get; set; }
}

public class RequestReplanningDto
{
    public string ReasonCode { get; set; } = string.Empty;
    public string? ReasonText { get; set; }
}
