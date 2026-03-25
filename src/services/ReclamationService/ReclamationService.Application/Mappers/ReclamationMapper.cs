using ReclamationService.Application.DTOs;
using ReclamationService.Domain.Entities;

namespace ReclamationService.Application.Mappers;

public static class ReclamationMapper
{
    public static ReclamationDto ToDto(this Reclamation reclamation)
    {
        if (reclamation == null)
        {
            return null!;
        }

        return new ReclamationDto
        {
            Id = reclamation.Id,
            Reference = reclamation.Reference,
            Description = reclamation.Description,
            Priority = reclamation.Priority,
            Status = reclamation.Status,
            CreatedAt = reclamation.CreatedAt,
            UpdatedAt = reclamation.UpdatedAt,
            ClientId = reclamation.ClientId,
            ClientName = reclamation.ClientName,
            SAVId = reclamation.SAVId,
            SAVName = reclamation.SAVName
        };
    }

    public static Reclamation ToEntity(this CreateReclamationDto dto)
    {
        if (dto == null)
        {
            return null!;
        }

        return new Reclamation
        {
            Reference = GenerateReference(),
            Description = dto.Description,
            Priority = dto.Priority,
            Status = "Ouverte",
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            ClientId = dto.ClientId,
            ClientName = dto.ClientName,
            SAVId = dto.SAVId,
            SAVName = dto.SAVName
        };
    }

    public static void ApplyUpdate(this Reclamation reclamation, UpdateReclamationDto dto)
    {
        reclamation.Description = dto.Description;
        reclamation.Priority = dto.Priority;
        reclamation.Status = dto.Status;
        reclamation.UpdatedAt = DateTime.Now;
    }

    private static string GenerateReference()
    {
        return $"REC-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";
    }
}
