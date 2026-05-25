namespace ClientMicroservice.Contracts.Users;

public record UserCreatedEvent(Guid UserId, string Name, string Email, DateTimeOffset CreatedAt);
