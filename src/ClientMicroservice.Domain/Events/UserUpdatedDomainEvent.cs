using ClientMicroservice.Domain.Abstractions;

namespace ClientMicroservice.Domain.Events;

public record UserUpdatedDomainEvent(Guid UserId, string Name, string Email) : IDomainEvent;
