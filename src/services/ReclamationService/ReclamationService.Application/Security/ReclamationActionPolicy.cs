using ReclamationService.Domain.Entities;
using ReclamationService.Domain.Enums;

namespace ReclamationService.Application.Security;

public static class ReclamationActionPolicy
{
    public const string Edit = "EDIT";
    public const string Assign = "ASSIGN";
    public const string Plan = "PLAN";
    public const string Start = "START";
    public const string Resolve = "RESOLVE";
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
            actions.Add(Plan);
        }

        if ((isSt || isAdmin) && item.Status == ReclamationStatus.Planned && (isAdmin || isMineTech))
        {
            actions.Add(Start);
        }

        if ((isSt || isAdmin) && item.Status == ReclamationStatus.InProgress && (isAdmin || isMineTech))
        {
            actions.Add(Resolve);
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
