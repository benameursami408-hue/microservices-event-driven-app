using ReclamationService.Application.Outbox;
using ReclamationService.Application.DTOs;
using ReclamationService.Application.Exceptions;
using ReclamationService.Application.Mappers;
using ReclamationService.Application.Security;
using ReclamationService.Domain.Entities;
using ReclamationService.Domain.Enums;
using ReclamationService.Domain.Interfaces;
using SharedEvents.Events;
using System.Text.Json;

namespace ReclamationService.Application.Services;

public partial class ReclamationsService
{
    private readonly IReclamationRepository _reclamationRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IReclamationHistoryRepository _historyRepository;
    private readonly IOutboxWriter _outboxWriter;
    private readonly TicketClassificationService _classificationService;
    private readonly ReclamationPriorityService _priorityService;
    private readonly ReclamationSlaService _slaService;

    public ReclamationsService(
        IReclamationRepository reclamationRepository,
        IClientRepository clientRepository,
        IReclamationHistoryRepository historyRepository,
        IOutboxWriter outboxWriter,
        TicketClassificationService classificationService,
        ReclamationPriorityService priorityService,
        ReclamationSlaService slaService)
    {
        _reclamationRepository = reclamationRepository;
        _clientRepository = clientRepository;
        _historyRepository = historyRepository;
        _outboxWriter = outboxWriter;
        _classificationService = classificationService;
        _priorityService = priorityService;
        _slaService = slaService;
    }

    public async Task<ReclamationDto> CreateAsync(CreateReclamationDto dto, CurrentUser actor)
    {
        EnsureRole(actor, "CLIENT", "SAV", "ADMIN");

        var role = NormalizeRole(actor.Role);
        var clientId = actor.UserId;
        var clientName = string.IsNullOrWhiteSpace(actor.FullName) ? (actor.Email ?? string.Empty) : actor.FullName;

        if (role is "SAV" or "ADMIN")
        {
            if (!dto.ClientId.HasValue || dto.ClientId.Value <= 0)
            {
                throw new BadRequestException("Client is required.");
            }

            var selectedClient = _clientRepository.GetById(dto.ClientId.Value)
                ?? throw new BadRequestException("Selected client was not found.");
            clientId = selectedClient.Id;
            clientName = selectedClient.FullName;
        }

        var reclamation = dto.ToEntity(clientId, clientName);
        var initialSnapshot = CaptureOperationalState(reclamation);
        ApplyDerivedState(reclamation);
        var created = _reclamationRepository.Create(reclamation);

        var clientEmail = actor.Email;
        if (string.IsNullOrWhiteSpace(clientEmail))
        {
            clientEmail = _clientRepository.GetById(created.ClientId)?.Email ?? string.Empty;
        }

        _historyRepository.Add(new ReclamationHistory
        {
            ReclamationId = created.Id,
            FromStatus = ReclamationStatus.Open,
            ToStatus = ReclamationStatus.Open,
            ActorUserId = actor.UserId,
            ActorRole = NormalizeRole(actor.Role),
            Comment = "Created",
            OccurredAt = DateTime.UtcNow
        });

        await _outboxWriter.EnqueueAsync(new ReclamationCreatedEvent
        {
            CorrelationId = actor.CorrelationId,
            ReclamationId = created.Id,
            Reference = created.Reference,
            ClientId = created.ClientId,
            ClientName = created.ClientName,
            ClientEmail = clientEmail,
            Priority = created.Priority.ToString(),
            Status = ToStatusCode(created.Status),
            Description = created.Description,
            OccurredAt = DateTime.UtcNow
        });

        await QueueOperationalEventsAsync(created, initialSnapshot, actor.CorrelationId, actor.UserId, NormalizeRole(actor.Role));

        return ToDtoWithActions(created, actor);
    }

}
