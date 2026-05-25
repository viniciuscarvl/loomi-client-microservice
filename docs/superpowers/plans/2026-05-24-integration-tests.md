# Integration Tests Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a `ClientMicroservice.IntegrationTests` project that tests the persistence layer (UserRepository, Repository<T>, UnitOfWork) against a real PostgreSQL database running in a Testcontainers container.

**Architecture:** A single shared Postgres container is started once per test run via xUnit `ICollectionFixture<PostgresContainerFixture>`. Migrations are applied on startup. Respawn resets data between each test method via `IAsyncLifetime.InitializeAsync` in the base class. Each test creates its own `ApplicationDbContext` scope to avoid EF change tracker pollution.

**Tech Stack:** Testcontainers.PostgreSql, Respawn 6, xUnit 2, .NET 10, EF Core 10 + Npgsql

---

## File Map

```
src/ClientMicroservice.Infrastructure/ClientMicroservice.Infrastructure.csproj   ← add InternalsVisibleTo

tests/
  ClientMicroservice.IntegrationTests/
    ClientMicroservice.IntegrationTests.csproj
    Infrastructure/
      PostgresContainerFixture.cs    ← starts container, runs migrations, owns Respawner
      IntegrationTestCollection.cs   ← [CollectionDefinition("Integration")]
      IntegrationTestBase.cs         ← abstract base, resets DB before each test
    Users/
      UserRepositoryTests.cs         ← 9 tests covering full CRUD + constraint

ClientMicroservice.slnx            ← add new project entry
```

---

## Task 1: Project scaffold

**Files:**
- Modify: `src/ClientMicroservice.Infrastructure/ClientMicroservice.Infrastructure.csproj`
- Create: `tests/ClientMicroservice.IntegrationTests/ClientMicroservice.IntegrationTests.csproj`
- Modify: `ClientMicroservice.slnx`

- [ ] **Step 1.1: Add InternalsVisibleTo to Infrastructure csproj**

`ApplicationDbContext`, `UserRepository`, and `UnitOfWork` are `internal`. Add visibility to the test project at the end of `src/ClientMicroservice.Infrastructure/ClientMicroservice.Infrastructure.csproj`, before the closing `</Project>`:

```xml
  <ItemGroup>
    <InternalsVisibleTo Include="ClientMicroservice.IntegrationTests" />
  </ItemGroup>
```

Final file:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <ItemGroup>
    <ProjectReference Include="..\ClientMicroservice.Application\ClientMicroservice.Application.csproj" />
    <ProjectReference Include="..\ClientMicroservice.Domain\ClientMicroservice.Domain.csproj" />
    <ProjectReference Include="..\ClientMicroservice.Contracts\ClientMicroservice.Contracts.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="MassTransit" Version="9.1.1" />
    <PackageReference Include="MassTransit.RabbitMQ" Version="9.1.1" />
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.8" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.8">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.Extensions.Configuration.Abstractions" Version="10.0.8" />
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.1" />
  </ItemGroup>

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <InternalsVisibleTo Include="ClientMicroservice.IntegrationTests" />
  </ItemGroup>

</Project>
```

- [ ] **Step 1.2: Create `ClientMicroservice.IntegrationTests.csproj`**

`tests/ClientMicroservice.IntegrationTests/ClientMicroservice.IntegrationTests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="coverlet.collector" Version="6.0.4" />
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.8" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.5.1" />
    <PackageReference Include="Respawn" Version="6.2.1" />
    <PackageReference Include="Testcontainers.PostgreSql" Version="3.10.0" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.4" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\ClientMicroservice.Infrastructure\ClientMicroservice.Infrastructure.csproj" />
    <ProjectReference Include="..\..\src\ClientMicroservice.Domain\ClientMicroservice.Domain.csproj" />
  </ItemGroup>

</Project>
```

> **Note on versions:** If `Testcontainers.PostgreSql 3.10.0` or `Respawn 6.2.1` are not found on restore, run `dotnet add package Testcontainers.PostgreSql` and `dotnet add package Respawn` from the project folder to get the latest stable version and update the versions accordingly.

- [ ] **Step 1.3: Add project to `ClientMicroservice.slnx`**

`ClientMicroservice.slnx` — add the new project entry:

```xml
<Solution>
  <Project Path="src/ClientMicroservice.Domain/ClientMicroservice.Domain.csproj" />
  <Project Path="src/ClientMicroservice.Application/ClientMicroservice.Application.csproj" />
  <Project Path="src/ClientMicroservice.Infrastructure/ClientMicroservice.Infrastructure.csproj" />
  <Project Path="src/ClientMicroservice.Contracts/ClientMicroservice.Contracts.csproj" />
  <Project Path="src/ClientMicroservice.API/ClientMicroservice.API.csproj" />
  <Project Path="tests/ClientMicroservice.UnitTests/ClientMicroservice.UnitTests.csproj" />
  <Project Path="tests/ClientMicroservice.IntegrationTests/ClientMicroservice.IntegrationTests.csproj" />
