namespace ClientMicroservice.Contracts.Clients;

public record ClientCreatedEvent(Guid ClientId, string Name, string Email, DateTimeOffset CreatedAt);
