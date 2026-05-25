using Microsoft.EntityFrameworkCore;
using ClientMicroservice.Domain.Entities;
using ClientMicroservice.Infrastructure.Persistence;
using ClientMicroservice.Infrastructure.Persistence.Repositories;
using ClientMicroservice.IntegrationTests.Infrastructure;

namespace ClientMicroservice.IntegrationTests.Users;

public sealed class UserRepositoryTests(PostgresContainerFixture fixture)
    : IntegrationTestBase(fixture)
{
    private async Task<User> SeedUserAsync(string name = "Alice", string email = "alice@example.com")
    {
        await using var ctx = CreateDbContext();
        var repo = new UserRepository(ctx);
        var uow = new UnitOfWork(ctx);
        var user = User.Create(name, email);
        await repo.AddAsync(user);
        await uow.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task AddAsync_PersistsUser()
    {
        var user = User.Create("Alice", "alice@example.com");

        await using var ctx = CreateDbContext();
        var repo = new UserRepository(ctx);
        var uow = new UnitOfWork(ctx);
        await repo.AddAsync(user);
        await uow.SaveChangesAsync();

        await using var verifyCtx = CreateDbContext();
        var found = await verifyCtx.Users.FindAsync(user.Id);
        Assert.NotNull(found);
        Assert.Equal(user.Id, found.Id);
        Assert.Equal("Alice", found.Name);
        Assert.Equal("alice@example.com", found.Email);
        Assert.NotEqual(default, found.CreatedAt);
    }

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsUser()
    {
        var seeded = await SeedUserAsync();

        await using var ctx = CreateDbContext();
        var repo = new UserRepository(ctx);
        var found = await repo.GetByIdAsync(seeded.Id);

        Assert.NotNull(found);
        Assert.Equal(seeded.Id, found.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotExists_ReturnsNull()
    {
        await using var ctx = CreateDbContext();
        var repo = new UserRepository(ctx);
        var found = await repo.GetByIdAsync(Guid.NewGuid());

        Assert.Null(found);
    }

    [Fact]
    public async Task GetByEmailAsync_WhenExists_ReturnsUser()
    {
        var seeded = await SeedUserAsync(email: "find@example.com");

        await using var ctx = CreateDbContext();
        var repo = new UserRepository(ctx);
        var found = await repo.GetByEmailAsync("find@example.com");

        Assert.NotNull(found);
        Assert.Equal(seeded.Id, found.Id);
    }

    [Fact]
    public async Task GetByEmailAsync_WhenNotExists_ReturnsNull()
    {
        await using var ctx = CreateDbContext();
        var repo = new UserRepository(ctx);
        var found = await repo.GetByEmailAsync("nobody@example.com");

        Assert.Null(found);
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsPaginatedResults()
    {
        for (var i = 1; i <= 5; i++)
            await SeedUserAsync($"User{i}", $"user{i}@example.com");

        await using var ctx = CreateDbContext();
        var repo = new UserRepository(ctx);
        var paged = await repo.GetPagedAsync(pageNumber: 1, pageSize: 3);

        Assert.Equal(3, paged.Items.Count);
        Assert.Equal(5, paged.TotalCount);
        Assert.Equal(2, paged.TotalPages);
        Assert.True(paged.HasNextPage);
        Assert.False(paged.HasPreviousPage);
    }

    [Fact]
    public async Task Update_PersistsChanges()
    {
        var seeded = await SeedUserAsync("OldName", "old@example.com");

        await using var ctx = CreateDbContext();
        var repo = new UserRepository(ctx);
        var uow = new UnitOfWork(ctx);
        var user = await repo.GetByIdAsync(seeded.Id);
        user!.Update("NewName", "new@example.com");
        repo.Update(user);
        await uow.SaveChangesAsync();

        await using var verifyCtx = CreateDbContext();
        var updated = await verifyCtx.Users.FindAsync(seeded.Id);
        Assert.NotNull(updated);
        Assert.Equal("NewName", updated.Name);
        Assert.Equal("new@example.com", updated.Email);
    }

    [Fact]
    public async Task Delete_RemovesUser()
    {
        var seeded = await SeedUserAsync();

        await using var ctx = CreateDbContext();
        var repo = new UserRepository(ctx);
        var uow = new UnitOfWork(ctx);
        var user = await repo.GetByIdAsync(seeded.Id);
        repo.Delete(user!);
        await uow.SaveChangesAsync();

        await using var verifyCtx = CreateDbContext();
        var found = await verifyCtx.Users.FindAsync(seeded.Id);
        Assert.Null(found);
    }

    [Fact]
    public async Task AddAsync_WithDuplicateEmail_ThrowsOnSaveChanges()
    {
        await SeedUserAsync(email: "dup@example.com");

        var user2 = User.Create("Bob", "dup@example.com");
        await using var ctx = CreateDbContext();
        var repo = new UserRepository(ctx);
        var uow = new UnitOfWork(ctx);
        await repo.AddAsync(user2);

        await Assert.ThrowsAsync<DbUpdateException>(() => uow.SaveChangesAsync());
    }
}
