namespace ClientMicroservice.Contracts.Users;

public record UserUpdatedEvent(Guid UserId, string Name, string Email);
