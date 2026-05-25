namespace ClientMicroservice.Application.Common.DTOs;

public record ClientDto(
    Guid Id,
    string Name,
    string Email,
    AddressDto Address,
    string? ProfilePictureUrl,
    BankingDetailsDto BankingDetails,
    DateTimeOffset CreatedAt);

public record AddressDto(string Street, string City, string State, string ZipCode, string Country);

public record BankingDetailsDto(string Agency, string AccountNumber);
