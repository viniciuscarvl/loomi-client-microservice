using ClientMicroservice.Domain.Abstractions;

namespace ClientMicroservice.Domain.Events;

public record ClientCreatedDomainEvent(
    Guid ClientId,
    string Name,
    string Email,
    DateTimeOffset CreatedAt) : IDomainEvent;
