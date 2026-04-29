using InterventionService.Domain.Enums;

namespace InterventionService.Application.DTOs;

public class PlanningRequestDto
{
    public Guid Id { get; set; }
    public long ReclamationId { get; set; }
    public string Reference { get; set; } = string.Empty;
    public long SavId { get; set; }
    public string SavName { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public long ClientId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public string? CustomerPhone { get; set; }
    public string? ServiceAddress { get; set; }
    public PlanningRequestStatus Status { get; set; }
    public DateTime RequestedAt { get; set; }
}

public class AppointmentDto
{
    public Guid Id { get; set; }
    public Guid PlanningRequestId { get; set; }
    public long ReclamationId { get; set; }
    public string Reference { get; set; } = string.Empty;
    public DateTime StartAt { get; set; }
    public DateTime? EndAt { get; set; }
    public int EstimatedDurationMinutes { get; set; }
    public string TimeZone { get; set; } = "UTC";
    public long? TechnicianId { get; set; }
    public string? TechnicianName { get; set; }
    public bool CustomerPresenceRequired { get; set; }
    public AppointmentStatus Status { get; set; }
    public int Sequence { get; set; }
    public string? PlanningNote { get; set; }
    public List<string> ScheduleWarnings { get; set; } = new();
    public List<string> AllowedActions { get; set; } = new();
}

public class CreateAppointmentDto
{
    public Guid PlanningRequestId { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime? EndAt { get; set; }
    public int EstimatedDurationMinutes { get; set; } = 90;
    public string? TimeZone { get; set; }
    public bool CustomerPresenceRequired { get; set; } = true;
    public string? PlanningNote { get; set; }
}

public class AssignTechnicianDto
{
    public long TechnicianId { get; set; }
    public string TechnicianName { get; set; } = string.Empty;
}

public class RescheduleAppointmentDto
{
    public DateTime StartAt { get; set; }
    public DateTime? EndAt { get; set; }
    public int? EstimatedDurationMinutes { get; set; }
    public string ReasonCode { get; set; } = string.Empty;
    public string? ReasonText { get; set; }
}

public class CancelAppointmentDto
{
    public string ReasonCode { get; set; } = string.Empty;
    public string? ReasonText { get; set; }
}

public class TechnicianCapacityDto
{
    public long TechnicianId { get; set; }
    public DateTime Date { get; set; }
    public DateTime WeekStart { get; set; }
    public int DailyMaxAppointments { get; set; }
    public int DailyAssignedAppointments { get; set; }
    public int DailyMaxMinutes { get; set; }
    public int DailyBookedMinutes { get; set; }
    public int WeeklyMaxAppointments { get; set; }
    public int WeeklyAssignedAppointments { get; set; }
    public int WeeklyMaxMinutes { get; set; }
    public int WeeklyBookedMinutes { get; set; }
    public int BufferMinutes { get; set; }
    public double DailyLoadPercent { get; set; }
    public double WeeklyLoadPercent { get; set; }
    public int RemainingDailyAppointments { get; set; }
    public int RemainingWeeklyAppointments { get; set; }
}
