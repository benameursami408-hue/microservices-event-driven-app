using MassTransit;
using ReclamationService.Application.DTOs;
using ReclamationService.Application.Exceptions;
using ReclamationService.Application.Mappers;
using ReclamationService.Domain.Enums;
using ReclamationService.Domain.Interfaces;
using SharedEvents.Events;

namespace ReclamationService.Application.Services;

public class ReclamationsService
{
    private readonly IReclamationRepository _reclamationRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IPublishEndpoint _publishEndpoint;

    public ReclamationsService(
        IReclamationRepository reclamationRepository,
        IClientRepository clientRepository,
        IPublishEndpoint publishEndpoint)
    {
        _reclamationRepository = reclamationRepository;
        _clientRepository = clientRepository;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<ReclamationDto> CreateAsync(CreateReclamationDto dto)
    {
        var reclamation = dto.ToEntity();
        var created = _reclamationRepository.Create(reclamation);

        var clientEmail = _clientRepository.GetById(created.ClientId)?.Email ?? string.Empty;

        await _publishEndpoint.Publish(new ReclamationCreatedEvent
        {
            ReclamationId = created.Id,
            Reference = created.Reference,
            ClientId = created.ClientId,
            ClientName = created.ClientName,
            ClientEmail = clientEmail,
            Priority = created.Priority.ToString(),
            Status = created.Status,
            Description = created.Description,
            OccurredAt = DateTime.UtcNow
        });

        return created.ToDto();
    }

    public void Delete(long id)
    {
        var existing = _reclamationRepository.GetById(id);
        if (existing == null)
            throw new NotFoundException($"Reclamation with id {id} not found.");

        _reclamationRepository.Delete(id);
    }

    public List<ReclamationDto> GetAll()
    {
        return _reclamationRepository.GetAll()
            .Select(r => r.ToDto())
            .ToList();
    }

    public ReclamationDto GetById(long id)
    {
        var reclamation = _reclamationRepository.GetById(id);
        if (reclamation == null)
            throw new NotFoundException($"Reclamation with id {id} not found.");

        return reclamation.ToDto();
    }

    public List<ReclamationDto> GetByPriority(NamePriority priority)
    {
        return _reclamationRepository.GetByPriority(priority)
            .Select(r => r.ToDto())
            .ToList();
    }

    public ReclamationDto GetByReference(string reference)
    {
        var reclamation = _reclamationRepository.GetByRefernce(reference);
        if (reclamation == null)
            throw new NotFoundException($"Reclamation with reference '{reference}' not found.");

        return reclamation.ToDto();
    }

    public ReclamationDto Update(long id, UpdateReclamationDto dto)
    {
        var existing = _reclamationRepository.GetById(id);
        if (existing == null)
            throw new NotFoundException($"Reclamation with id {id} not found.");

        existing.ApplyUpdate(dto);
        var updated = _reclamationRepository.Update(existing);
        return updated.ToDto();
    }
}
