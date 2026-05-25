# Microservice Template — Design Spec

**Date:** 2026-05-24  
**Status:** Approved

---

## 1. Overview

A reusable .NET 10 solution template for building microservices that communicate via messaging. It enforces Clean Architecture with CQRS, provides a complete running example using a `User` domain entity, and is designed so teams can fork and replace the example domain with their own.

**Fixed technology choices:**
- Runtime: .NET 10
- Architecture: Clean Architecture + CQRS via MediatR
- Database: PostgreSQL via Npgsql + EF Core 10
- Messaging: MassTransit 8 + RabbitMQ
- Validation: FluentValidation
- Mapping: AutoMapper
- Auth: JWT Bearer
- Observability: OpenTelemetry (traces + metrics, OTLP export)
- API docs: OpenAPI + Swagger/Scalar UI
- Tests: xUnit + Moq
- Containers: Dockerfile (multi-stage) + docker-compose

---

## 2. Solution Structure

```
ClientMicroservice.slnx
│
├── src/
│   ├── ClientMicroservice.Domain/
│   ├── ClientMicroservice.Application/
│   ├── ClientMicroservice.Infrastructure/
│   ├── ClientMicroservice.Contracts/
│   └── ClientMicroservice.API/
│
└── tests/
    └── ClientMicroservice.UnitTests/
```

### Dependency rules

| Project | References |
|---|---|
| Domain | Nothing |
| Application | Domain |
| Infrastructure | Application, Domain |
| Contracts | Nothing |
| API | Application, Infrastructure (via `AddInfrastructure` extension only), Contracts |
| UnitTests | Application, Domain (Infrastructure mocked via Moq) |

Infrastructure types never appear in the API layer except through the `AddInfrastructure(IServiceCollection)` extension method.

---

## 3. Domain Layer (`ClientMicroservice.Domain`)

### Entity

```
User
  Id          : Guid
  Name        : string
  Email       : string
  CreatedAt   : DateTime (UTC)
```

No EF, no framework dependencies.

### Abstractions

```csharp
IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IPagedList<T>> GetPagedAsync(int page, int size, CancellationToken ct);
    Task AddAsync(T entity, CancellationToken ct);
    void Update(T entity);
    void Delete(T entity);
}

IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct);
}
```

### Result type

```csharp
readonly struct Result<T>
{
    bool IsSuccess { get; }
    T Value { get; }        // valid when IsSuccess = true
    Error Error { get; }    // valid when IsSuccess = false
}

record Error(string Code, string Message);
```

### Domain errors

```csharp
static class UserErrors
{
    static readonly Error NotFound        = new("User.NotFound", "User was not found.");
    static readonly Error EmailTaken      = new("User.EmailTaken", "Email is already in use.");
    static readonly Error InvalidEmail    = new("User.InvalidEmail", "Email address is not valid.");
}
```

---

## 4. Application Layer (`ClientMicroservice.Application`)

### Commands

| Command | Input | Output |
|---|---|---|
| `CreateUserCommand` | `Name`, `Email` | `Result<Guid>` |
| `UpdateUserCommand` | `Id`, `Name`, `Email` | `Result<Unit>` |
| `DeleteUserCommand` | `Id` | `Result<Unit>` |

### Queries

| Query | Input | Output |
|---|---|---|
| `GetUserByIdQuery` | `Id` | `Result<UserDto>` |
| `GetUsersQuery` | `PageNumber`, `PageSize` | `Result<PagedList<UserDto>>` |

### DTOs

```csharp
record UserDto(Guid Id, string Name, string Email, DateTime CreatedAt);
```

### Pipeline behaviors (registered in order)

1. **`LoggingBehavior<TRequest, TResponse>`** — logs request name and elapsed time at `Information` level
2. **`ValidationBehavior<TRequest, TResponse>`** — runs all `IValidator<TRequest>` instances; if any fail, returns `Result.Failure` with a validation error before the handler executes

### Validators

- `CreateUserCommandValidator` — `Name` required/max 100, `Email` required/valid format/max 200
- `UpdateUserCommandValidator` — same rules as Create plus `Id` non-empty

### AutoMapper profile

`UserMappingProfile` maps `User` → `UserDto`.

### DI registration

`ApplicationServiceExtensions.AddApplication(this IServiceCollection)` — registers MediatR, AutoMapper, FluentValidation, pipeline behaviors.

---

## 5. Contracts (`ClientMicroservice.Contracts`)

No project references. Intended to be extracted into a standalone NuGet package shared across services.

### Integration events (records)

```csharp
record UserCreatedEvent(Guid UserId, string Name, string Email, DateTime CreatedAt);
record UserUpdatedEvent(Guid UserId, string Name, string Email);
record UserDeletedEvent(Guid UserId);
```

Events are immutable value objects with no behavior.

---

## 6. Infrastructure Layer (`ClientMicroservice.Infrastructure`)

### Persistence

- `ApplicationDbContext : DbContext` — internal to Infrastructure; injected directly into `Repository<T>` and `UnitOfWork`
- `UserConfiguration : IEntityTypeConfiguration<User>` — maps to `users` table (snake_case via Npgsql conventions)
- `Repository<T> : IRepository<T>` — generic EF Core implementation
- `UnitOfWork : IUnitOfWork` — wraps `DbContext.SaveChangesAsync`
- Migrations: `Infrastructure/Persistence/Migrations/`

