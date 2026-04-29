using ReclamationService.Application.Services;
using ReclamationService.Domain.Entities;
using ReclamationService.Domain.Enums;

namespace ReclamationService.Tests.Services;

public class ReclamationSlaServiceTests
{
    private readonly ReclamationSlaService _service = new();

    [Fact]
    public void Compute_WhenOpenUrgentAndDeadlinePassed_ReturnsBreached()
    {
        var now = DateTime.UtcNow;
        var reclamation = new Reclamation
        {
            Reference = "REC-SLA-1",
            Description = "Panne critique",
            Priority = NamePriority.URGENT,
            Severity = NamePriority.URGENT,
            Status = ReclamationStatus.Open,
            ClientId = 1,
            ClientName = "Client",
            CreatedAt = now.AddHours(-2),
            UpdatedAt = now.AddHours(-2)
        };

        var result = _service.Compute(reclamation, now);

        Assert.Equal(SlaStatus.Breached, result.Status);
        Assert.Equal("FIRST_RESPONSE", result.ActiveTarget);
        Assert.NotNull(result.BreachedAt);
    }

    [Fact]
    public void Compute_WhenAssignedMediumApproachesDeadline_ReturnsNearBreach()
    {
        var now = DateTime.UtcNow;
        var assignedAt = now.AddDays(-2).AddHours(-16);
        var reclamation = new Reclamation
        {
            Reference = "REC-SLA-2",
            Description = "Planification a suivre",
            Priority = NamePriority.MEDUIM,
            Severity = NamePriority.MEDUIM,
            Status = ReclamationStatus.Assigned,
            ClientId = 1,
            ClientName = "Client",
            CreatedAt = assignedAt.AddHours(-2),
            UpdatedAt = assignedAt,
            AssignedAt = assignedAt
        };

        var result = _service.Compute(reclamation, now);

        Assert.Equal(SlaStatus.NearBreach, result.Status);
        Assert.Equal("PLANNING", result.ActiveTarget);
        Assert.NotNull(result.ActiveDeadline);
    }
}
