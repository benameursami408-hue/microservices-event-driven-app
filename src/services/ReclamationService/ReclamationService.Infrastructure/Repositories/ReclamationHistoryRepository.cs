using Microsoft.EntityFrameworkCore;
using ReclamationService.Domain.Entities;
using ReclamationService.Domain.Interfaces;
using ReclamationService.Infrastructure.Data;

namespace ReclamationService.Infrastructure.Repositories;

public class ReclamationHistoryRepository : IReclamationHistoryRepository
{
    private readonly AppDbContext _context;

    public ReclamationHistoryRepository(AppDbContext context)
    {
        _context = context;
    }

    public List<ReclamationHistory> GetByReclamationId(long reclamationId)
    {
        return _context.ReclamationHistories
            .AsNoTracking()
            .Where(h => h.ReclamationId == reclamationId)
            .OrderBy(h => h.OccurredAt)
            .ThenBy(h => h.Id)
            .ToList();
    }

    public ReclamationHistory Add(ReclamationHistory item)
    {
        _context.ReclamationHistories.Add(item);
        _context.SaveChanges();
        return item;
    }
}
