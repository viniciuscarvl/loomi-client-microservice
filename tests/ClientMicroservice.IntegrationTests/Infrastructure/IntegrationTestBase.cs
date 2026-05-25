using Microsoft.EntityFrameworkCore;
using ClientMicroservice.Infrastructure.Persistence;

namespace ClientMicroservice.IntegrationTests.Infrastructure;

[Collection("Integration")]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture;

    protected IntegrationTestBase(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => _fixture.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    internal ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;
        return new ApplicationDbContext(options);
    }
}
