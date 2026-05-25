# Microservice Template Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a production-ready .NET 10 microservice solution template with Clean Architecture, CQRS, messaging, and a complete `User` domain example that teams can fork and replace.

**Architecture:** Five source projects (Domain → Application → Infrastructure, Contracts standalone, API as composition root) plus unit tests follow strict Clean Architecture dependency rules. CQRS via MediatR; generic repository + unit of work; `Result<T>` error monad; domain events dispatched via `IEventBus` and translated to integration events in Infrastructure.

**Tech Stack:** .NET 10, MediatR 12, FluentValidation 11, AutoMapper 13, EF Core 10 + Npgsql, MassTransit 8 + RabbitMQ, JWT Bearer, OpenTelemetry OTLP, Scalar UI, xUnit 2 + Moq 4, Docker

---

## File Map

```
ClientMicroservice.slnx                    ← update to list all projects
Dockerfile
docker-compose.yml

src/
  ClientMicroservice.Domain/
    ClientMicroservice.Domain.csproj
    Common/Error.cs
    Common/Result.cs
    Common/Unit.cs
    Common/PagedList.cs
    Abstractions/IRepository.cs
    Abstractions/IUserRepository.cs
    Abstractions/IUnitOfWork.cs
    Abstractions/IEventBus.cs
    Entities/User.cs
    Errors/UserErrors.cs
    Events/UserCreatedDomainEvent.cs
    Events/UserUpdatedDomainEvent.cs
    Events/UserDeletedDomainEvent.cs

  ClientMicroservice.Contracts/
    ClientMicroservice.Contracts.csproj
    Users/UserCreatedEvent.cs
    Users/UserUpdatedEvent.cs
    Users/UserDeletedEvent.cs

  ClientMicroservice.Application/
    ClientMicroservice.Application.csproj
    DependencyInjection.cs
    Common/DTOs/UserDto.cs
    Common/Exceptions/AppValidationException.cs
    Common/Behaviors/LoggingBehavior.cs
    Common/Behaviors/ValidationBehavior.cs
    Users/Mappings/UserMappingProfile.cs
    Users/Commands/CreateUser/CreateUserCommand.cs
    Users/Commands/CreateUser/CreateUserCommandHandler.cs
    Users/Commands/CreateUser/CreateUserCommandValidator.cs
    Users/Commands/UpdateUser/UpdateUserCommand.cs
    Users/Commands/UpdateUser/UpdateUserCommandHandler.cs
    Users/Commands/UpdateUser/UpdateUserCommandValidator.cs
    Users/Commands/DeleteUser/DeleteUserCommand.cs
    Users/Commands/DeleteUser/DeleteUserCommandHandler.cs
    Users/Queries/GetUserById/GetUserByIdQuery.cs
    Users/Queries/GetUserById/GetUserByIdQueryHandler.cs
    Users/Queries/GetUsers/GetUsersQuery.cs
    Users/Queries/GetUsers/GetUsersQueryHandler.cs

  ClientMicroservice.Infrastructure/
    ClientMicroservice.Infrastructure.csproj
    DependencyInjection.cs
    Persistence/ApplicationDbContext.cs
    Persistence/Configurations/UserConfiguration.cs
    Persistence/Repositories/Repository.cs
    Persistence/Repositories/UserRepository.cs
    Persistence/UnitOfWork.cs
    Messaging/MassTransitEventBus.cs
    Messaging/Consumers/UserCreatedEventConsumer.cs

  ClientMicroservice.API/
    ClientMicroservice.API.csproj
    Program.cs
    appsettings.json
    appsettings.Development.json
    Controllers/UsersController.cs
    Extensions/ControllerExtensions.cs
    Middleware/GlobalExceptionHandler.cs
    Properties/launchSettings.json

tests/
  ClientMicroservice.UnitTests/
    ClientMicroservice.UnitTests.csproj
    Users/Commands/CreateUserCommandHandlerTests.cs
    Users/Queries/GetUserByIdQueryHandlerTests.cs
    Common/Behaviors/ValidationBehaviorTests.cs
    Users/Validators/CreateUserCommandValidatorTests.cs
```

---

## Task 1: Scaffold solution structure

**Files:**
- Modify: `ClientMicroservice.slnx`
- Delete: `ClientMicroservice/` (default scaffold)
- Create: all six `.csproj` files and project references

- [ ] **Step 1.1: Create directory structure and scaffold projects**

Run from `/Users/viniciuscarvalho/dev/loomi/ClientMicroservice/`:

```bash
mkdir -p src tests

dotnet new classlib -n ClientMicroservice.Domain -f net10.0 -o src/ClientMicroservice.Domain
dotnet new classlib -n ClientMicroservice.Application -f net10.0 -o src/ClientMicroservice.Application
dotnet new classlib -n ClientMicroservice.Infrastructure -f net10.0 -o src/ClientMicroservice.Infrastructure
dotnet new classlib -n ClientMicroservice.Contracts -f net10.0 -o src/ClientMicroservice.Contracts
dotnet new webapi --use-controllers -n ClientMicroservice.API -f net10.0 -o src/ClientMicroservice.API
dotnet new xunit -n ClientMicroservice.UnitTests -f net10.0 -o tests/ClientMicroservice.UnitTests

rm src/ClientMicroservice.Domain/Class1.cs
rm src/ClientMicroservice.Application/Class1.cs
rm src/ClientMicroservice.Infrastructure/Class1.cs
rm src/ClientMicroservice.Contracts/Class1.cs
rm tests/ClientMicroservice.UnitTests/UnitTest1.cs
rm src/ClientMicroservice.API/WeatherForecast.cs
rm src/ClientMicroservice.API/Controllers/WeatherForecastController.cs
rm -rf ClientMicroservice/
```

Expected: six project directories created, no errors.

- [ ] **Step 1.2: Update ClientMicroservice.slnx**

Replace the entire contents of `ClientMicroservice.slnx` with:

```xml
<Solution>
  <Project Path="src/ClientMicroservice.Domain/ClientMicroservice.Domain.csproj" />
  <Project Path="src/ClientMicroservice.Application/ClientMicroservice.Application.csproj" />
  <Project Path="src/ClientMicroservice.Infrastructure/ClientMicroservice.Infrastructure.csproj" />
  <Project Path="src/ClientMicroservice.Contracts/ClientMicroservice.Contracts.csproj" />
  <Project Path="src/ClientMicroservice.API/ClientMicroservice.API.csproj" />
  <Project Path="tests/ClientMicroservice.UnitTests/ClientMicroservice.UnitTests.csproj" />
</Solution>
```

- [ ] **Step 1.3: Add project references**

```bash
# Application → Domain
dotnet add src/ClientMicroservice.Application/ClientMicroservice.Application.csproj \
  reference src/ClientMicroservice.Domain/ClientMicroservice.Domain.csproj

# Infrastructure → Application + Domain
dotnet add src/ClientMicroservice.Infrastructure/ClientMicroservice.Infrastructure.csproj \
  reference src/ClientMicroservice.Application/ClientMicroservice.Application.csproj
dotnet add src/ClientMicroservice.Infrastructure/ClientMicroservice.Infrastructure.csproj \
  reference src/ClientMicroservice.Domain/ClientMicroservice.Domain.csproj

# API → Application + Infrastructure + Contracts
dotnet add src/ClientMicroservice.API/ClientMicroservice.API.csproj \
  reference src/ClientMicroservice.Application/ClientMicroservice.Application.csproj
dotnet add src/ClientMicroservice.API/ClientMicroservice.API.csproj \
  reference src/ClientMicroservice.Infrastructure/ClientMicroservice.Infrastructure.csproj
dotnet add src/ClientMicroservice.API/ClientMicroservice.API.csproj \
  reference src/ClientMicroservice.Contracts/ClientMicroservice.Contracts.csproj

# Tests → Application + Domain
dotnet add tests/ClientMicroservice.UnitTests/ClientMicroservice.UnitTests.csproj \
  reference src/ClientMicroservice.Application/ClientMicroservice.Application.csproj
dotnet add tests/ClientMicroservice.UnitTests/ClientMicroservice.UnitTests.csproj \
  reference src/ClientMicroservice.Domain/ClientMicroservice.Domain.csproj
```

