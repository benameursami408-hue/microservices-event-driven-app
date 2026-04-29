using ReclamationService.Domain.Entities;
using ReclamationService.Domain.Enums;

namespace ReclamationService.Application.Security;

public static class ReclamationActionPolicy
{
    public const string Edit = "EDIT";
    public const string Assign = "ASSIGN";
    public const string RequestPlanning = "REQUEST_PLANNING";
    public const string RecalculatePriority = "RECALCULATE_PRIORITY";
    public const string OverridePriority = "OVERRIDE_PRIORITY";
    public const string Close = "CLOSE";
    public const string Cancel = "CANCEL";
    public const string Reject = "REJECT";
    public const string Delete = "DELETE";

    public static List<string> GetAllowedActions(CurrentUser actor, Reclamation item)
    {
        var role = NormalizeRole(actor.Role);
        var actions = new List<string>();

        var isAdmin = role == "ADMIN";
        var isClient = role == "CLIENT";
        var isSav = role == "SAV";
        var isSt = role == "ST";

        var isMineClient = item.ClientId == actor.UserId;
        var isMineSav = item.SAVId == actor.UserId;
        var isMineTech = item.TechnicianId == actor.UserId;

        var canEditDetails =
            isAdmin
            || (isSav && isMineSav && item.Status is not (ReclamationStatus.Closed or ReclamationStatus.Cancelled or ReclamationStatus.Rejected))
            || (isClient && isMineClient && item.Status == ReclamationStatus.Open);

        if (canEditDetails)
        {
            actions.Add(Edit);
        }

        if ((isSav || isAdmin) && item.Status == ReclamationStatus.Open)
        {
            actions.Add(Assign);
        }

        if ((isSav || isAdmin) && item.Status == ReclamationStatus.Assigned && (isAdmin || isMineSav))
        {
            actions.Add(RequestPlanning);
        }

        if ((isSav || isAdmin)
            && item.Status is not (ReclamationStatus.Closed or ReclamationStatus.Cancelled or ReclamationStatus.Rejected)
            && (isAdmin || item.Status == ReclamationStatus.Open || isMineSav))
        {
            actions.Add(RecalculatePriority);
            actions.Add(OverridePriority);
        }

        if ((isSav || isAdmin) && item.Status == ReclamationStatus.Resolved && (isAdmin || isMineSav))
        {
            actions.Add(Close);
        }

        if ((isClient || isAdmin) && item.Status == ReclamationStatus.Open && (isAdmin || isMineClient))
        {
            actions.Add(Cancel);
        }

        if ((isSav || isAdmin) && item.Status is (ReclamationStatus.Open or ReclamationStatus.Assigned) && (isAdmin || isMineSav))
        {
            actions.Add(Reject);
        }

        if (isAdmin && item.Status is (ReclamationStatus.Open or ReclamationStatus.Cancelled))
        {
            actions.Add(Delete);
        }

        return actions;
    }

    private static string NormalizeRole(string role)
    {
        return (role ?? string.Empty).Trim().ToUpperInvariant();
    }
}
