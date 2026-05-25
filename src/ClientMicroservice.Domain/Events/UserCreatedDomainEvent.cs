using ClientMicroservice.Domain.Abstractions;

namespace ClientMicroservice.Domain.Events;

public record UserCreatedDomainEvent(Guid UserId, string Name, string Email, DateTimeOffset CreatedAt) : IDomainEvent;
