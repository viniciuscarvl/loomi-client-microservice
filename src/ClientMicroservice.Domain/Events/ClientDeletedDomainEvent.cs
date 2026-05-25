using ClientMicroservice.Domain.Abstractions;

namespace ClientMicroservice.Domain.Events;

public record ClientDeletedDomainEvent(Guid ClientId) : IDomainEvent;
