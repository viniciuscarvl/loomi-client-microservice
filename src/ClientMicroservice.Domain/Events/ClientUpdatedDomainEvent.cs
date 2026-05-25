using ClientMicroservice.Domain.Abstractions;

namespace ClientMicroservice.Domain.Events;

public record ClientUpdatedDomainEvent(Guid ClientId) : IDomainEvent;
