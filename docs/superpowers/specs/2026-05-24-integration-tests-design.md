# Integration Tests — Design Spec

**Date:** 2026-05-24  
**Status:** Approved

---

## 1. Overview

A new `ClientMicroservice.IntegrationTests` project that tests the persistence layer (repositories and Unit of Work) against a real PostgreSQL database running in a Docker container via Testcontainers. Tests are isolated by Respawn, which resets data between each test method.

**Scope:** persistence layer only — `UserRepository`, `Repository<T>`, `UnitOfWork`. No HTTP layer, no MassTransit.

**Fixed technology choices:**
- Testcontainers.PostgreSql — manages the Postgres container lifecycle
- Respawn — resets database state between tests (respects FK order)
- xUnit 2 with `IAsyncLifetime` — async setup/teardown per test
- `ICollectionFixture<>` — one shared container per test run

---

## 2. Solution Structure

```
tests/
  ClientMicroservice.IntegrationTests/
    ClientMicroservice.IntegrationTests.csproj
    Infrastructure/
      PostgresContainerFixture.cs
      IntegrationTestCollection.cs
      IntegrationTestBase.cs
    Users/
      UserRepositoryTests.cs
```

**Project references:** `ClientMicroservice.Infrastructure`, `ClientMicroservice.Domain`  
**Does not reference:** `Application`, `API`, `Contracts`

`ApplicationDbContext`, `UserRepository`, and `UnitOfWork` are `internal` in Infrastructure. The Infrastructure csproj must expose them to the test project via:

```xml
<ItemGroup>
  <InternalsVisibleTo Include="ClientMicroservice.IntegrationTests" />
</ItemGroup>
```

---

## 3. NuGet Packages

| Package | Purpose |
|---|---|
| `Testcontainers.PostgreSql` | Start/stop Postgres container |
| `Respawn` | Reset database data between tests |
| `Microsoft.NET.Test.Sdk` | Test runner SDK |
| `xunit` | Test framework |
| `xunit.runner.visualstudio` | IDE integration |
| `coverlet.collector` | Coverage (consistent with UnitTests project) |

---

## 4. Infrastructure Fixtures

### `PostgresContainerFixture` (`IAsyncLifetime`)

**`InitializeAsync`:**
1. Build and start a `PostgreSqlContainer` using image `postgres:17-alpine` (matches docker-compose)
2. Retrieve the connection string from the running container
3. Create an `ApplicationDbContext` with `UseNpgsql(connectionString)`
4. Apply all pending migrations via `context.Database.MigrateAsync()`
5. Initialize a `Respawner` via `Respawner.CreateAsync(connection)` — uses an open `NpgsqlConnection`

**`DisposeAsync`:** stop and remove the container.

Exposes:
- `string ConnectionString` — used by `IntegrationTestBase` to create `DbContext` instances per test
- `Task ResetAsync(NpgsqlConnection connection)` — delegates to `Respawner.ResetAsync(connection)`

### `IntegrationTestCollection`

```csharp
[CollectionDefinition("Integration")]
public sealed class IntegrationTestCollection
    : ICollectionFixture<PostgresContainerFixture> { }
```

Ensures a single container is shared across all test classes decorated with `[Collection("Integration")]`.

### `IntegrationTestBase` (`IAsyncLifetime`)

Base class for all integration test classes. Receives `PostgresContainerFixture` via constructor.

**`InitializeAsync`:** opens a `NpgsqlConnection` and calls `fixture.ResetAsync(connection)` — ensures each test starts with a clean database.

**`DisposeAsync`:** disposes the connection.

Exposes:
- `ApplicationDbContext CreateDbContext()` — creates a new `DbContext` instance with `NoTracking` not forced (tests control tracking as needed). Each test should create its own scope to avoid EF change tracker pollution.

---

## 5. Test Cases — `UserRepositoryTests`

All tests inherit `IntegrationTestBase` and carry `[Collection("Integration")]`.

### CRUD operations

| Test | Scenario | Assertion |
|---|---|---|
| `AddAsync_PersistsUser` | Create user, `AddAsync`, `SaveChangesAsync`, new context `GetByIdAsync` | All fields match (Id, Name, Email, CreatedAt) |
| `GetByIdAsync_WhenExists_ReturnsUser` | Seed one user, query by Id | Returns entity with correct Id |
| `GetByIdAsync_WhenNotExists_ReturnsNull` | Query by random Guid | Returns `null` |
| `GetByEmailAsync_WhenExists_ReturnsUser` | Seed one user, query by email | Returns entity |
| `GetByEmailAsync_WhenNotExists_ReturnsNull` | Query non-existent email | Returns `null` |
| `GetPagedAsync_ReturnsPaginatedResults` | Seed 5 users, query page 1 size 3 | `Items.Count=3`, `TotalCount=5`, `TotalPages=2`, `HasNextPage=true` |
| `Update_PersistsChanges` | Seed user, call `user.Update(newName, newEmail)`, `repository.Update`, `SaveChangesAsync`, re-fetch | New name and email persisted |
| `Delete_RemovesUser` | Seed user, `repository.Delete`, `SaveChangesAsync`, `GetByIdAsync` | Returns `null` |

### Constraint validation

| Test | Scenario | Assertion |
|---|---|---|
| `AddAsync_WithDuplicateEmail_ThrowsOnSaveChanges` | Insert two users with identical email, call `SaveChangesAsync` on second | Throws `DbUpdateException` (Npgsql unique constraint violation) |

**Total: 9 tests.**

---

## 6. Seeding Pattern

Each test that requires pre-existing data uses a private helper:

```csharp
private async Task<User> SeedUserAsync(string name = "Alice", string email = "alice@example.com")
{
    using var ctx = CreateDbContext();
    var repo = new UserRepository(ctx);
    var uow = new UnitOfWork(ctx);
    var user = User.Create(name, email);
    await repo.AddAsync(user);
    await uow.SaveChangesAsync();
    return user;
}
```

Tests that verify a read create a **new** `DbContext` via `CreateDbContext()` to avoid reading from the EF change tracker cache.

---

## 7. Out of Scope

- HTTP/API layer integration tests
- MassTransit / RabbitMQ container tests
- `GetUsersQueryHandler` or any Application-layer test (those belong in UnitTests)
- Performance/load tests