Expected: each command prints `Reference ... added to project.`

- [ ] **Step 1.4: Add NuGet packages**

```bash
# Application
dotnet add src/ClientMicroservice.Application/ClientMicroservice.Application.csproj package MediatR
dotnet add src/ClientMicroservice.Application/ClientMicroservice.Application.csproj package FluentValidation
dotnet add src/ClientMicroservice.Application/ClientMicroservice.Application.csproj package FluentValidation.DependencyInjectionExtensions
dotnet add src/ClientMicroservice.Application/ClientMicroservice.Application.csproj package AutoMapper
dotnet add src/ClientMicroservice.Application/ClientMicroservice.Application.csproj package Microsoft.Extensions.Logging.Abstractions
dotnet add src/ClientMicroservice.Application/ClientMicroservice.Application.csproj package Microsoft.Extensions.DependencyInjection.Abstractions

# Infrastructure
dotnet add src/ClientMicroservice.Infrastructure/ClientMicroservice.Infrastructure.csproj package Microsoft.EntityFrameworkCore
dotnet add src/ClientMicroservice.Infrastructure/ClientMicroservice.Infrastructure.csproj package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add src/ClientMicroservice.Infrastructure/ClientMicroservice.Infrastructure.csproj package Microsoft.EntityFrameworkCore.Design
dotnet add src/ClientMicroservice.Infrastructure/ClientMicroservice.Infrastructure.csproj package MassTransit
dotnet add src/ClientMicroservice.Infrastructure/ClientMicroservice.Infrastructure.csproj package MassTransit.RabbitMQ
dotnet add src/ClientMicroservice.Infrastructure/ClientMicroservice.Infrastructure.csproj package Microsoft.Extensions.Configuration.Abstractions

# API
dotnet add src/ClientMicroservice.API/ClientMicroservice.API.csproj package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add src/ClientMicroservice.API/ClientMicroservice.API.csproj package Scalar.AspNetCore
dotnet add src/ClientMicroservice.API/ClientMicroservice.API.csproj package Microsoft.AspNetCore.OpenApi
dotnet add src/ClientMicroservice.API/ClientMicroservice.API.csproj package OpenTelemetry.Extensions.Hosting
dotnet add src/ClientMicroservice.API/ClientMicroservice.API.csproj package OpenTelemetry.Instrumentation.AspNetCore
dotnet add src/ClientMicroservice.API/ClientMicroservice.API.csproj package OpenTelemetry.Exporter.OpenTelemetryProtocol
dotnet add src/ClientMicroservice.API/ClientMicroservice.API.csproj package OpenTelemetry.Instrumentation.Http

# Tests
dotnet add tests/ClientMicroservice.UnitTests/ClientMicroservice.UnitTests.csproj package Moq
dotnet add tests/ClientMicroservice.UnitTests/ClientMicroservice.UnitTests.csproj package Microsoft.NET.Test.Sdk
```

Expected: no errors from `dotnet add package`.

- [ ] **Step 1.5: Verify solution builds (with empty projects)**

```bash
dotnet build ClientMicroservice.slnx
```

Expected: `Build succeeded` (0 errors). Warnings about empty projects are acceptable.

- [ ] **Step 1.6: Commit**

```bash
git init
git add .
git commit -m "chore: scaffold solution structure with six projects"
```

---

## Task 2: Domain — core primitives

**Files:**
- Create: `src/ClientMicroservice.Domain/Common/Error.cs`
- Create: `src/ClientMicroservice.Domain/Common/Result.cs`
- Create: `src/ClientMicroservice.Domain/Common/Unit.cs`
- Create: `src/ClientMicroservice.Domain/Common/PagedList.cs`

- [ ] **Step 2.1: Create `Error.cs`**

`src/ClientMicroservice.Domain/Common/Error.cs`:

```csharp
namespace ClientMicroservice.Domain.Common;

public sealed record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);
    public static readonly Error Unexpected = new("Unexpected", "An unexpected error occurred.");
}
```

- [ ] **Step 2.2: Create `Unit.cs`**

`src/ClientMicroservice.Domain/Common/Unit.cs`:

```csharp
namespace ClientMicroservice.Domain.Common;

public readonly struct Unit
{
    public static readonly Unit Value = new();
}
```

- [ ] **Step 2.3: Create `Result.cs`**

`src/ClientMicroservice.Domain/Common/Result.cs`:

```csharp
namespace ClientMicroservice.Domain.Common;

public readonly struct Result<T>
{
    private Result(T value)
    {
        IsSuccess = true;
        Value = value;
        Error = Error.None;
    }

    private Result(Error error)
    {
        IsSuccess = false;
        Value = default!;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T Value { get; }
    public Error Error { get; }

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(Error error) => new(error);

    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(Error error) => Failure(error);
}
```

- [ ] **Step 2.4: Create `PagedList.cs`**

`src/ClientMicroservice.Domain/Common/PagedList.cs`:

```csharp
namespace ClientMicroservice.Domain.Common;

public sealed class PagedList<T>
{
    public PagedList(IReadOnlyList<T> items, int pageNumber, int pageSize, int totalCount)
    {
        Items = items;
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalCount = totalCount;
    }

    public IReadOnlyList<T> Items { get; }
    public int PageNumber { get; }
    public int PageSize { get; }
    public int TotalCount { get; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNextPage => PageNumber < TotalPages;
    public bool HasPreviousPage => PageNumber > 1;
}
```

- [ ] **Step 2.5: Build Domain project**

```bash
dotnet build src/ClientMicroservice.Domain/ClientMicroservice.Domain.csproj
```

Expected: `Build succeeded`.

- [ ] **Step 2.6: Commit**

```bash
git add src/ClientMicroservice.Domain/Common/
git commit -m "feat(domain): add Result<T>, Error, Unit, and PagedList primitives"
```

---

## Task 3: Domain — entity, abstractions, errors, domain events

**Files:**
- Create: `src/ClientMicroservice.Domain/Entities/User.cs`
- Create: `src/ClientMicroservice.Domain/Abstractions/IRepository.cs`
- Create: `src/ClientMicroservice.Domain/Abstractions/IUserRepository.cs`
- Create: `src/ClientMicroservice.Domain/Abstractions/IUnitOfWork.cs`
- Create: `src/ClientMicroservice.Domain/Abstractions/IEventBus.cs`
- Create: `src/ClientMicroservice.Domain/Errors/UserErrors.cs`
- Create: `src/ClientMicroservice.Domain/Events/UserCreatedDomainEvent.cs`
- Create: `src/ClientMicroservice.Domain/Events/UserUpdatedDomainEvent.cs`
- Create: `src/ClientMicroservice.Domain/Events/UserDeletedDomainEvent.cs`

- [ ] **Step 3.1: Create `User.cs`**

`src/ClientMicroservice.Domain/Entities/User.cs`:

```csharp
namespace ClientMicroservice.Domain.Entities;

public sealed class User
{
    private User() { }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    public static User Create(string name, string email) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Email = email,
        CreatedAt = DateTime.UtcNow
    };

    public void Update(string name, string email)
    {
        Name = name;
        Email = email;
    }
}
```

- [ ] **Step 3.2: Create `IRepository.cs`**

`src/ClientMicroservice.Domain/Abstractions/IRepository.cs`:

```csharp
using ClientMicroservice.Domain.Common;

namespace ClientMicroservice.Domain.Abstractions;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedList<T>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    void Update(T entity);
    void Delete(T entity);
}
```

- [ ] **Step 3.3: Create `IUserRepository.cs`**

`src/ClientMicroservice.Domain/Abstractions/IUserRepository.cs`:

```csharp
using ClientMicroservice.Domain.Entities;

namespace ClientMicroservice.Domain.Abstractions;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
}
```

- [ ] **Step 3.4: Create `IUnitOfWork.cs`**

`src/ClientMicroservice.Domain/Abstractions/IUnitOfWork.cs`:

```csharp
namespace ClientMicroservice.Domain.Abstractions;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
```

- [ ] **Step 3.5: Create `IEventBus.cs`**

`src/ClientMicroservice.Domain/Abstractions/IEventBus.cs`:

