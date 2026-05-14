using InterventionService.Application.DTOs;
using InterventionService.Application.Security;
using InterventionService.Domain.Entities;
using InterventionService.Domain.Enums;
using InterventionService.Domain.Interfaces;

namespace InterventionService.Application.Services;

public class VisitReportsService
{
    private readonly IInterventionRepository _interventionRepository;
    public VisitReportsService(IInterventionRepository interventionRepository) => _interventionRepository = interventionRepository;

    public async Task<List<VisitReportDto>> QueryAsync(CurrentUser actor, CancellationToken cancellationToken = default)
    {
        long? technicianId = IsTechnicianRole(actor.Role) ? actor.UserId : null;
        long? clientId = NormalizeRole(actor.Role) == "CLIENT" ? actor.UserId : null;
        var reports = await _interventionRepository.QueryVisitReportsAsync(clientId, technicianId, cancellationToken);
        var result = new List<VisitReportDto>();
        foreach (var report in reports)
        {
            var intervention = await _interventionRepository.GetByIdAsync(report.InterventionId, cancellationToken);
            if (intervention is not null) result.Add(ToDto(report, intervention));
        }
        return result;
    }

    public async Task<VisitReportDto?> GetAsync(Guid id, CurrentUser actor, CancellationToken cancellationToken = default)
    {
        var report = await _interventionRepository.GetVisitReportAsync(id, cancellationToken);
        if (report is null) return null;
        var intervention = await _interventionRepository.GetByIdAsync(report.InterventionId, cancellationToken);
        if (intervention is null) return null;
        EnsureCanRead(actor, intervention);
        return ToDto(report, intervention);
    }

    public async Task<VisitReportDto> UpdateAsync(Guid id, UpdateVisitReportDto dto, CurrentUser actor, CancellationToken cancellationToken = default)
    {
        EnsureRole(actor, "SAV", "ADMIN", "ST", "TECHNICIAN");
        var report = await _interventionRepository.GetVisitReportAsync(id, cancellationToken) ?? throw new InvalidOperationException("Visit report not found.");
        var intervention = await _interventionRepository.GetByIdAsync(report.InterventionId, cancellationToken) ?? throw new InvalidOperationException("Intervention not found.");
        EnsureCanRead(actor, intervention);
        report.Summary = dto.Summary;
        report.Outcome = dto.Outcome;
        report.CustomerPresent = dto.CustomerPresent;
        report.NextStep = dto.NextStep;
        await _interventionRepository.SaveChangesAsync(cancellationToken);
        return ToDto(report, intervention);
    }

    public async Task<VisitReportDto> PublishAsync(Guid id, CurrentUser actor, CancellationToken cancellationToken = default)
    {
        EnsureRole(actor, "SAV", "ADMIN", "ST", "TECHNICIAN");
        var report = await _interventionRepository.GetVisitReportAsync(id, cancellationToken) ?? throw new InvalidOperationException("Visit report not found.");
        var intervention = await _interventionRepository.GetByIdAsync(report.InterventionId, cancellationToken) ?? throw new InvalidOperationException("Intervention not found.");
        EnsureCanRead(actor, intervention);
        report.Status = VisitReportStatus.Published;
        report.PublishedAt ??= DateTime.UtcNow;
        await _interventionRepository.SaveChangesAsync(cancellationToken);
        return ToDto(report, intervention);
    }

    private static VisitReportDto ToDto(VisitReport report, Intervention intervention) => new()
    {
        Id = report.Id,
        InterventionId = report.InterventionId,
        ReclamationId = intervention.ReclamationId,
        ClientId = intervention.ClientId,
        ClientName = intervention.Reference,
        TechnicianId = intervention.TechnicianId,
        TechnicianName = intervention.TechnicianName,
        Status = report.Status,
        Summary = report.Summary,
        Outcome = report.Outcome,
        CustomerPresent = report.CustomerPresent,
        NextStep = report.NextStep,
        CreatedAt = report.CreatedAt,
        PublishedAt = report.PublishedAt
    };

    private static void EnsureCanRead(CurrentUser actor, Intervention intervention)
    {
        var role = NormalizeRole(actor.Role);
        if (role == "ADMIN" || role == "SAV") return;
        if (IsTechnicianRole(actor.Role) && intervention.TechnicianId == actor.UserId) return;
        if (role == "CLIENT" && intervention.ClientId == actor.UserId) return;
        throw new UnauthorizedAccessException();
    }

    private static void EnsureRole(CurrentUser actor, params string[] roles)
    {
        if (!roles.Any(role => RoleMatches(actor.Role, role))) throw new UnauthorizedAccessException();
    }

    private static bool RoleMatches(string currentRole, string expectedRole)
    {
        var current = NormalizeRole(currentRole);
        var expected = NormalizeRole(expectedRole);
        if (expected == "ST" || expected == "TECHNICIAN")
        {
            return current is "ST" or "TECHNICIAN";
        }

        return current == expected;
    }

    private static bool IsTechnicianRole(string role) => NormalizeRole(role) is "ST" or "TECHNICIAN";

    private static string NormalizeRole(string role) => (role ?? string.Empty).Trim().Replace("-", "_").Replace(" ", "_").ToUpperInvariant();
}
