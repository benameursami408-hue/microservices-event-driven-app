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
    public const string Claim = "CLAIM";
    public const string ReleaseOwnership = "RELEASE_OWNERSHIP";
    public const string ReassignSav = "REASSIGN_SAV";

    public static List<string> GetAllowedActions(CurrentUser actor, Reclamation item)
    {
        var role = NormalizeRole(actor.Role);
        var actions = new List<string>();

        var isAdmin = role == "ADMIN";
        var isClient = role == "CLIENT";
        var isSav = role == "SAV";
        var isSt = role == "ST";

        var isMineClient = item.ClientId == actor.UserId;
        var isMineSav = item.ClaimedBySavId == actor.UserId;
        var isMineTech = item.TechnicianId == actor.UserId;
        var canWorkAsSav = isSav && isMineSav;
        var canWork = isAdmin || canWorkAsSav;
        var isTerminal = item.Status is ReclamationStatus.Closed or ReclamationStatus.Cancelled or ReclamationStatus.Rejected;

        if (isSav && !item.ClaimedBySavId.HasValue && !isTerminal)
        {
            actions.Add(Claim);
        }

        if ((isAdmin || canWorkAsSav) && item.ClaimedBySavId.HasValue)
        {
            actions.Add(ReleaseOwnership);
        }

        if (isAdmin)
        {
            actions.Add(ReassignSav);
        }

        var canEditDetails =
            isAdmin
            || (canWorkAsSav && !isTerminal)
            || (isClient && isMineClient && item.Status == ReclamationStatus.Open);

        if (canEditDetails)
        {
            actions.Add(Edit);
        }

        if (canWork && item.Status == ReclamationStatus.Open)
        {
            actions.Add(Assign);
        }

        if (canWork && item.Status == ReclamationStatus.Assigned)
        {
            actions.Add(RequestPlanning);
        }

        if (canWork && !isTerminal)
        {
            actions.Add(RecalculatePriority);
            actions.Add(OverridePriority);
        }

        if (canWork && item.Status == ReclamationStatus.Resolved)
        {
            actions.Add(Close);
        }

        if ((isClient || isAdmin) && item.Status == ReclamationStatus.Open && (isAdmin || isMineClient))
        {
            actions.Add(Cancel);
        }

        if (canWork && item.Status is (ReclamationStatus.Open or ReclamationStatus.Assigned))
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
