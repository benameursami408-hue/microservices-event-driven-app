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
                TechnicianName = r.TechnicianName
            })
            .ToListAsync();

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
            Latest = latestItems
        };
    }
}
