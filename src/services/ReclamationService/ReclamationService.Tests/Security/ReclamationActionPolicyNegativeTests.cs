using ReclamationService.Application.Security;
using ReclamationService.Domain.Entities;
using ReclamationService.Domain.Enums;

namespace ReclamationService.Tests.Security;

public class ReclamationActionPolicyNegativeTests
{
    [Fact]
    public void Sav_CannotPlanCaseAssignedToAnotherSavUser()
    {
        var actor = new CurrentUser(21, "sav2@sav.local", "Other SAV", "SAV", "corr-1");
        var item = CreateReclamation(clientId: 10, savId: 20, status: ReclamationStatus.Assigned);

        var actions = ReclamationActionPolicy.GetAllowedActions(actor, item);

        Assert.DoesNotContain(ReclamationActionPolicy.RequestPlanning, actions);
        Assert.DoesNotContain(ReclamationActionPolicy.Edit, actions);
    }

    [Fact]
    public void Client_CannotEditClosedCase()
    {
        var actor = new CurrentUser(10, "client@sav.local", "Client", "CLIENT", "corr-2");
        var item = CreateReclamation(clientId: 10, status: ReclamationStatus.Closed);

        var actions = ReclamationActionPolicy.GetAllowedActions(actor, item);

        Assert.DoesNotContain(ReclamationActionPolicy.Edit, actions);
        Assert.DoesNotContain(ReclamationActionPolicy.Cancel, actions);
    }

    private static Reclamation CreateReclamation(long clientId, ReclamationStatus status, long? savId = null)
        => new()
        {
            Id = 100,
            Reference = "REC-20260430-0001",
            Description = "Issue",
            Priority = NamePriority.MEDUIM,
            Status = status,
            ClientId = clientId,
            ClientName = "Client",
            SAVId = savId,
            SAVName = savId.HasValue ? "SAV" : null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
}
