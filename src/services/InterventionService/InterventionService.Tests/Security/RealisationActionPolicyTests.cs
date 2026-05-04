using InterventionService.Application.Security;
using InterventionService.Domain.Entities;
using InterventionService.Domain.Enums;

namespace InterventionService.Tests.Security;

public class RealisationActionPolicyTests
{
    [Fact]
    public void AssignedTechnician_CanStartReadyIntervention()
    {
        var actor = new CurrentUser(30, "tech@sav.local", "Tech User", "ST", "corr-1");
        var intervention = CreateIntervention(technicianId: 30, status: InterventionStatus.Ready);

        var actions = RealisationActionPolicy.GetAllowedActions(actor, intervention);

        Assert.Contains(RealisationActionPolicy.Start, actions);
        Assert.DoesNotContain(RealisationActionPolicy.Complete, actions);
    }

    [Fact]
    public void OtherTechnician_CannotOperateIntervention()
    {
        var actor = new CurrentUser(31, "other@sav.local", "Other Tech", "ST", "corr-2");
        var intervention = CreateIntervention(technicianId: 30, status: InterventionStatus.Started);

        var actions = RealisationActionPolicy.GetAllowedActions(actor, intervention);

        Assert.Empty(actions);
    }

    [Fact]
    public void Sav_CanPublishCompletedReport_ButCannotStartIntervention()
    {
        var actor = new CurrentUser(20, "sav@sav.local", "SAV User", "SAV", "corr-3");
        var completed = CreateIntervention(technicianId: 30, status: InterventionStatus.Completed);
        var ready = CreateIntervention(technicianId: 30, status: InterventionStatus.Ready);

        var completedActions = RealisationActionPolicy.GetAllowedActions(actor, completed);
        var readyActions = RealisationActionPolicy.GetAllowedActions(actor, ready);

        Assert.Contains(RealisationActionPolicy.PublishReport, completedActions);
        Assert.DoesNotContain(RealisationActionPolicy.Start, readyActions);
    }

    private static Intervention CreateIntervention(long technicianId, InterventionStatus status)
        => new()
        {
            Id = Guid.NewGuid(),
            AppointmentId = Guid.NewGuid(),
            ReclamationId = 100,
            Reference = "REC-20260430-0001",
            TechnicianId = technicianId,
            TechnicianName = "Tech",
            Status = status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
}
