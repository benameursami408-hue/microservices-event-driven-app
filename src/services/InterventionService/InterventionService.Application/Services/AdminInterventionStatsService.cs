using InterventionService.Application.DTOs;
using InterventionService.Domain.Enums;
using InterventionService.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InterventionService.Application.Services;

public class AdminInterventionStatsService
{
    private readonly IInterventionRepository _interventionRepository;

    public AdminInterventionStatsService(IInterventionRepository interventionRepository)
    {
        _interventionRepository = interventionRepository;
    }

    public async Task<GlobalInterventionStatsDto> GetGlobalStatisticsAsync()
    {
        var statusCounts = await _interventionRepository.Query()
            .GroupBy(i => i.Status)
            .Select(g => new InterventionStatusCountDto { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        return new GlobalInterventionStatsDto
        {
            TotalInterventions = statusCounts.Sum(x => x.Count),
            PlannedInterventions = statusCounts.FirstOrDefault(x => x.Status == InterventionStatus.Ready)?.Count ?? 0,
            InProgressInterventions = statusCounts.Where(x => x.Status is InterventionStatus.Started or InterventionStatus.Paused).Sum(x => x.Count),
            CompletedInterventions = statusCounts.FirstOrDefault(x => x.Status == InterventionStatus.Completed)?.Count ?? 0,
            CancelledInterventions = statusCounts.FirstOrDefault(x => x.Status == InterventionStatus.Aborted)?.Count ?? 0,
            ByStatus = statusCounts.OrderBy(x => x.Status).ToList()
        };
    }

    public async Task<List<TechnicianStatsDto>> GetTechnicianStatisticsAsync()
    {
        var rows = await _interventionRepository.Query()
            .Select(i => new
            {
                i.TechnicianId,
                FullName = i.TechnicianName,
                i.Status,
                i.CreatedAt,
                i.StartedAt,
                i.EndedAt,
                i.UpdatedAt
            })
            .ToListAsync();

        return rows
            .GroupBy(i => i.TechnicianId)
            .Select(g =>
            {
                var assigned = g.Count();
                var completed = g.Count(i => i.Status == InterventionStatus.Completed);
                var inProgress = g.Count(i => i.Status is InterventionStatus.Started or InterventionStatus.Paused);
                var lastIntervention = g
                    .Select(i => i.EndedAt ?? i.StartedAt ?? i.UpdatedAt)
                    .DefaultIfEmpty()
                    .Max();

                return new TechnicianStatsDto
                {
                    TechnicianId = g.Key,
                    FullName = g.Select(i => i.FullName).FirstOrDefault(name => !string.IsNullOrWhiteSpace(name)) ?? $"Technicien #{g.Key}",
                    Email = string.Empty,
                    AssignedCount = assigned,
                    CompletedCount = completed,
                    InProgressCount = inProgress,
                    CompletionRate = assigned == 0 ? 0 : Math.Round((decimal)completed * 100 / assigned, 1),
                    LastInterventionAt = lastIntervention
                };
            })
            .OrderByDescending(x => x.CompletedCount)
            .ThenBy(x => x.FullName)
            .ToList();
    }
}
