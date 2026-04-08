using ReclamationService.Application.DTOs;
using ReclamationService.Domain.Entities;

namespace ReclamationService.Application.Mappers;

public static class ReclamationHistoryMapper
{
    public static ReclamationHistoryDto ToDto(this ReclamationHistory item)
    {
        if (item == null)
        {
            return null!;
        }

        return new ReclamationHistoryDto
        {
            Id = item.Id,
            FromStatus = item.FromStatus,
            ToStatus = item.ToStatus,
            ActorUserId = item.ActorUserId,
            ActorRole = item.ActorRole,
            Comment = item.Comment,
            OccurredAt = item.OccurredAt
        };
    }
}
