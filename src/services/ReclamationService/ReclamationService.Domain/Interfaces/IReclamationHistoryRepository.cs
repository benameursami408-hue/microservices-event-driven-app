using ReclamationService.Domain.Entities;

namespace ReclamationService.Domain.Interfaces;

public interface IReclamationHistoryRepository
{
    List<ReclamationHistory> GetByReclamationId(long reclamationId);
    ReclamationHistory Add(ReclamationHistory item);
}
