using ClientMicroservice.Domain.ValueObjects;

namespace ClientMicroservice.Domain.Entities;

public sealed class Client
{
    private Client() { }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public Address Address { get; private set; } = null!;
    public string? ProfilePictureUrl { get; private set; }
    public BankingDetails BankingDetails { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }

    public static Client Create(
        string name,
        string email,
        Address address,
        BankingDetails bankingDetails) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Email = email,
        Address = address,
        BankingDetails = bankingDetails,
        CreatedAt = DateTimeOffset.UtcNow
    };

    public void Update(string? name, string? email, Address? address, BankingDetails? bankingDetails)
    {
        if (name is not null) Name = name;
        if (email is not null) Email = email;
        if (address is not null) Address = address;
        if (bankingDetails is not null) BankingDetails = bankingDetails;
    }

    public void SetProfilePicture(string url) => ProfilePictureUrl = url;
}
