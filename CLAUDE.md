# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Build entire solution
dotnet build ClientMicroservice.slnx

# Run all tests
dotnet test tests/ClientMicroservice.UnitTests/

# Run a single test class
dotnet test tests/ClientMicroservice.UnitTests/ --filter "FullyQualifiedName~CreateUserCommandHandlerTests"

# Run a single test method
dotnet test tests/ClientMicroservice.UnitTests/ --filter "FullyQualifiedName~CreateUserCommandHandlerTests.Handle_WithValidData_CreatesUserAndReturnsId"

# Add a new EF Core migration (run from solution root)
dotnet ef migrations add <MigrationName> \
  --project src/ClientMicroservice.Infrastructure/ClientMicroservice.Infrastructure.csproj \
  --startup-project src/ClientMicroservice.API/ClientMicroservice.API.csproj \
  --output-dir Persistence/Migrations

# Run locally (requires Postgres + RabbitMQ — use docker-compose)
docker-compose up -d postgres rabbitmq
dotnet run --project src/ClientMicroservice.API/
```

## Architecture

Five source projects with strict dependency rules:

| Project | References | Purpose |
|---|---|---|
| `Domain` | nothing | Entities, `Result<T>`, abstractions, domain events |
| `Application` | Domain | CQRS handlers, validators, DTOs, pipeline behaviors |
| `Infrastructure` | Application, Domain, Contracts | EF Core, MassTransit, event bus impl |
| `Contracts` | nothing | Integration events (publishable as NuGet package) |
| `API` | Application, Infrastructure, Contracts | Controllers, middleware, DI composition root |

Infrastructure types must never appear in the API layer except via `AddInfrastructure(IServiceCollection, IConfiguration)`.

## Key Patterns

**Result monad** — handlers return `Result<T>` (a readonly struct). Use implicit operators:
```csharp
return UserErrors.NotFound;   // Error → Result<T>
return user.Id;               // T → Result<T>
```
Never read `.Value` without checking `.IsSuccess` first.

**CQRS** — MediatR 14. Each operation lives in its own folder under `Application/Users/Commands/<Name>/` or `Application/Users/Queries/<Name>/`. Each folder contains the record (command/query), its handler, and optionally its FluentValidation validator.

**Pipeline behaviors** — registered in order in `Application/DependencyInjection.cs`:
1. `LoggingBehavior` — logs request name and elapsed time
2. `ValidationBehavior` — runs all `IValidator<TRequest>` instances; throws `AppValidationException` on failure (never returns `Result.Failure`)

`AppValidationException` is caught by `GlobalExceptionHandler` and mapped to HTTP 422 `ValidationProblemDetails`.

**Domain events vs. integration events** — Application handlers publish *domain events* (`UserCreatedDomainEvent` etc.) via `IEventBus` (defined in Domain). `MassTransitEventBus` in Infrastructure translates them to *Contracts integration events* (`UserCreatedEvent` etc.) via a switch expression before publishing to RabbitMQ. To add a new event: create a domain event in `Domain/Events/`, a contracts event in `Contracts/Users/`, and extend the switch in `Infrastructure/Messaging/MassTransitEventBus.cs`.

**HTTP mapping** — `ControllerExtensions` (in `API/Extensions/`) maps `Result<T>` to `IActionResult`. Error codes ending in or containing `.NotFound` become 404; all others become 400. Validation errors never reach this path (they're thrown before handlers return).

**`Unit` type** — `ClientMicroservice.Domain.Common.Unit` is used for void-returning commands (`Result<Unit>`). MediatR 14 also defines `MediatR.Unit`; files that use both must alias one: `using Unit = ClientMicroservice.Domain.Common.Unit;`.

## Adding a New Domain Entity

1. **Domain**: entity class, `I<Entity>Repository : IRepository<T>`, domain events, errors static class
2. **Application**: commands/queries in `Users/` sibling folder, DTOs, mapping profile, validators
3. **Infrastructure**: `IEntityTypeConfiguration<T>`, extend `ApplicationDbContext.DbSet`, repository class, register in `DependencyInjection.cs`, add migration
4. **Contracts**: integration events
5. **API**: controller, extend `MassTransitEventBus` switch
6. **Tests**: handler tests mocking `IRepository<T>`, `IUnitOfWork`, `IEventBus` via Moq

## Configuration Keys

```
ConnectionStrings:DefaultConnection   Npgsql connection string
RabbitMq:Host / Username / Password   MassTransit transport
Jwt:Secret / Issuer / Audience        JWT Bearer (Secret min 32 chars)
OpenTelemetry:Endpoint                OTLP exporter endpoint
```

All five keys are required at startup — missing values throw `InvalidOperationException`.

## Technology Versions

.NET 10 · MediatR 14.1 · FluentValidation 12.1 · AutoMapper 16.1 · EF Core 10 + Npgsql 10 · MassTransit 9.1 · xUnit 2 + Moq 4.20

**AutoMapper 16 API change**: use `services.AddAutoMapper(cfg => cfg.AddMaps(assembly))`, not `AddAutoMapper(assembly)`. In tests, `MapperConfiguration` constructor requires `NullLoggerFactory.Instance` as second argument.