### Messaging

- `AddMassTransit` configured with RabbitMQ transport
- Exchange/queue naming: `{lowercase-service}.{lowercase-event}` (e.g., `microservicetemplate.user-created`)
- Publish: `CreateUserCommandHandler` publishes `UserCreatedEvent` after successful save
- Consume: `UserCreatedEventConsumer` skeleton — demonstrates inbound event handling

### DI registration

`InfrastructureServiceExtensions.AddInfrastructure(this IServiceCollection, IConfiguration)` — the only Infrastructure symbol referenced by the API.

Reads from `IConfiguration`:
- `ConnectionStrings:DefaultConnection` — Npgsql connection string
- `RabbitMq:Host`, `RabbitMq:Username`, `RabbitMq:Password`

---

## 7. API Layer (`ClientMicroservice.API`)

### Endpoints

| Method | Route | Command/Query | Auth |
|---|---|---|---|
| `GET` | `/users` | `GetUsersQuery` | Required |
| `GET` | `/users/{id}` | `GetUserByIdQuery` | Required |
| `POST` | `/users` | `CreateUserCommand` | Required |
| `PUT` | `/users/{id}` | `UpdateUserCommand` | Required |
| `DELETE` | `/users/{id}` | `DeleteUserCommand` | Required |

### Result → HTTP mapping

`ControllerExtensions.ToActionResult<T>(this Result<T>)`:

| Endpoint | Success | Failure (NotFound) | Failure (Validation) | Failure (other) |
|---|---|---|---|---|
| `GET /users` | 200 OK | — | 422 | 400 |
| `GET /users/{id}` | 200 OK | 404 | — | 400 |
| `POST /users` | 201 Created + `Location: /users/{id}` | — | 422 | 400 |
| `PUT /users/{id}` | 204 No Content | 404 | 422 | 400 |
| `DELETE /users/{id}` | 204 No Content | 404 | — | 400 |

All error responses use `ProblemDetails` (RFC 7807).

### Cross-cutting middleware (in order)

1. Global exception handler → `500 Problem` for unhandled exceptions
2. `UseAuthentication` / `UseAuthorization`
3. Controller routing

### Authentication

JWT Bearer. Reads `Jwt:Secret`, `Jwt:Issuer`, `Jwt:Audience` from configuration. All `UsersController` actions carry `[Authorize]`.

### Observability

OpenTelemetry SDK with:
- ASP.NET Core instrumentation
- EF Core instrumentation
- MassTransit instrumentation
- HttpClient instrumentation
- OTLP exporter (endpoint from `OpenTelemetry:Endpoint`)

### API Documentation

- `AddOpenApi()` + Scalar/Swagger UI in Development
- Bearer token input field visible in UI

### DI composition (Program.cs)

```
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
// JWT, OpenTelemetry, OpenAPI, Controllers registered here
```

---

## 8. Unit Tests (`ClientMicroservice.UnitTests`)

### Test classes

| Class | Tests |
|---|---|
| `CreateUserCommandHandlerTests` | Happy path creates user + publishes event; duplicate email returns `UserErrors.EmailTaken` |
| `GetUserByIdQueryHandlerTests` | Found → returns `UserDto`; not found → returns `UserErrors.NotFound` |
| `ValidationBehaviorTests` | Invalid request short-circuits before handler; valid request calls handler |
| `CreateUserCommandValidatorTests` | Empty name, invalid email, oversized fields all fail validation |

All infrastructure dependencies (`IRepository<T>`, `IUnitOfWork`, `IPublishEndpoint`) are mocked with Moq.

---

## 9. Docker

### `Dockerfile` (multi-stage)

```
Stage 1: mcr.microsoft.com/dotnet/sdk:10.0     → restore + publish
Stage 2: mcr.microsoft.com/dotnet/aspnet:10.0  → final runtime image
```

### `docker-compose.yml`

| Service | Image | Ports |
|---|---|---|
| `api` | built from Dockerfile | 8080 |
| `postgres` | `postgres:17-alpine` | 5432 |
| `rabbitmq` | `rabbitmq:3-management-alpine` | 5672, 15672 |

Environment variables for the API service point to `postgres` and `rabbitmq` service names.

---

## 10. Configuration (`appsettings.json` shape)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=postgres;Database=microservice;Username=postgres;Password=postgres"
  },
  "RabbitMq": {
    "Host": "rabbitmq",
    "Username": "guest",
    "Password": "guest"
  },
  "Jwt": {
    "Secret": "REPLACE_ME_WITH_32_CHAR_MIN_SECRET",
    "Issuer": "ClientMicroservice",
    "Audience": "ClientMicroservice"
  },
  "OpenTelemetry": {
    "Endpoint": "http://localhost:4317"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

---

## 11. Out of Scope

- Integration tests (hitting a real database or broker) — unit tests only in this template
- Event sourcing
- Read/write database split
- Service discovery / health check endpoints (can be added per-service)
- CI/CD pipeline files
