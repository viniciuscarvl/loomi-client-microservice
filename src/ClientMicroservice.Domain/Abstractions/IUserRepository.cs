using ClientMicroservice.Domain.Entities;

namespace ClientMicroservice.Domain.Abstractions;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
}