```csharp
namespace ClientMicroservice.Domain.Abstractions;

public interface IEventBus
{
    Task PublishAsync<T>(T message, CancellationToken ct = default) where T : class;
}
```

- [ ] **Step 3.6: Create `UserErrors.cs`**

`src/ClientMicroservice.Domain/Errors/UserErrors.cs`:

```csharp
using ClientMicroservice.Domain.Common;

namespace ClientMicroservice.Domain.Errors;

public static class UserErrors
{
    public static readonly Error NotFound = new("User.NotFound", "User was not found.");
    public static readonly Error EmailTaken = new("User.EmailTaken", "Email address is already in use.");
}
```

- [ ] **Step 3.7: Create domain events**

`src/ClientMicroservice.Domain/Events/UserCreatedDomainEvent.cs`:

```csharp
namespace ClientMicroservice.Domain.Events;

public record UserCreatedDomainEvent(Guid UserId, string Name, string Email, DateTime CreatedAt);
```

`src/ClientMicroservice.Domain/Events/UserUpdatedDomainEvent.cs`:

```csharp
namespace ClientMicroservice.Domain.Events;

public record UserUpdatedDomainEvent(Guid UserId, string Name, string Email);
```

`src/ClientMicroservice.Domain/Events/UserDeletedDomainEvent.cs`:

```csharp
namespace ClientMicroservice.Domain.Events;

public record UserDeletedDomainEvent(Guid UserId);
```

- [ ] **Step 3.8: Build Domain**

```bash
dotnet build src/ClientMicroservice.Domain/ClientMicroservice.Domain.csproj
```

Expected: `Build succeeded`.

- [ ] **Step 3.9: Commit**

```bash
git add src/ClientMicroservice.Domain/
git commit -m "feat(domain): add User entity, repository abstractions, domain events, and errors"
```

---

## Task 4: Contracts project

**Files:**
- Create: `src/ClientMicroservice.Contracts/Users/UserCreatedEvent.cs`
- Create: `src/ClientMicroservice.Contracts/Users/UserUpdatedEvent.cs`
- Create: `src/ClientMicroservice.Contracts/Users/UserDeletedEvent.cs`

- [ ] **Step 4.1: Create integration events**

`src/ClientMicroservice.Contracts/Users/UserCreatedEvent.cs`:

```csharp
namespace ClientMicroservice.Contracts.Users;

public record UserCreatedEvent(Guid UserId, string Name, string Email, DateTime CreatedAt);
```

`src/ClientMicroservice.Contracts/Users/UserUpdatedEvent.cs`:

```csharp
namespace ClientMicroservice.Contracts.Users;

public record UserUpdatedEvent(Guid UserId, string Name, string Email);
```

`src/ClientMicroservice.Contracts/Users/UserDeletedEvent.cs`:

```csharp
namespace ClientMicroservice.Contracts.Users;

public record UserDeletedEvent(Guid UserId);
```

- [ ] **Step 4.2: Build Contracts**

```bash
dotnet build src/ClientMicroservice.Contracts/ClientMicroservice.Contracts.csproj
```

Expected: `Build succeeded`.

- [ ] **Step 4.3: Commit**

```bash
git add src/ClientMicroservice.Contracts/
git commit -m "feat(contracts): add integration event records for User"
```

---

## Task 5: Application — project setup, DTO, mapping, DI

**Files:**
- Create: `src/ClientMicroservice.Application/Common/DTOs/UserDto.cs`
- Create: `src/ClientMicroservice.Application/Users/Mappings/UserMappingProfile.cs`
- Create: `src/ClientMicroservice.Application/DependencyInjection.cs`

- [ ] **Step 5.1: Create `UserDto.cs`**

`src/ClientMicroservice.Application/Common/DTOs/UserDto.cs`:

```csharp
namespace ClientMicroservice.Application.Common.DTOs;

public record UserDto(Guid Id, string Name, string Email, DateTime CreatedAt);
```

- [ ] **Step 5.2: Create `UserMappingProfile.cs`**

`src/ClientMicroservice.Application/Users/Mappings/UserMappingProfile.cs`:

```csharp
using AutoMapper;
using ClientMicroservice.Application.Common.DTOs;
using ClientMicroservice.Domain.Entities;

namespace ClientMicroservice.Application.Users.Mappings;

public sealed class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        CreateMap<User, UserDto>();
    }
}
```

- [ ] **Step 5.3: Create `DependencyInjection.cs`**

`src/ClientMicroservice.Application/DependencyInjection.cs`:

```csharp
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using ClientMicroservice.Application.Common.Behaviors;

namespace ClientMicroservice.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        });

        services.AddAutoMapper(typeof(DependencyInjection).Assembly);

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
```

- [ ] **Step 5.4: Build Application (will fail — behaviors not yet created)**

```bash
dotnet build src/ClientMicroservice.Application/ClientMicroservice.Application.csproj 2>&1 | head -20
```

Expected: compile errors referencing `LoggingBehavior` and `ValidationBehavior`. This confirms the stubs are needed.

- [ ] **Step 5.5: Commit partial state**

```bash
git add src/ClientMicroservice.Application/Common/DTOs/ \
        src/ClientMicroservice.Application/Users/Mappings/ \
        src/ClientMicroservice.Application/DependencyInjection.cs
git commit -m "feat(application): add UserDto, mapping profile, and DI registration stub"
```

---

## Task 6: Application — pipeline behaviors and validation exception

**Files:**
- Create: `src/ClientMicroservice.Application/Common/Exceptions/AppValidationException.cs`
- Create: `src/ClientMicroservice.Application/Common/Behaviors/LoggingBehavior.cs`
- Create: `src/ClientMicroservice.Application/Common/Behaviors/ValidationBehavior.cs`
- Test: `tests/ClientMicroservice.UnitTests/Common/Behaviors/ValidationBehaviorTests.cs`

- [ ] **Step 6.1: Write failing tests for ValidationBehavior**

`tests/ClientMicroservice.UnitTests/Common/Behaviors/ValidationBehaviorTests.cs`:

```csharp
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using ClientMicroservice.Application.Common.Behaviors;
using ClientMicroservice.Application.Common.Exceptions;
using ClientMicroservice.Domain.Common;
using Moq;

namespace ClientMicroservice.UnitTests.Common.Behaviors;

public sealed class ValidationBehaviorTests
{
    private sealed record TestRequest(string Value) : IRequest<Result<string>>;

    private sealed class FailingValidator : AbstractValidator<TestRequest>
    {
        public FailingValidator()
        {
            RuleFor(x => x.Value).NotEmpty().WithMessage("Value is required.");
        }
    }

    [Fact]
    public async Task Handle_WhenValidationFails_ThrowsAppValidationException()
    {
        var validators = new List<IValidator<TestRequest>> { new FailingValidator() };
        var behavior = new ValidationBehavior<TestRequest, Result<string>>(validators);
        var next = new Mock<RequestHandlerDelegate<Result<string>>>();

        var act = async () => await behavior.Handle(
            new TestRequest(string.Empty), next.Object, CancellationToken.None);

        await Assert.ThrowsAsync<AppValidationException>(act);
        next.Verify(n => n(), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenValidationPasses_CallsNext()
    {
        var validators = new List<IValidator<TestRequest>> { new FailingValidator() };
        var behavior = new ValidationBehavior<TestRequest, Result<string>>(validators);
        var next = new Mock<RequestHandlerDelegate<Result<string>>>();
        next.Setup(n => n()).ReturnsAsync(Result<string>.Success("ok"));

        var result = await behavior.Handle(
            new TestRequest("valid"), next.Object, CancellationToken.None);

        Assert.True(result.IsSuccess);
        next.Verify(n => n(), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNoValidators_CallsNext()
    {
        var behavior = new ValidationBehavior<TestRequest, Result<string>>(
            Enumerable.Empty<IValidator<TestRequest>>());
        var next = new Mock<RequestHandlerDelegate<Result<string>>>();
        next.Setup(n => n()).ReturnsAsync(Result<string>.Success("ok"));

        var result = await behavior.Handle(
            new TestRequest("any"), next.Object, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }
}
```

