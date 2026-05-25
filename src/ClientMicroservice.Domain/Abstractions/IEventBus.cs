namespace ClientMicroservice.Domain.Abstractions;

public interface IEventBus
{
    Task PublishAsync<T>(T message, CancellationToken ct = default) where T : class, IDomainEvent;
}
