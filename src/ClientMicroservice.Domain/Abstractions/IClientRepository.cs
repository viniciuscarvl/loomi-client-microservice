using ClientMicroservice.Domain.Entities;

namespace ClientMicroservice.Domain.Abstractions;

public interface IClientRepository : IRepository<Client>
{
    Task<Client?> GetByEmailAsync(string email, CancellationToken ct = default);
}
