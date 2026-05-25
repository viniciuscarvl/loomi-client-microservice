using Microsoft.EntityFrameworkCore;
using ClientMicroservice.Domain.Abstractions;
using ClientMicroservice.Domain.Entities;

namespace ClientMicroservice.Infrastructure.Persistence.Repositories;

internal sealed class ClientRepository(ApplicationDbContext context)
    : Repository<Client>(context), IClientRepository
{
    private readonly ApplicationDbContext _context = context;

    public Task<Client?> GetByEmailAsync(string email, CancellationToken ct = default)
        => _context.Set<Client>()
            .FirstOrDefaultAsync(c => c.Email == email, ct);
}
