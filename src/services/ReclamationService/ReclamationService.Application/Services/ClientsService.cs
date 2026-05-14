using ReclamationService.Application.DTOs;
using ReclamationService.Application.Exceptions;
using ReclamationService.Domain.Entities;
using ReclamationService.Domain.Interfaces;

namespace ReclamationService.Application.Services;

public class ClientsService
{
    private readonly IClientRepository _clientRepository;

    public ClientsService(IClientRepository clientRepository)
    {
        _clientRepository = clientRepository;
    }

    public List<ClientDto> GetAll()
    {
        return _clientRepository.GetAll().Select(ToDto).ToList();
    }

    public ClientDto GetById(long id)
    {
        var client = _clientRepository.GetById(id) ?? throw new NotFoundException($"Client {id} was not found.");
        return ToDto(client);
    }

    public ClientDto Create(CreateClientDto dto)
    {
        var normalizedEmail = NormalizeEmail(dto.Email);
        if (_clientRepository.GetByEmail(normalizedEmail) is not null)
        {
            throw new BadRequestException($"A client with email {normalizedEmail} already exists.");
        }

        var id = dto.Id.GetValueOrDefault();
        if (id <= 0)
        {
            id = _clientRepository.GetNextId();
        }

        var client = new Client(id, dto.FullName.Trim(), normalizedEmail, dto.PhoneNumber.Trim());
        _clientRepository.Add(client);
        return ToDto(client);
    }

    public ClientDto Update(long id, UpdateClientDto dto)
    {
        var client = _clientRepository.GetById(id) ?? throw new NotFoundException($"Client {id} was not found.");
        client.FullName = dto.FullName.Trim();
        client.Email = NormalizeEmail(dto.Email);
        client.PhoneNumber = dto.PhoneNumber.Trim();
        _clientRepository.Update(client);
        return ToDto(client);
    }

    private static string NormalizeEmail(string email)
    {
        return (email ?? string.Empty).Trim().ToLowerInvariant();
    }

    private static ClientDto ToDto(Client client)
    {
        return new ClientDto
        {
            Id = client.Id,
            FullName = client.FullName,
            Email = client.Email,
            PhoneNumber = client.PhoneNumber,
            CreatedAt = client.CreatedAt
        };
    }
}
