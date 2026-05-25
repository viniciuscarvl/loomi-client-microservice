using MassTransit;
using ClientMicroservice.Contracts.Clients;
using ClientMicroservice.Domain.Abstractions;
using ClientMicroservice.Domain.Events;

namespace ClientMicroservice.Infrastructure.Messaging;

internal sealed class MassTransitEventBus(IPublishEndpoint endpoint) : IEventBus
{
    public Task PublishAsync<T>(T message, CancellationToken ct = default) where T : class, IDomainEvent
        => message switch
        {
            ClientCreatedDomainEvent e => endpoint.Publish(
                new ClientCreatedEvent(e.ClientId, e.Name, e.Email, e.CreatedAt), ct),
            ClientUpdatedDomainEvent e => endpoint.Publish(
                new ClientUpdatedEvent(e.ClientId), ct),
            ClientDeletedDomainEvent e => endpoint.Publish(
                new ClientDeletedEvent(e.ClientId), ct),
            _ => endpoint.Publish(message, ct)
        };
}