</Solution>
```

- [ ] **Step 1.4: Restore and build**

```bash
dotnet restore ClientMicroservice.slnx
dotnet build tests/ClientMicroservice.IntegrationTests/ClientMicroservice.IntegrationTests.csproj
```

Expected: `Build succeeded` (project has no source files yet — that's fine).

- [ ] **Step 1.5: Commit**

```bash
git add src/ClientMicroservice.Infrastructure/ClientMicroservice.Infrastructure.csproj \
        tests/ClientMicroservice.IntegrationTests/ClientMicroservice.IntegrationTests.csproj \
        ClientMicroservice.slnx
git commit -m "feat(tests): scaffold IntegrationTests project with Testcontainers and Respawn"
```

---

## Task 2: Infrastructure fixtures

**Files:**
- Create: `tests/ClientMicroservice.IntegrationTests/Infrastructure/PostgresContainerFixture.cs`
- Create: `tests/ClientMicroservice.IntegrationTests/Infrastructure/IntegrationTestCollection.cs`
- Create: `tests/ClientMicroservice.IntegrationTests/Infrastructure/IntegrationTestBase.cs`

- [ ] **Step 2.1: Create `PostgresContainerFixture.cs`**

`tests/ClientMicroservice.IntegrationTests/Infrastructure/PostgresContainerFixture.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Respawn;
using Testcontainers.PostgreSql;
using ClientMicroservice.Infrastructure.Persistence;

namespace ClientMicroservice.IntegrationTests.Infrastructure;

public sealed class PostgresContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .Build();

    private Respawner _respawner = null!;

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        await using var context = new ApplicationDbContext(options);
        await context.Database.MigrateAsync();

        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"]
        });
    }

    public async Task ResetAsync()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await _respawner.ResetAsync(connection);
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();
}
```

- [ ] **Step 2.2: Create `IntegrationTestCollection.cs`**

`tests/ClientMicroservice.IntegrationTests/Infrastructure/IntegrationTestCollection.cs`:

```csharp
namespace ClientMicroservice.IntegrationTests.Infrastructure;

[CollectionDefinition("Integration")]
public sealed class IntegrationTestCollection : ICollectionFixture<PostgresContainerFixture> { }
```

- [ ] **Step 2.3: Create `IntegrationTestBase.cs`**

`tests/ClientMicroservice.IntegrationTests/Infrastructure/IntegrationTestBase.cs`:

```csharp
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

    protected ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;
        return new ApplicationDbContext(options);
    }
}
```

- [ ] **Step 2.4: Build integration tests project**

```bash
dotnet build tests/ClientMicroservice.IntegrationTests/ClientMicroservice.IntegrationTests.csproj
```

Expected: `Build succeeded, 0 Error(s)`.

- [ ] **Step 2.5: Commit**

```bash
git add tests/ClientMicroservice.IntegrationTests/Infrastructure/
git commit -m "feat(tests): add PostgresContainerFixture, collection, and base class"
```

---

## Task 3: UserRepositoryTests

**Files:**
- Create: `tests/ClientMicroservice.IntegrationTests/Users/UserRepositoryTests.cs`

> **Requires Docker running locally.** If Docker is not available, the build will succeed but the tests will fail at runtime with a container startup error.

- [ ] **Step 3.1: Create `UserRepositoryTests.cs`**

`tests/ClientMicroservice.IntegrationTests/Users/UserRepositoryTests.cs`:

```csharp
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
        Assert.Equal("NewName", updated!.Name);
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
```

- [ ] **Step 3.2: Build**

```bash
dotnet build tests/ClientMicroservice.IntegrationTests/ClientMicroservice.IntegrationTests.csproj
```

Expected: `Build succeeded, 0 Error(s)`.

- [ ] **Step 3.3: Run integration tests (requires Docker)**

```bash
dotnet test tests/ClientMicroservice.IntegrationTests/ -v minimal
```

Expected: `Passed! - Failed: 0, Passed: 9, Skipped: 0, Total: 9`

Container startup takes ~10-20 seconds on first run. Subsequent runs reuse the same container for the collection.

If tests fail with a container error, verify Docker is running: `docker info`.

- [ ] **Step 3.4: Run unit tests to confirm no regressions**

```bash
dotnet test tests/ClientMicroservice.UnitTests/ -v minimal
```

Expected: `Passed! - Failed: 0, Passed: 14, Skipped: 0, Total: 14`

- [ ] **Step 3.5: Commit**

```bash
git add tests/ClientMicroservice.IntegrationTests/Users/
git commit -m "feat(tests): add UserRepository integration tests with Testcontainers and Respawn"
```
