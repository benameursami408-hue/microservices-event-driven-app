using InterventionService.Application.Security;
using InterventionService.Domain.Entities;
using InterventionService.Domain.Enums;

namespace InterventionService.Tests.Security;

public class PlanningActionPolicyTests
{
    [Fact]
    public void Sav_CanConfirmAndRescheduleProposedAppointment()
    {
        var actor = new CurrentUser(20, "sav@sav.local", "SAV User", "SAV", "corr-1");
        var appointment = CreateAppointment(AppointmentStatus.Proposed);

        var actions = PlanningActionPolicy.GetAllowedActions(actor, appointment);

        Assert.Contains(PlanningActionPolicy.AssignTechnician, actions);
        Assert.Contains(PlanningActionPolicy.Confirm, actions);
        Assert.Contains(PlanningActionPolicy.Reschedule, actions);
        Assert.Contains(PlanningActionPolicy.Cancel, actions);
    }

    [Fact]
    public void Technician_CanOnlyRescheduleOwnConfirmedAppointment()
    {
        var owner = new CurrentUser(30, "tech@sav.local", "Tech User", "ST", "corr-2");
        var other = new CurrentUser(31, "other@sav.local", "Other Tech", "ST", "corr-3");
        var appointment = CreateAppointment(AppointmentStatus.Confirmed, technicianId: 30);

        var ownerActions = PlanningActionPolicy.GetAllowedActions(owner, appointment);
        var otherActions = PlanningActionPolicy.GetAllowedActions(other, appointment);

        Assert.Equal(new[] { PlanningActionPolicy.Reschedule }, ownerActions);
        Assert.Empty(otherActions);
    }

    private static Appointment CreateAppointment(AppointmentStatus status, long? technicianId = null)
        => new()
        {
            Id = Guid.NewGuid(),
            PlanningRequestId = Guid.NewGuid(),
            ReclamationId = 100,
            Reference = "REC-20260430-0001",
            StartAt = DateTime.UtcNow.Date.AddHours(9),
            EndAt = DateTime.UtcNow.Date.AddHours(10),
            EstimatedDurationMinutes = 60,
            TechnicianId = technicianId,
            TechnicianName = technicianId.HasValue ? "Tech" : null,
            Status = status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
}
