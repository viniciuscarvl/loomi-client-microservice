using Microsoft.EntityFrameworkCore;
using ClientMicroservice.Domain.Abstractions;
using ClientMicroservice.Domain.Entities;

namespace ClientMicroservice.Infrastructure.Persistence.Repositories;

internal sealed class UserRepository(ApplicationDbContext context)
    : Repository<User>(context), IUserRepository
{
    private readonly ApplicationDbContext _context = context;

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        => _context.Set<User>()
            .FirstOrDefaultAsync(u => u.Email == email, ct);
}
