using ReclamationService.Application.Security;
using ReclamationService.Domain.Entities;
using ReclamationService.Domain.Enums;

namespace ReclamationService.Tests.Security;

public class ReclamationActionPolicyTests
{
    [Fact]
    public void Client_OwnOpenCase_CanEditAndCancel()
    {
        var actor = new CurrentUser(10, "client@local", "Client User", "CLIENT", "corr-1");
        var item = CreateReclamation(clientId: 10, status: ReclamationStatus.Open);

        var actions = ReclamationActionPolicy.GetAllowedActions(actor, item);

        Assert.Contains(ReclamationActionPolicy.Edit, actions);
        Assert.Contains(ReclamationActionPolicy.Cancel, actions);
    }

    [Fact]
    public void Sav_AssignedCase_CanPlanAndEdit()
    {
        var actor = new CurrentUser(20, "sav@local", "Sav User", "SAV", "corr-2");
        var item = CreateReclamation(clientId: 10, savId: 20, status: ReclamationStatus.Assigned);

        var actions = ReclamationActionPolicy.GetAllowedActions(actor, item);

        Assert.Contains(ReclamationActionPolicy.RequestPlanning, actions);
        Assert.Contains(ReclamationActionPolicy.Edit, actions);
        Assert.Contains(ReclamationActionPolicy.RecalculatePriority, actions);
    }

    [Fact]
    public void Technician_PlannedCase_CannotClose()
    {
        var actor = new CurrentUser(30, "st@local", "Tech User", "ST", "corr-3");
        var item = CreateReclamation(clientId: 10, technicianId: 30, status: ReclamationStatus.Planned);

        var actions = ReclamationActionPolicy.GetAllowedActions(actor, item);

        Assert.DoesNotContain(ReclamationActionPolicy.Close, actions);
    }

    [Fact]
    public void Admin_OpenCase_CanAssignRejectAndDelete()
    {
        var actor = new CurrentUser(1, "admin@local", "Admin User", "ADMIN", "corr-4");
        var item = CreateReclamation(clientId: 10, status: ReclamationStatus.Open);

        var actions = ReclamationActionPolicy.GetAllowedActions(actor, item);

        Assert.Contains(ReclamationActionPolicy.Assign, actions);
        Assert.Contains(ReclamationActionPolicy.Reject, actions);
        Assert.Contains(ReclamationActionPolicy.Delete, actions);
    }

    private static Reclamation CreateReclamation(
        long clientId,
        ReclamationStatus status,
        long? savId = null,
        long? technicianId = null)
    {
        return new Reclamation
        {
            Id = 100,
            Reference = "REC-20260101-ABCDEF",
            Description = "Issue",
            Priority = NamePriority.MEDUIM,
            Status = status,
            ClientId = clientId,
            ClientName = "Client",
            SAVId = savId,
            SAVName = savId.HasValue ? "SAV" : null,
            ClaimedBySavId = savId,
            ClaimedBySavName = savId.HasValue ? "SAV" : null,
            ClaimedAt = savId.HasValue ? DateTime.UtcNow : null,
            TechnicianId = technicianId,
            TechnicianName = technicianId.HasValue ? "Tech" : null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
