using InterventionService.Domain.Entities;
using InterventionService.Domain.Enums;

namespace InterventionService.Application.Security;

public static class PlanningActionPolicy
{
    public const string AssignTechnician = "ASSIGN_TECHNICIAN";
    public const string Confirm = "CONFIRM_APPOINTMENT";
    public const string Reschedule = "RESCHEDULE_APPOINTMENT";
    public const string Cancel = "CANCEL_APPOINTMENT";

    public static List<string> GetAllowedActions(CurrentUser actor, Appointment appointment)
    {
        var role = NormalizeRole(actor.Role);
        var isSavOrAdmin = role is "SAV" or "ADMIN";
        var isTech = role == "ST";
        var actions = new List<string>();

        if (isSavOrAdmin && appointment.Status is AppointmentStatus.Proposed or AppointmentStatus.Rescheduled)
        {
            actions.Add(AssignTechnician);
            actions.Add(Confirm);
            actions.Add(Reschedule);
            actions.Add(Cancel);
        }

        if (isSavOrAdmin && appointment.Status == AppointmentStatus.Confirmed)
        {
            actions.Add(Reschedule);
            actions.Add(Cancel);
        }

        if (isTech && appointment.TechnicianId == actor.UserId && appointment.Status == AppointmentStatus.Confirmed)
        {
            actions.Add(Reschedule);
        }

        return actions;
    }

    private static string NormalizeRole(string role) => (role ?? string.Empty).Trim().ToUpperInvariant();
}
