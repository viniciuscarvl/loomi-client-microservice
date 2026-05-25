using ClientMicroservice.Domain.Abstractions;

namespace ClientMicroservice.Domain.Events;

public record UserDeletedDomainEvent(Guid UserId) : IDomainEvent;
