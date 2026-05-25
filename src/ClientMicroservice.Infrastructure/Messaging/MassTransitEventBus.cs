using MassTransit;
using ClientMicroservice.Contracts.Users;
using ClientMicroservice.Domain.Abstractions;
using ClientMicroservice.Domain.Events;

namespace ClientMicroservice.Infrastructure.Messaging;

internal sealed class MassTransitEventBus(IPublishEndpoint endpoint) : IEventBus
{
    public Task PublishAsync<T>(T message, CancellationToken ct = default) where T : class, IDomainEvent
        => message switch
        {
            UserCreatedDomainEvent e => endpoint.Publish(
                new UserCreatedEvent(e.UserId, e.Name, e.Email, e.CreatedAt), ct),
            UserUpdatedDomainEvent e => endpoint.Publish(
                new UserUpdatedEvent(e.UserId, e.Name, e.Email), ct),
            UserDeletedDomainEvent e => endpoint.Publish(
                new UserDeletedEvent(e.UserId), ct),
            _ => endpoint.Publish(message, ct)
        };
}
