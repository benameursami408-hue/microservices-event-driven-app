using ReclamationService.Application.Services;
using ReclamationService.Domain.Entities;
using ReclamationService.Domain.Enums;

namespace ReclamationService.Tests.Services;

public class ReclamationPriorityServiceTests
{
    private readonly ReclamationPriorityService _service = new();

    [Fact]
    public void Compute_WithBlockingUrgentDescriptionAndSlaRisk_ReturnsUrgentWithReasons()
    {
        var reclamation = new Reclamation
        {
            Reference = "REC-1",
            Description = "Panne bloquante, traitement urgent asap",
            Priority = NamePriority.MEDUIM,
            Severity = NamePriority.HIGH,
            Status = ReclamationStatus.Assigned,
            ClientId = 1,
            ClientName = "Client",
            CreatedAt = DateTime.UtcNow.AddDays(-3),
            UpdatedAt = DateTime.UtcNow,
            IsBlocking = true,
            FollowUpCount = 3,
            SlaStatus = SlaStatus.NearBreach
        };

        var result = _service.Compute(reclamation);

        Assert.Equal(NamePriority.URGENT, result.Level);
        Assert.True(result.Score >= 85);
        Assert.Contains(result.Reasons, x => x.Contains("Panne bloquante"));
        Assert.Contains(result.Reasons, x => x.Contains("Risque SLA eleve"));
        Assert.Contains(result.Reasons, x => x.Contains("mots d'urgence"));
    }

    [Fact]
    public void ApplyAutomaticPriority_UsesSeverityAsPrimarySignal()
    {
        var reclamation = new Reclamation
        {
            Reference = "REC-2",
            Description = "Demande standard",
            Priority = NamePriority.LOW,
            Severity = NamePriority.MEDUIM,
            Status = ReclamationStatus.Open,
            ClientId = 1,
            ClientName = "Client",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _service.ApplyAutomaticPriority(reclamation);

        Assert.Equal(NamePriority.MEDUIM, reclamation.Priority);
        Assert.True(reclamation.PriorityScore >= 25);
        Assert.Equal(PrioritySource.Rules, reclamation.PrioritySource);
        Assert.NotNull(reclamation.PriorityUpdatedAt);
    }
}
