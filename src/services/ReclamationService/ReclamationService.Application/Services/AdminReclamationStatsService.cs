using Microsoft.EntityFrameworkCore;
using ReclamationService.Application.DTOs;
using ReclamationService.Domain.Enums;
using ReclamationService.Domain.Interfaces;

namespace ReclamationService.Application.Services;

public class AdminReclamationStatsService
{
    private readonly IReclamationRepository _reclamationRepository;

    public AdminReclamationStatsService(IReclamationRepository reclamationRepository)
    {
        _reclamationRepository = reclamationRepository;
    }

    public async Task<ReclamationStatsDto> GetStatsAsync(int days = 14, int latest = 8)
    {
        days = Math.Clamp(days, 7, 90);
        latest = Math.Clamp(latest, 3, 20);

        var query = _reclamationRepository.Query();

        var total = await query.CountAsync();

        var statusCounts = await query
            .GroupBy(r => r.Status)
            .Select(g => new StatusCountDto { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var priorityCounts = await query
            .GroupBy(r => r.Priority)
            .Select(g => new PriorityCountDto { Priority = g.Key, Count = g.Count() })
            .ToListAsync();

        var categoryCounts = await query
            .GroupBy(r => r.Category)
            .Select(g => new CategoryCountDto { Category = g.Key, Count = g.Count() })
            .ToListAsync();

        var latestItems = await query
            .OrderByDescending(r => r.CreatedAt)
            .Take(latest)
            .Select(r => new LatestReclamationDto
            {
                Id = r.Id,
                Reference = r.Reference,
                Category = r.Category,
                Priority = r.Priority,
                Status = r.Status,
                ClientName = r.ClientName,
                CreatedAt = r.CreatedAt,
                SavName = r.SAVName,
                ClaimedBySavId = r.ClaimedBySavId,
                ClaimedBySavName = r.ClaimedBySavName,
                ClaimedAt = r.ClaimedAt,
                TechnicianName = r.TechnicianName
            })
            .ToListAsync();

        var workloadRows = await query
            .Where(r => r.ClaimedBySavId.HasValue
                && r.Status != ReclamationStatus.Closed
                && r.Status != ReclamationStatus.Cancelled
                && r.Status != ReclamationStatus.Rejected)
            .GroupBy(r => new { SavId = r.ClaimedBySavId!.Value, SavName = r.ClaimedBySavName })
            .Select(g => new
            {
                SavId = g.Key.SavId,
                SavName = g.Key.SavName,
                ActiveClaimedCount = g.Count(),
                UrgentOrHighCount = g.Count(r => r.Priority == NamePriority.URGENT || r.Priority == NamePriority.HIGH)
            })
            .OrderByDescending(x => x.ActiveClaimedCount)
            .ThenBy(x => x.SavName)
            .ToListAsync();

        var workload = workloadRows
            .Select(row => new SavWorkloadDto
            {
                SavId = row.SavId,
                SavName = string.IsNullOrWhiteSpace(row.SavName) ? $"SAV#{row.SavId}" : row.SavName,
                ActiveClaimedCount = row.ActiveClaimedCount,
                UrgentOrHighCount = row.UrgentOrHighCount
            })
            .ToList();

        var fromDate = DateTime.UtcNow.Date.AddDays(-(days - 1));
        var trendRaw = await query
            .Where(r => r.CreatedAt >= fromDate)
            .GroupBy(r => r.CreatedAt.Date)
            .Select(g => new TrendPointDto { Date = g.Key, Count = g.Count() })
            .ToListAsync();

        var trendLookup = trendRaw.ToDictionary(x => x.Date.Date, x => x.Count);
        var trend = new List<TrendPointDto>(days);
        for (var i = 0; i < days; i++)
        {
            var date = fromDate.AddDays(i);
            trend.Add(new TrendPointDto
            {
                Date = date,
                Count = trendLookup.TryGetValue(date, out var count) ? count : 0
            });
        }

        var kpis = new ReclamationKpiDto
        {
            Total = total,
            Open = statusCounts.FirstOrDefault(x => x.Status == ReclamationStatus.Open)?.Count ?? 0,
            Assigned = statusCounts.FirstOrDefault(x => x.Status == ReclamationStatus.Assigned)?.Count ?? 0,
            Planned = statusCounts.FirstOrDefault(x => x.Status == ReclamationStatus.Planned)?.Count ?? 0,
            InProgress = statusCounts.FirstOrDefault(x => x.Status == ReclamationStatus.InProgress)?.Count ?? 0,
            Resolved = statusCounts.FirstOrDefault(x => x.Status == ReclamationStatus.Resolved)?.Count ?? 0,
            Closed = statusCounts.FirstOrDefault(x => x.Status == ReclamationStatus.Closed)?.Count ?? 0,
            Cancelled = statusCounts.FirstOrDefault(x => x.Status == ReclamationStatus.Cancelled)?.Count ?? 0,
            Rejected = statusCounts.FirstOrDefault(x => x.Status == ReclamationStatus.Rejected)?.Count ?? 0
        };

        return new ReclamationStatsDto
        {
            Kpis = kpis,
            ByStatus = statusCounts.OrderBy(x => x.Status).ToList(),
            ByPriority = priorityCounts.OrderBy(x => x.Priority).ToList(),
            ByCategory = categoryCounts.OrderBy(x => x.Category).ToList(),
            Trend = trend,
            Latest = latestItems,
            WorkloadBySav = workload
        };
    }

    public async Task<GlobalReclamationStatsDto> GetGlobalStatisticsAsync(int days = 30)
    {
        days = Math.Clamp(days, 7, 180);
        var query = _reclamationRepository.Query();

        var statusCounts = await query
            .GroupBy(r => r.Status)
            .Select(g => new StatusCountDto { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var fromDate = DateTime.UtcNow.Date.AddDays(-(days - 1));
        var trendRaw = await query
            .Where(r => r.CreatedAt >= fromDate)
            .GroupBy(r => r.CreatedAt.Date)
            .Select(g => new TrendPointDto { Date = g.Key, Count = g.Count() })
            .ToListAsync();

        var trendLookup = trendRaw.ToDictionary(x => x.Date.Date, x => x.Count);
        var trend = Enumerable.Range(0, days)
            .Select(offset =>
            {
                var date = fromDate.AddDays(offset);
                return new TrendPointDto
                {
                    Date = date,
                    Count = trendLookup.TryGetValue(date, out var count) ? count : 0
                };
            })
            .ToList();

        return new GlobalReclamationStatsDto
        {
            TotalReclamations = statusCounts.Sum(x => x.Count),
            OpenReclamations = statusCounts.FirstOrDefault(x => x.Status == ReclamationStatus.Open)?.Count ?? 0,
            InProgressReclamations = statusCounts.Where(x => x.Status is ReclamationStatus.Assigned or ReclamationStatus.Planned or ReclamationStatus.InProgress).Sum(x => x.Count),
            ResolvedReclamations = statusCounts.FirstOrDefault(x => x.Status == ReclamationStatus.Resolved)?.Count ?? 0,
            ClosedReclamations = statusCounts.FirstOrDefault(x => x.Status == ReclamationStatus.Closed)?.Count ?? 0,
            ByStatus = statusCounts.OrderBy(x => x.Status).ToList(),
            Trend = trend
        };
    }

    public async Task<List<SavAgentStatsDto>> GetSavAgentStatisticsAsync()
    {
        var rows = await _reclamationRepository.Query()
            .Where(r => r.ClaimedBySavId.HasValue || r.SAVId.HasValue)
            .Select(r => new
            {
                UserId = r.ClaimedBySavId ?? r.SAVId ?? 0,
                FullName = r.ClaimedBySavName ?? r.SAVName,
                r.Status,
                r.UpdatedAt,
                r.ClaimedAt,
                r.AssignedAt,
                r.ResolvedAt,
                r.ClosedAt
            })
            .ToListAsync();

        return rows
            .GroupBy(r => r.UserId)
            .Select(g =>
            {
                var assigned = g.Count();
                var resolved = g.Count(r => r.Status == ReclamationStatus.Resolved);
                var closed = g.Count(r => r.Status == ReclamationStatus.Closed);
                var handled = g.Count(r => r.Status != ReclamationStatus.Open);
                var lastActivity = g
                    .Select(r => r.ClosedAt ?? r.ResolvedAt ?? r.UpdatedAt)
                    .DefaultIfEmpty()
                    .Max();

                return new SavAgentStatsDto
                {
                    UserId = g.Key,
                    FullName = g.Select(r => r.FullName).FirstOrDefault(name => !string.IsNullOrWhiteSpace(name)) ?? $"SAV #{g.Key}",
                    Email = string.Empty,
                    AssignedCount = assigned,
                    HandledCount = handled,
                    ResolvedCount = resolved,
                    ClosedCount = closed,
                    ResolutionRate = assigned == 0 ? 0 : Math.Round((decimal)(resolved + closed) * 100 / assigned, 1),
                    LastActivityAt = lastActivity
                };
            })
            .OrderByDescending(x => x.HandledCount)
            .ThenBy(x => x.FullName)
            .ToList();
    }
}
