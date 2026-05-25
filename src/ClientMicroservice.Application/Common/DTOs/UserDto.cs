namespace ClientMicroservice.Application.Common.DTOs;

public record UserDto(Guid Id, string Name, string Email, DateTimeOffset CreatedAt);
