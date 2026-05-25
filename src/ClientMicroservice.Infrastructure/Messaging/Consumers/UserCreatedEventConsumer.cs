using MassTransit;
using Microsoft.Extensions.Logging;
using ClientMicroservice.Contracts.Users;

namespace ClientMicroservice.Infrastructure.Messaging.Consumers;

public sealed class UserCreatedEventConsumer(ILogger<UserCreatedEventConsumer> logger)
    : IConsumer<UserCreatedEvent>
{
    public Task Consume(ConsumeContext<UserCreatedEvent> context)
    {
        logger.LogInformation(
            "Received UserCreatedEvent for UserId={UserId}, Name={Name}",
            context.Message.UserId,
            context.Message.Name);

        // TODO: add your inbound event handling logic here
        return Task.CompletedTask;
    }
}