- [ ] **Step 6.2: Run tests — expect compile failure (types don't exist yet)**

```bash
dotnet test tests/ClientMicroservice.UnitTests/ClientMicroservice.UnitTests.csproj 2>&1 | head -30
```

Expected: compile errors for `AppValidationException`, `ValidationBehavior`, `LoggingBehavior`.

- [ ] **Step 6.3: Create `AppValidationException.cs`**

`src/ClientMicroservice.Application/Common/Exceptions/AppValidationException.cs`:

```csharp
using FluentValidation.Results;

namespace ClientMicroservice.Application.Common.Exceptions;

public sealed class AppValidationException : Exception
{
    public AppValidationException(IEnumerable<ValidationFailure> failures)
        : base("One or more validation errors occurred.")
    {
        Errors = failures
            .GroupBy(f => f.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(f => f.ErrorMessage).ToArray());
    }

    public IDictionary<string, string[]> Errors { get; }
}
```

- [ ] **Step 6.4: Create `LoggingBehavior.cs`**

`src/ClientMicroservice.Application/Common/Behaviors/LoggingBehavior.cs`:

```csharp
using MediatR;
using Microsoft.Extensions.Logging;

namespace ClientMicroservice.Application.Common.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var requestName = typeof(TRequest).Name;
        logger.LogInformation("Handling {RequestName}", requestName);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var response = await next();
        stopwatch.Stop();

        logger.LogInformation(
            "Handled {RequestName} in {ElapsedMs}ms",
            requestName,
            stopwatch.ElapsedMilliseconds);

        return response;
    }
}
```

- [ ] **Step 6.5: Create `ValidationBehavior.cs`**

`src/ClientMicroservice.Application/Common/Behaviors/ValidationBehavior.cs`:

```csharp
using FluentValidation;
using MediatR;
using ClientMicroservice.Application.Common.Exceptions;

namespace ClientMicroservice.Application.Common.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        if (!validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);

        var failures = validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count > 0)
            throw new AppValidationException(failures);

        return await next();
    }
}
```

- [ ] **Step 6.6: Run tests — expect pass**

```bash
dotnet test tests/ClientMicroservice.UnitTests/ClientMicroservice.UnitTests.csproj \
  --filter "FullyQualifiedName~ValidationBehaviorTests" -v minimal
```

Expected: `3 passed, 0 failed`.

- [ ] **Step 6.7: Commit**

```bash
git add src/ClientMicroservice.Application/Common/ \
        tests/ClientMicroservice.UnitTests/Common/
git commit -m "feat(application): add pipeline behaviors and AppValidationException"
```

---

## Task 7: Application — CreateUser command

**Files:**
- Create: `src/ClientMicroservice.Application/Users/Commands/CreateUser/CreateUserCommand.cs`
- Create: `src/ClientMicroservice.Application/Users/Commands/CreateUser/CreateUserCommandValidator.cs`
- Create: `src/ClientMicroservice.Application/Users/Commands/CreateUser/CreateUserCommandHandler.cs`
- Test: `tests/ClientMicroservice.UnitTests/Users/Commands/CreateUserCommandHandlerTests.cs`
- Test: `tests/ClientMicroservice.UnitTests/Users/Validators/CreateUserCommandValidatorTests.cs`

- [ ] **Step 7.1: Write failing handler tests**

`tests/ClientMicroservice.UnitTests/Users/Commands/CreateUserCommandHandlerTests.cs`:

```csharp
using ClientMicroservice.Application.Users.Commands.CreateUser;
using ClientMicroservice.Domain.Abstractions;
using ClientMicroservice.Domain.Common;
using ClientMicroservice.Domain.Entities;
using ClientMicroservice.Domain.Errors;
using ClientMicroservice.Domain.Events;
using Moq;

namespace ClientMicroservice.UnitTests.Users.Commands;

public sealed class CreateUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IEventBus> _eventBusMock = new();
    private readonly CreateUserCommandHandler _handler;

    public CreateUserCommandHandlerTests()
    {
        _handler = new CreateUserCommandHandler(
            _repoMock.Object, _uowMock.Object, _eventBusMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidData_CreatesUserAndReturnsId()
    {
        _repoMock.Setup(r => r.GetByEmailAsync("john@example.com", It.IsAny<CancellationToken>()))
                 .ReturnsAsync((User?)null);

        var command = new CreateUserCommand("John Doe", "john@example.com");
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _eventBusMock.Verify(e => e.PublishAsync(
            It.IsAny<UserCreatedDomainEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenEmailAlreadyTaken_ReturnsEmailTakenError()
    {
        var existing = User.Create("Existing", "john@example.com");
        _repoMock.Setup(r => r.GetByEmailAsync("john@example.com", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(existing);

        var command = new CreateUserCommand("John Doe", "john@example.com");
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(UserErrors.EmailTaken.Code, result.Error.Code);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
```

- [ ] **Step 7.2: Write failing validator tests**

`tests/ClientMicroservice.UnitTests/Users/Validators/CreateUserCommandValidatorTests.cs`:

```csharp
using ClientMicroservice.Application.Users.Commands.CreateUser;

namespace ClientMicroservice.UnitTests.Users.Validators;

public sealed class CreateUserCommandValidatorTests
{
    private readonly CreateUserCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidData_Passes()
    {
        var result = _validator.Validate(new CreateUserCommand("Jane", "jane@example.com"));
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("", "jane@example.com")]
    [InlineData(null, "jane@example.com")]
    public void Validate_WithEmptyName_Fails(string? name, string email)
    {
        var result = _validator.Validate(new CreateUserCommand(name!, email));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateUserCommand.Name));
    }

    [Theory]
    [InlineData("Jane", "")]
    [InlineData("Jane", "not-an-email")]
    [InlineData("Jane", null)]
    public void Validate_WithInvalidEmail_Fails(string name, string? email)
    {
        var result = _validator.Validate(new CreateUserCommand(name, email!));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateUserCommand.Email));
    }

    [Fact]
    public void Validate_WithNameExceedingMaxLength_Fails()
    {
        var result = _validator.Validate(new CreateUserCommand(new string('A', 101), "a@b.com"));
        Assert.False(result.IsValid);
    }
}
```

- [ ] **Step 7.3: Run tests — expect compile failure**

```bash
dotnet test tests/ClientMicroservice.UnitTests/ClientMicroservice.UnitTests.csproj 2>&1 | head -20
```

Expected: errors for `CreateUserCommand`, `CreateUserCommandValidator`, `CreateUserCommandHandler`.

- [ ] **Step 7.4: Create `CreateUserCommand.cs`**

`src/ClientMicroservice.Application/Users/Commands/CreateUser/CreateUserCommand.cs`:

```csharp
using MediatR;
using ClientMicroservice.Domain.Common;

namespace ClientMicroservice.Application.Users.Commands.CreateUser;

public sealed record CreateUserCommand(string Name, string Email) : IRequest<Result<Guid>>;
```

- [ ] **Step 7.5: Create `CreateUserCommandValidator.cs`**

`src/ClientMicroservice.Application/Users/Commands/CreateUser/CreateUserCommandValidator.cs`:

```csharp
using FluentValidation;

namespace ClientMicroservice.Application.Users.Commands.CreateUser;

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(200);
    }
}
```

- [ ] **Step 7.6: Create `CreateUserCommandHandler.cs`**

`src/ClientMicroservice.Application/Users/Commands/CreateUser/CreateUserCommandHandler.cs`:

```csharp
using MediatR;
using ClientMicroservice.Domain.Abstractions;
using ClientMicroservice.Domain.Common;
using ClientMicroservice.Domain.Entities;
using ClientMicroservice.Domain.Errors;
using ClientMicroservice.Domain.Events;

namespace ClientMicroservice.Application.Users.Commands.CreateUser;

public sealed class CreateUserCommandHandler(
    IUserRepository repository,
    IUnitOfWork unitOfWork,
    IEventBus eventBus)
    : IRequestHandler<CreateUserCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateUserCommand command, CancellationToken ct)
    {
        var existing = await repository.GetByEmailAsync(command.Email, ct);
        if (existing is not null)
            return UserErrors.EmailTaken;

        var user = User.Create(command.Name, command.Email);
        await repository.AddAsync(user, ct);
        await unitOfWork.SaveChangesAsync(ct);

        await eventBus.PublishAsync(
            new UserCreatedDomainEvent(user.Id, user.Name, user.Email, user.CreatedAt), ct);

        return user.Id;
    }
}
```

- [ ] **Step 7.7: Run tests — expect pass**

```bash
dotnet test tests/ClientMicroservice.UnitTests/ClientMicroservice.UnitTests.csproj \
  --filter "FullyQualifiedName~CreateUser" -v minimal
```

Expected: `6 passed, 0 failed`.

- [ ] **Step 7.8: Commit**

```bash
git add src/ClientMicroservice.Application/Users/Commands/CreateUser/ \
        tests/ClientMicroservice.UnitTests/Users/
git commit -m "feat(application): add CreateUser command with handler and validator"
```

---

## Task 8: Application — UpdateUser and DeleteUser commands

**Files:**
- Create: `src/ClientMicroservice.Application/Users/Commands/UpdateUser/UpdateUserCommand.cs`
- Create: `src/ClientMicroservice.Application/Users/Commands/UpdateUser/UpdateUserCommandValidator.cs`
- Create: `src/ClientMicroservice.Application/Users/Commands/UpdateUser/UpdateUserCommandHandler.cs`
- Create: `src/ClientMicroservice.Application/Users/Commands/DeleteUser/DeleteUserCommand.cs`
- Create: `src/ClientMicroservice.Application/Users/Commands/DeleteUser/DeleteUserCommandHandler.cs`

- [ ] **Step 8.1: Create `UpdateUserCommand.cs`**

`src/ClientMicroservice.Application/Users/Commands/UpdateUser/UpdateUserCommand.cs`:

```csharp
using MediatR;
using ClientMicroservice.Domain.Common;

namespace ClientMicroservice.Application.Users.Commands.UpdateUser;

public sealed record UpdateUserCommand(Guid Id, string Name, string Email) : IRequest<Result<Unit>>;
```

- [ ] **Step 8.2: Create `UpdateUserCommandValidator.cs`**

`src/ClientMicroservice.Application/Users/Commands/UpdateUser/UpdateUserCommandValidator.cs`:

```csharp
using FluentValidation;

namespace ClientMicroservice.Application.Users.Commands.UpdateUser;

public sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(200);
    }
}
```

- [ ] **Step 8.3: Create `UpdateUserCommandHandler.cs`**

`src/ClientMicroservice.Application/Users/Commands/UpdateUser/UpdateUserCommandHandler.cs`:

```csharp
using MediatR;
using ClientMicroservice.Domain.Abstractions;
using ClientMicroservice.Domain.Common;
using ClientMicroservice.Domain.Errors;
using ClientMicroservice.Domain.Events;

namespace ClientMicroservice.Application.Users.Commands.UpdateUser;

public sealed class UpdateUserCommandHandler(
    IUserRepository repository,
    IUnitOfWork unitOfWork,
    IEventBus eventBus)
    : IRequestHandler<UpdateUserCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(UpdateUserCommand command, CancellationToken ct)
    {
        var user = await repository.GetByIdAsync(command.Id, ct);
        if (user is null)
            return UserErrors.NotFound;

        user.Update(command.Name, command.Email);
        repository.Update(user);
        await unitOfWork.SaveChangesAsync(ct);

        await eventBus.PublishAsync(
            new UserUpdatedDomainEvent(user.Id, user.Name, user.Email), ct);

        return Unit.Value;
    }
}
```

- [ ] **Step 8.4: Create `DeleteUserCommand.cs`**

`src/ClientMicroservice.Application/Users/Commands/DeleteUser/DeleteUserCommand.cs`:

```csharp
using MediatR;
using ClientMicroservice.Domain.Common;

namespace ClientMicroservice.Application.Users.Commands.DeleteUser;

public sealed record DeleteUserCommand(Guid Id) : IRequest<Result<Unit>>;
```

- [ ] **Step 8.5: Create `DeleteUserCommandHandler.cs`**

`src/ClientMicroservice.Application/Users/Commands/DeleteUser/DeleteUserCommandHandler.cs`:

```csharp
using MediatR;
using ClientMicroservice.Domain.Abstractions;
using ClientMicroservice.Domain.Common;
using ClientMicroservice.Domain.Errors;
using ClientMicroservice.Domain.Events;

namespace ClientMicroservice.Application.Users.Commands.DeleteUser;

public sealed class DeleteUserCommandHandler(
    IUserRepository repository,
    IUnitOfWork unitOfWork,
    IEventBus eventBus)
    : IRequestHandler<DeleteUserCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(DeleteUserCommand command, CancellationToken ct)
    {
        var user = await repository.GetByIdAsync(command.Id, ct);
        if (user is null)
            return UserErrors.NotFound;

        repository.Delete(user);
        await unitOfWork.SaveChangesAsync(ct);

        await eventBus.PublishAsync(new UserDeletedDomainEvent(user.Id), ct);

        return Unit.Value;
    }
}
```

- [ ] **Step 8.6: Build Application**

```bash
dotnet build src/ClientMicroservice.Application/ClientMicroservice.Application.csproj
```

Expected: `Build succeeded`.

- [ ] **Step 8.7: Commit**

```bash
git add src/ClientMicroservice.Application/Users/Commands/UpdateUser/ \
        src/ClientMicroservice.Application/Users/Commands/DeleteUser/
git commit -m "feat(application): add UpdateUser and DeleteUser commands"
```

---

## Task 9: Application — GetUserById and GetUsers queries

**Files:**
- Create: `src/ClientMicroservice.Application/Users/Queries/GetUserById/GetUserByIdQuery.cs`
- Create: `src/ClientMicroservice.Application/Users/Queries/GetUserById/GetUserByIdQueryHandler.cs`
- Create: `src/ClientMicroservice.Application/Users/Queries/GetUsers/GetUsersQuery.cs`
- Create: `src/ClientMicroservice.Application/Users/Queries/GetUsers/GetUsersQueryHandler.cs`
- Test: `tests/ClientMicroservice.UnitTests/Users/Queries/GetUserByIdQueryHandlerTests.cs`

- [ ] **Step 9.1: Write failing tests for GetUserByIdQueryHandler**

`tests/ClientMicroservice.UnitTests/Users/Queries/GetUserByIdQueryHandlerTests.cs`:

```csharp
using AutoMapper;
using ClientMicroservice.Application.Common.DTOs;
using ClientMicroservice.Application.Users.Mappings;
using ClientMicroservice.Application.Users.Queries.GetUserById;
using ClientMicroservice.Domain.Abstractions;
using ClientMicroservice.Domain.Entities;
using ClientMicroservice.Domain.Errors;
using Moq;

namespace ClientMicroservice.UnitTests.Users.Queries;

public sealed class GetUserByIdQueryHandlerTests
{
    private readonly Mock<IUserRepository> _repoMock = new();
    private readonly IMapper _mapper;
    private readonly GetUserByIdQueryHandler _handler;

    public GetUserByIdQueryHandlerTests()
    {
        _mapper = new MapperConfiguration(cfg => cfg.AddProfile<UserMappingProfile>())
            .CreateMapper();
        _handler = new GetUserByIdQueryHandler(_repoMock.Object, _mapper);
    }

    [Fact]
    public async Task Handle_WhenUserExists_ReturnsUserDto()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("Alice", "alice@example.com");
        _repoMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(user);

        var result = await _handler.Handle(new GetUserByIdQuery(userId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Alice", result.Value.Name);
        Assert.Equal("alice@example.com", result.Value.Email);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsNotFoundError()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((User?)null);

        var result = await _handler.Handle(
            new GetUserByIdQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(UserErrors.NotFound.Code, result.Error.Code);
    }
}
```

- [ ] **Step 9.2: Create `GetUserByIdQuery.cs`**

`src/ClientMicroservice.Application/Users/Queries/GetUserById/GetUserByIdQuery.cs`:

```csharp
using MediatR;
using ClientMicroservice.Application.Common.DTOs;
using ClientMicroservice.Domain.Common;

namespace ClientMicroservice.Application.Users.Queries.GetUserById;

public sealed record GetUserByIdQuery(Guid Id) : IRequest<Result<UserDto>>;
```

- [ ] **Step 9.3: Create `GetUserByIdQueryHandler.cs`**

`src/ClientMicroservice.Application/Users/Queries/GetUserById/GetUserByIdQueryHandler.cs`:

```csharp
using AutoMapper;
using MediatR;
using ClientMicroservice.Application.Common.DTOs;
using ClientMicroservice.Domain.Abstractions;
using ClientMicroservice.Domain.Common;
using ClientMicroservice.Domain.Errors;

namespace ClientMicroservice.Application.Users.Queries.GetUserById;

public sealed class GetUserByIdQueryHandler(IUserRepository repository, IMapper mapper)
    : IRequestHandler<GetUserByIdQuery, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(GetUserByIdQuery query, CancellationToken ct)
    {
        var user = await repository.GetByIdAsync(query.Id, ct);
        if (user is null)
            return UserErrors.NotFound;

        return mapper.Map<UserDto>(user);
    }
}
```

- [ ] **Step 9.4: Create `GetUsersQuery.cs`**

`src/ClientMicroservice.Application/Users/Queries/GetUsers/GetUsersQuery.cs`:

```csharp
using MediatR;
using ClientMicroservice.Application.Common.DTOs;
using ClientMicroservice.Domain.Common;

namespace ClientMicroservice.Application.Users.Queries.GetUsers;

public sealed record GetUsersQuery(int PageNumber = 1, int PageSize = 20)
    : IRequest<Result<PagedList<UserDto>>>;
```

- [ ] **Step 9.5: Create `GetUsersQueryHandler.cs`**

`src/ClientMicroservice.Application/Users/Queries/GetUsers/GetUsersQueryHandler.cs`:

```csharp
using AutoMapper;
using MediatR;
using ClientMicroservice.Application.Common.DTOs;
using ClientMicroservice.Domain.Abstractions;
using ClientMicroservice.Domain.Common;

namespace ClientMicroservice.Application.Users.Queries.GetUsers;

public sealed class GetUsersQueryHandler(IUserRepository repository, IMapper mapper)
    : IRequestHandler<GetUsersQuery, Result<PagedList<UserDto>>>
{
    public async Task<Result<PagedList<UserDto>>> Handle(GetUsersQuery query, CancellationToken ct)
    {
        var paged = await repository.GetPagedAsync(query.PageNumber, query.PageSize, ct);
        var dtos = mapper.Map<List<UserDto>>(paged.Items);
        return new PagedList<UserDto>(dtos, paged.PageNumber, paged.PageSize, paged.TotalCount);
    }
}
```

- [ ] **Step 9.6: Run query tests**

```bash
dotnet test tests/ClientMicroservice.UnitTests/ClientMicroservice.UnitTests.csproj \
  --filter "FullyQualifiedName~GetUserByIdQueryHandlerTests" -v minimal
```

Expected: `2 passed, 0 failed`.

- [ ] **Step 9.7: Run all tests**

```bash
dotnet test tests/ClientMicroservice.UnitTests/ClientMicroservice.UnitTests.csproj -v minimal
```

Expected: `11 passed, 0 failed`.

- [ ] **Step 9.8: Commit**

```bash
git add src/ClientMicroservice.Application/Users/Queries/ \
        tests/ClientMicroservice.UnitTests/Users/Queries/
git commit -m "feat(application): add GetUserById and GetUsers queries"
```

---

## Task 10: Infrastructure — persistence

**Files:**
- Create: `src/ClientMicroservice.Infrastructure/Persistence/ApplicationDbContext.cs`
- Create: `src/ClientMicroservice.Infrastructure/Persistence/Configurations/UserConfiguration.cs`
- Create: `src/ClientMicroservice.Infrastructure/Persistence/Repositories/Repository.cs`
- Create: `src/ClientMicroservice.Infrastructure/Persistence/Repositories/UserRepository.cs`
- Create: `src/ClientMicroservice.Infrastructure/Persistence/UnitOfWork.cs`

- [ ] **Step 10.1: Create `ApplicationDbContext.cs`**

`src/ClientMicroservice.Infrastructure/Persistence/ApplicationDbContext.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using ClientMicroservice.Domain.Entities;

namespace ClientMicroservice.Infrastructure.Persistence;

internal sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
```

- [ ] **Step 10.2: Create `UserConfiguration.cs`**

`src/ClientMicroservice.Infrastructure/Persistence/Configurations/UserConfiguration.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ClientMicroservice.Domain.Entities;

namespace ClientMicroservice.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .HasColumnName("id");

        builder.Property(u => u.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(u => u.Email)
            .HasColumnName("email")
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.Property(u => u.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();
    }
}
```

- [ ] **Step 10.3: Create `Repository.cs`**

`src/ClientMicroservice.Infrastructure/Persistence/Repositories/Repository.cs`:

```csharp
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
```

- [ ] **Step 10.4: Create `UserRepository.cs`**

`src/ClientMicroservice.Infrastructure/Persistence/Repositories/UserRepository.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using ClientMicroservice.Domain.Abstractions;
using ClientMicroservice.Domain.Entities;

namespace ClientMicroservice.Infrastructure.Persistence.Repositories;

internal sealed class UserRepository(ApplicationDbContext context)
    : Repository<User>(context), IUserRepository
{
    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        => context.Set<User>()
            .FirstOrDefaultAsync(u => u.Email == email, ct);
}
```

- [ ] **Step 10.5: Create `UnitOfWork.cs`**

`src/ClientMicroservice.Infrastructure/Persistence/UnitOfWork.cs`:

```csharp
using ClientMicroservice.Domain.Abstractions;

namespace ClientMicroservice.Infrastructure.Persistence;

internal sealed class UnitOfWork(ApplicationDbContext context) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => context.SaveChangesAsync(ct);
}
```

- [ ] **Step 10.6: Create `DesignTimeDbContextFactory.cs`**

`src/ClientMicroservice.Infrastructure/Persistence/DesignTimeDbContextFactory.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ClientMicroservice.Infrastructure.Persistence;

internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=microservice_dev;Username=postgres;Password=postgres")
            .Options;
        return new ApplicationDbContext(opts);
    }
}
```

- [ ] **Step 10.7: Build Infrastructure**

```bash
dotnet build src/ClientMicroservice.Infrastructure/ClientMicroservice.Infrastructure.csproj
```

Expected: `Build succeeded`.

- [ ] **Step 10.8: Commit**

```bash
git add src/ClientMicroservice.Infrastructure/Persistence/
git commit -m "feat(infrastructure): add EF Core DbContext, Repository<T>, UnitOfWork, and design-time factory"
```

---

## Task 11: Infrastructure — messaging and DI registration

**Files:**
- Create: `src/ClientMicroservice.Infrastructure/Messaging/MassTransitEventBus.cs`
- Create: `src/ClientMicroservice.Infrastructure/Messaging/Consumers/UserCreatedEventConsumer.cs`
- Create: `src/ClientMicroservice.Infrastructure/DependencyInjection.cs`

- [ ] **Step 11.1: Create `MassTransitEventBus.cs`**

`src/ClientMicroservice.Infrastructure/Messaging/MassTransitEventBus.cs`:

```csharp
using MassTransit;
using ClientMicroservice.Contracts.Users;
using ClientMicroservice.Domain.Abstractions;
using ClientMicroservice.Domain.Events;

namespace ClientMicroservice.Infrastructure.Messaging;

internal sealed class MassTransitEventBus(IPublishEndpoint endpoint) : IEventBus
{
    public Task PublishAsync<T>(T message, CancellationToken ct = default) where T : class
        => message switch
        {
            UserCreatedDomainEvent e => endpoint.Publish(
                new UserCreatedEvent(e.UserId, e.Name, e.Email, e.CreatedAt), ct),
            UserUpdatedDomainEvent e => endpoint.Publish(
                new UserUpdatedEvent(e.UserId, e.Name, e.Email), ct),
            UserDeletedDomainEvent e => endpoint.Publish(
                new UserDeletedEvent(e.UserId), ct),
            _ => endpoint.Publish(message, ct)
        };
}
```

- [ ] **Step 11.2: Create `UserCreatedEventConsumer.cs`**

`src/ClientMicroservice.Infrastructure/Messaging/Consumers/UserCreatedEventConsumer.cs`:

```csharp
using MassTransit;
using Microsoft.Extensions.Logging;
using ClientMicroservice.Contracts.Users;

namespace ClientMicroservice.Infrastructure.Messaging.Consumers;

public sealed class UserCreatedEventConsumer(ILogger<UserCreatedEventConsumer> logger)
    : IConsumer<UserCreatedEvent>
{
    public Task Consume(ConsumeContext<UserCreatedEvent> context)
    {
        logger.LogInformation(
            "Received UserCreatedEvent for UserId={UserId}, Name={Name}",
            context.Message.UserId,
            context.Message.Name);

        // TODO: add your inbound event handling logic here
        return Task.CompletedTask;
    }
}
```

- [ ] **Step 11.3: Create `DependencyInjection.cs`**

`src/ClientMicroservice.Infrastructure/DependencyInjection.cs`:

```csharp
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ClientMicroservice.Domain.Abstractions;
using ClientMicroservice.Infrastructure.Messaging;
using ClientMicroservice.Infrastructure.Messaging.Consumers;
using ClientMicroservice.Infrastructure.Persistence;
using ClientMicroservice.Infrastructure.Persistence.Repositories;

namespace ClientMicroservice.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(opts =>
            opts.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IEventBus, MassTransitEventBus>();

        services.AddMassTransit(bus =>
        {
            bus.AddConsumer<UserCreatedEventConsumer>();

            bus.UsingRabbitMq((ctx, cfg) =>
            {
                cfg.Host(configuration["RabbitMq:Host"], h =>
                {
                    h.Username(configuration["RabbitMq:Username"]!);
                    h.Password(configuration["RabbitMq:Password"]!);
                });

                cfg.ConfigureEndpoints(ctx);
            });
        });

        return services;
    }
}
```

- [ ] **Step 11.4: Build Infrastructure**

```bash
dotnet build src/ClientMicroservice.Infrastructure/ClientMicroservice.Infrastructure.csproj
```

Expected: `Build succeeded`.

- [ ] **Step 11.5: Commit**

```bash
git add src/ClientMicroservice.Infrastructure/Messaging/ \
        src/ClientMicroservice.Infrastructure/DependencyInjection.cs
git commit -m "feat(infrastructure): add MassTransit event bus, consumer skeleton, and DI"
```

---

## Task 12: API — configuration, middleware, and Program.cs

**Files:**
- Modify: `src/ClientMicroservice.API/appsettings.json`
- Create: `src/ClientMicroservice.API/appsettings.Development.json`
- Create: `src/ClientMicroservice.API/Middleware/GlobalExceptionHandler.cs`
- Modify: `src/ClientMicroservice.API/Program.cs`

- [ ] **Step 12.1: Replace `appsettings.json`**

`src/ClientMicroservice.API/appsettings.json`:

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
    "Secret": "REPLACE_ME_WITH_32_CHAR_MIN_SECRET_KEY",
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
  },
  "AllowedHosts": "*"
}
```

- [ ] **Step 12.2: Create `appsettings.Development.json`**

`src/ClientMicroservice.API/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=microservice_dev;Username=postgres;Password=postgres"
  },
  "RabbitMq": {
    "Host": "localhost",
    "Username": "guest",
    "Password": "guest"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information"
    }
  }
}
```

- [ ] **Step 12.3: Create `GlobalExceptionHandler.cs`**

`src/ClientMicroservice.API/Middleware/GlobalExceptionHandler.cs`:

```csharp
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ClientMicroservice.Application.Common.Exceptions;

