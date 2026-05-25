namespace ClientMicroservice.Domain.Entities;

public sealed class User
{
    private User() { }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }

    public static User Create(string name, string email) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Email = email,
        CreatedAt = DateTimeOffset.UtcNow
    };

    public void Update(string name, string email)
    {
        Name = name;
        Email = email;
    }
}
