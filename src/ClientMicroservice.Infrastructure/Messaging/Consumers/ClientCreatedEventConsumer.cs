using MassTransit;
using Microsoft.Extensions.Logging;
using ClientMicroservice.Contracts.Clients;

namespace ClientMicroservice.Infrastructure.Messaging.Consumers;

public sealed class ClientCreatedEventConsumer(ILogger<ClientCreatedEventConsumer> logger)
    : IConsumer<ClientCreatedEvent>
{
    public Task Consume(ConsumeContext<ClientCreatedEvent> context)
    {
        logger.LogInformation(
            "Received ClientCreatedEvent for ClientId={ClientId}, Name={Name}",
            context.Message.ClientId,
            context.Message.Name);

        return Task.CompletedTask;
    }
}