namespace ClientMicroservice.API.Middleware;

internal sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken ct)
    {
        logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);

        var (statusCode, title) = exception switch
        {
            AppValidationException => (StatusCodes.Status422UnprocessableEntity, "Validation Error"),
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error")
        };

        ProblemDetails problem;
        if (exception is AppValidationException validationEx)
        {
            problem = new ValidationProblemDetails(validationEx.Errors)
            {
                Status = statusCode,
                Title = title,
                Type = "https://tools.ietf.org/html/rfc4918#section-11.2"
            };
        }
        else
        {
            problem = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = exception.Message
            };
        }

        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(problem, ct);
        return true;
    }
}
```

- [ ] **Step 12.4: Replace `Program.cs`**

`src/ClientMicroservice.API/Program.cs`:

```csharp
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ClientMicroservice.API.Middleware;
using ClientMicroservice.Application;
using ClientMicroservice.Infrastructure;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var jwtSettings = builder.Configuration.GetSection("Jwt");
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings["Secret"]!))
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var otlpEndpoint = builder.Configuration["OpenTelemetry:Endpoint"];
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("ClientMicroservice"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSource("MassTransit")
        .AddOtlpExporter(opts => opts.Endpoint = new Uri(otlpEndpoint!)))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter(opts => opts.Endpoint = new Uri(otlpEndpoint!)));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(opts =>
    {
        opts.WithTitle("ClientMicroservice API")
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

- [ ] **Step 12.5: Build API**

```bash
dotnet build src/ClientMicroservice.API/ClientMicroservice.API.csproj
```

Expected: `Build succeeded`.

- [ ] **Step 12.6: Commit**

```bash
git add src/ClientMicroservice.API/appsettings.json \
        src/ClientMicroservice.API/appsettings.Development.json \
        src/ClientMicroservice.API/Middleware/ \
        src/ClientMicroservice.API/Program.cs
git commit -m "feat(api): configure JWT, OpenTelemetry, exception handling, and Scalar UI"
```

---

## Task 13: API — ControllerExtensions and UsersController

**Files:**
- Create: `src/ClientMicroservice.API/Extensions/ControllerExtensions.cs`
- Create: `src/ClientMicroservice.API/Controllers/UsersController.cs`

- [ ] **Step 13.1: Create `ControllerExtensions.cs`**

`src/ClientMicroservice.API/Extensions/ControllerExtensions.cs`:

```csharp
using Microsoft.AspNetCore.Mvc;
using ClientMicroservice.Domain.Common;

namespace ClientMicroservice.API.Extensions;

public static class ControllerExtensions
{
    private const string NotFoundCode = "NotFound";

    public static IActionResult ToOkResult<T>(this ControllerBase controller, Result<T> result)
        => result.IsSuccess
            ? controller.Ok(result.Value)
            : ToErrorResult(controller, result.Error);

    public static IActionResult ToCreatedResult<T>(
        this ControllerBase controller,
        Result<T> result,
        string actionName,
        object? routeValues = null)
        => result.IsSuccess
            ? controller.CreatedAtAction(actionName, routeValues, result.Value)
            : ToErrorResult(controller, result.Error);

    public static IActionResult ToNoContentResult<T>(
        this ControllerBase controller, Result<T> result)
        => result.IsSuccess
            ? controller.NoContent()
            : ToErrorResult(controller, result.Error);

    private static IActionResult ToErrorResult(ControllerBase controller, Error error)
    {
        if (error.Code.EndsWith(NotFoundCode, StringComparison.OrdinalIgnoreCase) ||
            error.Code.Contains(".NotFound", StringComparison.OrdinalIgnoreCase))
        {
            return controller.NotFound(new ProblemDetails
            {
                Title = "Not Found",
                Detail = error.Message,
                Status = StatusCodes.Status404NotFound
            });
        }

        return controller.BadRequest(new ProblemDetails
        {
            Title = "Bad Request",
            Detail = error.Message,
            Status = StatusCodes.Status400BadRequest
        });
    }
}
```

- [ ] **Step 13.2: Create `UsersController.cs`**

`src/ClientMicroservice.API/Controllers/UsersController.cs`:

```csharp
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ClientMicroservice.API.Extensions;
using ClientMicroservice.Application.Users.Commands.CreateUser;
using ClientMicroservice.Application.Users.Commands.DeleteUser;
using ClientMicroservice.Application.Users.Commands.UpdateUser;
using ClientMicroservice.Application.Users.Queries.GetUserById;
using ClientMicroservice.Application.Users.Queries.GetUsers;

namespace ClientMicroservice.API.Controllers;

[ApiController]
[Authorize]
[Route("users")]
public sealed class UsersController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetUsersQuery(pageNumber, pageSize), ct);
        return this.ToOkResult(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetUserById(Guid id, CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetUserByIdQuery(id), ct);
        return this.ToOkResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser(
        [FromBody] CreateUserCommand command,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(command, ct);
        return this.ToCreatedResult(
            result,
            actionName: nameof(GetUserById),
            routeValues: new { id = result.IsSuccess ? result.Value : Guid.Empty });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateUser(
        Guid id,
        [FromBody] UpdateUserRequest request,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(
            new UpdateUserCommand(id, request.Name, request.Email), ct);
        return this.ToNoContentResult(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteUser(Guid id, CancellationToken ct = default)
    {
        var result = await mediator.Send(new DeleteUserCommand(id), ct);
        return this.ToNoContentResult(result);
    }
}

public sealed record UpdateUserRequest(string Name, string Email);
```

- [ ] **Step 13.3: Build full solution**

```bash
dotnet build ClientMicroservice.slnx
```

Expected: `Build succeeded, 0 Error(s)`.

- [ ] **Step 13.4: Run all tests**

```bash
dotnet test tests/ClientMicroservice.UnitTests/ClientMicroservice.UnitTests.csproj -v minimal
```

Expected: `11 passed, 0 failed`.

- [ ] **Step 13.5: Commit**

```bash
git add src/ClientMicroservice.API/
git commit -m "feat(api): add UsersController with 5 endpoints and ControllerExtensions"
```

---

## Task 14: Unit tests — ValidationBehavior and Validator (verify full suite)

The tests for `ValidationBehaviorTests` and `CreateUserCommandValidatorTests` were created in Tasks 6 and 7. This task verifies they all pass and adds any remaining tests identified during review.

- [ ] **Step 14.1: Run full test suite with verbosity**

```bash
dotnet test tests/ClientMicroservice.UnitTests/ClientMicroservice.UnitTests.csproj -v normal
```

Expected output lists all test names and shows `11 passed, 0 failed`.

- [ ] **Step 14.2: Verify test coverage of key scenarios**

Confirm these tests exist and pass:
- `CreateUserCommandHandlerTests.Handle_WithValidData_CreatesUserAndReturnsId` ✓
- `CreateUserCommandHandlerTests.Handle_WhenEmailAlreadyTaken_ReturnsEmailTakenError` ✓
- `GetUserByIdQueryHandlerTests.Handle_WhenUserExists_ReturnsUserDto` ✓
- `GetUserByIdQueryHandlerTests.Handle_WhenUserNotFound_ReturnsNotFoundError` ✓
- `ValidationBehaviorTests.Handle_WhenValidationFails_ThrowsAppValidationException` ✓
- `ValidationBehaviorTests.Handle_WhenValidationPasses_CallsNext` ✓
- `ValidationBehaviorTests.Handle_WhenNoValidators_CallsNext` ✓
- `CreateUserCommandValidatorTests.Validate_WithValidData_Passes` ✓
- `CreateUserCommandValidatorTests.Validate_WithEmptyName_Fails` ✓
- `CreateUserCommandValidatorTests.Validate_WithInvalidEmail_Fails` ✓
- `CreateUserCommandValidatorTests.Validate_WithNameExceedingMaxLength_Fails` ✓

- [ ] **Step 14.3: Commit if any fixes were needed**

```bash
git add tests/
git commit -m "test: verify full unit test suite passes (11 tests)"
```

---

## Task 15: Docker

**Files:**
- Create: `Dockerfile`
- Create: `docker-compose.yml`

- [ ] **Step 15.1: Create multi-stage `Dockerfile`**

`Dockerfile` (at solution root):

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ClientMicroservice.slnx ./
COPY src/ClientMicroservice.Domain/ClientMicroservice.Domain.csproj src/ClientMicroservice.Domain/
COPY src/ClientMicroservice.Application/ClientMicroservice.Application.csproj src/ClientMicroservice.Application/
COPY src/ClientMicroservice.Infrastructure/ClientMicroservice.Infrastructure.csproj src/ClientMicroservice.Infrastructure/
COPY src/ClientMicroservice.Contracts/ClientMicroservice.Contracts.csproj src/ClientMicroservice.Contracts/
COPY src/ClientMicroservice.API/ClientMicroservice.API.csproj src/ClientMicroservice.API/

RUN dotnet restore src/ClientMicroservice.API/ClientMicroservice.API.csproj

COPY . .
RUN dotnet publish src/ClientMicroservice.API/ClientMicroservice.API.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ClientMicroservice.API.dll"]
```

- [ ] **Step 15.2: Create `docker-compose.yml`**

`docker-compose.yml` (at solution root):

```yaml
services:
  api:
    build:
      context: .
      dockerfile: Dockerfile
    ports:
      - "8080:8080"
    environment:
      ConnectionStrings__DefaultConnection: "Host=postgres;Database=microservice;Username=postgres;Password=postgres"
      RabbitMq__Host: rabbitmq
      RabbitMq__Username: guest
      RabbitMq__Password: guest
      Jwt__Secret: "dev-secret-min-32-chars-replace-in-prod"
      Jwt__Issuer: ClientMicroservice
      Jwt__Audience: ClientMicroservice
      OpenTelemetry__Endpoint: "http://localhost:4317"
    depends_on:
      postgres:
        condition: service_healthy
      rabbitmq:
        condition: service_healthy

  postgres:
    image: postgres:17-alpine
    environment:
      POSTGRES_DB: microservice
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 5s
      timeout: 5s
      retries: 5

  rabbitmq:
    image: rabbitmq:3-management-alpine
    ports:
      - "5672:5672"
      - "15672:15672"
    healthcheck:
      test: ["CMD", "rabbitmq-diagnostics", "ping"]
      interval: 10s
      timeout: 5s
      retries: 5

volumes:
  postgres_data:
```

- [ ] **Step 15.3: Verify Dockerfile builds**

```bash
docker build -t microservice-template:local .
```

Expected: `Successfully built <image-id>` and `Successfully tagged microservice-template:local`.

- [ ] **Step 15.4: Commit**

```bash
git add Dockerfile docker-compose.yml
git commit -m "feat(docker): add multi-stage Dockerfile and docker-compose with Postgres and RabbitMQ"
```

---

## Task 16: EF Core migrations

- [ ] **Step 16.1: Install EF Core tools (if not already installed)**

```bash
dotnet tool install --global dotnet-ef
```

Expected: `Tool 'dotnet-ef' is already installed` or `successfully installed`.

- [ ] **Step 16.2: Add initial migration**

Run from solution root:

```bash
dotnet ef migrations add InitialCreate \
  --project src/ClientMicroservice.Infrastructure/ClientMicroservice.Infrastructure.csproj \
  --startup-project src/ClientMicroservice.API/ClientMicroservice.API.csproj \
  --output-dir Persistence/Migrations
```

Expected: `Build succeeded.` and `Done. To undo this action, use 'ef migrations remove'`.

- [ ] **Step 16.3: Verify migration files were created**

```bash
ls src/ClientMicroservice.Infrastructure/Persistence/Migrations/
```

Expected: files like `20260524_InitialCreate.cs` and `ApplicationDbContextModelSnapshot.cs`.

- [ ] **Step 16.4: Commit**

```bash
git add src/ClientMicroservice.Infrastructure/Persistence/Migrations/
git commit -m "feat(infrastructure): add initial EF Core migration for users table"
```

---

## Task 17: Final verification

- [ ] **Step 17.1: Full solution build**

```bash
dotnet build ClientMicroservice.slnx -c Release
```

Expected: `Build succeeded. 0 Error(s)`.

- [ ] **Step 17.2: Full test run**

```bash
dotnet test tests/ClientMicroservice.UnitTests/ClientMicroservice.UnitTests.csproj -c Release
```

Expected: `11 passed, 0 failed, 0 skipped`.

- [ ] **Step 17.3: Final commit**

```bash
git add .
git commit -m "chore: complete microservice template implementation"
```
