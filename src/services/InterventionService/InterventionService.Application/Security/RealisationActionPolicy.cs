using InterventionService.Domain.Entities;
using InterventionService.Domain.Enums;

namespace InterventionService.Application.Security;

public static class RealisationActionPolicy
{
    public const string Start = "START_INTERVENTION";
    public const string Pause = "PAUSE_INTERVENTION";
    public const string RecordDiagnostic = "RECORD_DIAGNOSTIC";
    public const string AddRepairAction = "ADD_REPAIR_ACTION";
    public const string AddPart = "ADD_PART_USED";
    public const string AddEvidence = "ADD_EVIDENCE";
    public const string Complete = "COMPLETE_INTERVENTION";
    public const string PublishReport = "PUBLISH_REPORT";
    public const string RequestReplanning = "REQUEST_REPLANNING";

    public static List<string> GetAllowedActions(CurrentUser actor, Intervention intervention)
    {
        var role = NormalizeRole(actor.Role);
        var isAdmin = role == "ADMIN";
        var isTechOwner = IsTechnicianRole(role) && intervention.TechnicianId == actor.UserId;
        var isSav = role == "SAV";
        var canOperate = isAdmin || isTechOwner;
        var actions = new List<string>();

        if (canOperate && intervention.Status == InterventionStatus.Ready)
        {
            actions.Add(Start);
        }

        if (canOperate && intervention.Status == InterventionStatus.Started)
        {
            actions.Add(Pause);
            actions.Add(RecordDiagnostic);
            actions.Add(AddRepairAction);
            actions.Add(AddPart);
            actions.Add(AddEvidence);
            actions.Add(Complete);
            actions.Add(RequestReplanning);
        }

        if (canOperate && intervention.Status == InterventionStatus.Paused)
        {
            actions.Add(Start);
            actions.Add(RecordDiagnostic);
            actions.Add(AddRepairAction);
            actions.Add(AddPart);
            actions.Add(AddEvidence);
            actions.Add(Complete);
            actions.Add(RequestReplanning);
        }

        if ((canOperate || isSav) && intervention.Status == InterventionStatus.Completed)
        {
            actions.Add(PublishReport);
        }

        return actions;
    }

    private static bool IsTechnicianRole(string role) => role is "ST" or "TECHNICIAN";

    private static string NormalizeRole(string role) => (role ?? string.Empty).Trim().Replace("-", "_").Replace(" ", "_").ToUpperInvariant();
}
