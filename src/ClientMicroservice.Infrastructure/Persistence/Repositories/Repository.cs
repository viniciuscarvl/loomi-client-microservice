using Microsoft.EntityFrameworkCore;
using ClientMicroservice.Domain.Abstractions;
using ClientMicroservice.Domain.Common;

namespace ClientMicroservice.Infrastructure.Persistence.Repositories;

internal class Repository<T>(ApplicationDbContext context) : IRepository<T>
    where T : class
{
    public Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => context.Set<T>().FindAsync([id], ct).AsTask();

    public async Task<PagedList<T>> GetPagedAsync(
        int pageNumber, int pageSize, CancellationToken ct = default)
    {
        var total = await context.Set<T>().CountAsync(ct);
        var items = await context.Set<T>()
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        return new PagedList<T>(items, pageNumber, pageSize, total);
    }

    public Task AddAsync(T entity, CancellationToken ct = default)
        => context.AddAsync(entity, ct).AsTask();

    public void Update(T entity)
        => context.Update(entity);

    public void Delete(T entity)
        => context.Remove(entity);
}
