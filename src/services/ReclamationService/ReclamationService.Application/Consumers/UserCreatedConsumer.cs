using MassTransit;
using SharedEvents.Events;
using ReclamationService.Domain.Entities;
using ReclamationService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ReclamationService.Application.Consumers
{
    public class UserCreatedConsumer : IConsumer<UserCreatedEvent>
    {
        private readonly IClientRepository _clientRepository;
        private readonly ILogger<UserCreatedConsumer> _logger;

        public UserCreatedConsumer(IClientRepository clientRepository, ILogger<UserCreatedConsumer> logger)
        {
            _clientRepository = clientRepository;
            _logger = logger;
        }

        public Task Consume(ConsumeContext<UserCreatedEvent> context)
        {
            var message = context.Message;
            _logger.LogInformation("Receiving UserCreatedEvent for UserId: {UserId}, Email: {Email}", message.UserId, message.Email);

            try
            {
                var existingClient = _clientRepository.GetById(message.UserId);

                if (existingClient == null)
                {
                    // Create new client mapping to the newly registered user
                    var newClient = new Client(
                        id: message.UserId,
                        fullName: $"{message.FirstName} {message.LastName}".Trim(),
                        email: message.Email,
                        phoneNumber: message.PhoneNumber
                    );
                    _clientRepository.Add(newClient);
                    _logger.LogInformation("Successfully registered internal Client for UserId: {UserId}", message.UserId);
                }
                else
                {
                    _logger.LogInformation("Client with ID {UserId} already exists. Skipping insertion.", message.UserId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing UserCreatedEvent for UserId: {UserId}", message.UserId);
                throw; // Rethrow lets MassTransit retry or move to error queue
            }

            return Task.CompletedTask;
        }
    }
}
