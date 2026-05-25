# Client Domain Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the User domain with Client, add Address/BankingDetails value objects, Redis caching for GET-by-id, and Azure Blob Storage for profile picture uploads.

**Architecture:** Cache-aside in `GetClientByIdQueryHandler` via `ICacheService` (implemented by `RedisCacheService`). Profile picture upload via `IStorageService` (implemented by `AzureBlobStorageService`), which returns a public URL stored on the `Client` entity. PATCH commands invalidate the Redis cache key `client:{id}`. All 6 endpoints require JWT Bearer.

**Tech Stack:** .NET 10, EF Core 10 + PostgreSQL, MediatR 14, AutoMapper 16, FluentValidation 12, Microsoft.Extensions.Caching.StackExchangeRedis, Azure.Storage.Blobs, xUnit + Moq

---

### Task 1: Delete User* files

**Files:**
- Delete: `src/ClientMicroservice.Domain/Entities/User.cs`
- Delete: `src/ClientMicroservice.Domain/Errors/UserErrors.cs`
- Delete: `src/ClientMicroservice.Domain/Abstractions/IUserRepository.cs`
- Delete: `src/ClientMicroservice.Domain/Events/UserCreatedDomainEvent.cs`
- Delete: `src/ClientMicroservice.Domain/Events/UserUpdatedDomainEvent.cs`
- Delete: `src/ClientMicroservice.Domain/Events/UserDeletedDomainEvent.cs`
- Delete: `src/ClientMicroservice.Application/Common/DTOs/UserDto.cs`
- Delete: `src/ClientMicroservice.Application/Users/` (entire directory)
- Delete: `src/ClientMicroservice.Infrastructure/Persistence/Configurations/UserConfiguration.cs`
- Delete: `src/ClientMicroservice.Infrastructure/Persistence/Repositories/UserRepository.cs`
- Delete: `src/ClientMicroservice.Infrastructure/Messaging/Consumers/UserCreatedEventConsumer.cs`
- Delete: `src/ClientMicroservice.Contracts/Users/` (entire directory)
- Delete: `src/ClientMicroservice.API/Controllers/UsersController.cs`
- Delete: `tests/ClientMicroservice.UnitTests/Users/` (entire directory)
- Delete: `src/ClientMicroservice.Infrastructure/Persistence/Migrations/` (entire directory)

- [ ] **Step 1: Remove all User* source files and old migrations**

```bash
git rm -r \
  src/ClientMicroservice.Domain/Entities/User.cs \
  src/ClientMicroservice.Domain/Errors/UserErrors.cs \
  src/ClientMicroservice.Domain/Abstractions/IUserRepository.cs \
  src/ClientMicroservice.Domain/Events/UserCreatedDomainEvent.cs \
  src/ClientMicroservice.Domain/Events/UserUpdatedDomainEvent.cs \
  src/ClientMicroservice.Domain/Events/UserDeletedDomainEvent.cs \
  src/ClientMicroservice.Application/Common/DTOs/UserDto.cs \
  src/ClientMicroservice.Application/Users/ \
  src/ClientMicroservice.Infrastructure/Persistence/Configurations/UserConfiguration.cs \
  src/ClientMicroservice.Infrastructure/Persistence/Repositories/UserRepository.cs \
  src/ClientMicroservice.Infrastructure/Messaging/Consumers/UserCreatedEventConsumer.cs \
  src/ClientMicroservice.Contracts/Users/ \
  src/ClientMicroservice.API/Controllers/UsersController.cs \
  tests/ClientMicroservice.UnitTests/Users/ \
  src/ClientMicroservice.Infrastructure/Persistence/Migrations/
```

- [ ] **Step 2: Commit**

```bash
git commit -m "chore: remove User domain files to make way for Client domain"
```

---

### Task 2: Domain — Value Objects

**Files:**
- Create: `src/ClientMicroservice.Domain/ValueObjects/Address.cs`
- Create: `src/ClientMicroservice.Domain/ValueObjects/BankingDetails.cs`

- [ ] **Step 1: Create Address value object**

`src/ClientMicroservice.Domain/ValueObjects/Address.cs`:
```csharp
namespace ClientMicroservice.Domain.ValueObjects;

public record Address(
    string Street,
    string City,
    string State,
    string ZipCode,
    string Country);
```

- [ ] **Step 2: Create BankingDetails value object**

`src/ClientMicroservice.Domain/ValueObjects/BankingDetails.cs`:
```csharp
namespace ClientMicroservice.Domain.ValueObjects;

public record BankingDetails(string Agency, string AccountNumber);
```

- [ ] **Step 3: Build Domain project**

```bash
dotnet build src/ClientMicroservice.Domain/ClientMicroservice.Domain.csproj
```
Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add src/ClientMicroservice.Domain/ValueObjects/
git commit -m "feat: add Address and BankingDetails value objects"
```

---

### Task 3: Domain — Client Entity

**Files:**
- Create: `src/ClientMicroservice.Domain/Entities/Client.cs`

- [ ] **Step 1: Create Client entity**

`src/ClientMicroservice.Domain/Entities/Client.cs`:
```csharp
using ClientMicroservice.Domain.ValueObjects;

namespace ClientMicroservice.Domain.Entities;

public sealed class Client
{
    private Client() { }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public Address Address { get; private set; } = null!;
    public string? ProfilePictureUrl { get; private set; }
    public BankingDetails BankingDetails { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }

    public static Client Create(
        string name,
        string email,
        Address address,
        BankingDetails bankingDetails) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Email = email,
        Address = address,
        BankingDetails = bankingDetails,
        CreatedAt = DateTimeOffset.UtcNow
    };

    public void Update(string? name, string? email, Address? address, BankingDetails? bankingDetails)
    {
        if (name is not null) Name = name;
        if (email is not null) Email = email;
        if (address is not null) Address = address;
        if (bankingDetails is not null) BankingDetails = bankingDetails;
    }

    public void SetProfilePicture(string url) => ProfilePictureUrl = url;
}
```

- [ ] **Step 2: Build Domain project**

```bash
dotnet build src/ClientMicroservice.Domain/ClientMicroservice.Domain.csproj
```
Expected: `Build succeeded.`

- [ ] **Step 3: Commit**

```bash
git add src/ClientMicroservice.Domain/Entities/Client.cs
git commit -m "feat: add Client entity with partial update and profile picture support"
```

---

### Task 4: Domain — Errors, Events, Repository Interface

**Files:**
- Create: `src/ClientMicroservice.Domain/Errors/ClientErrors.cs`
- Create: `src/ClientMicroservice.Domain/Events/ClientCreatedDomainEvent.cs`
- Create: `src/ClientMicroservice.Domain/Events/ClientUpdatedDomainEvent.cs`
- Create: `src/ClientMicroservice.Domain/Events/ClientDeletedDomainEvent.cs`
- Create: `src/ClientMicroservice.Domain/Abstractions/IClientRepository.cs`

- [ ] **Step 1: Create ClientErrors**

`src/ClientMicroservice.Domain/Errors/ClientErrors.cs`:
```csharp
using ClientMicroservice.Domain.Common;

namespace ClientMicroservice.Domain.Errors;

public static class ClientErrors
{
    public static readonly Error NotFound = new("Client.NotFound", "Client was not found.");
    public static readonly Error EmailTaken = new("Client.EmailTaken", "Email address is already in use.");
}
```

- [ ] **Step 2: Create domain events**

`src/ClientMicroservice.Domain/Events/ClientCreatedDomainEvent.cs`:
```csharp
using ClientMicroservice.Domain.Abstractions;

namespace ClientMicroservice.Domain.Events;

public record ClientCreatedDomainEvent(
    Guid ClientId,
    string Name,
    string Email,
    DateTimeOffset CreatedAt) : IDomainEvent;
```

`src/ClientMicroservice.Domain/Events/ClientUpdatedDomainEvent.cs`:
```csharp
using ClientMicroservice.Domain.Abstractions;

namespace ClientMicroservice.Domain.Events;

public record ClientUpdatedDomainEvent(Guid ClientId) : IDomainEvent;
```

`src/ClientMicroservice.Domain/Events/ClientDeletedDomainEvent.cs`:
```csharp
using ClientMicroservice.Domain.Abstractions;

namespace ClientMicroservice.Domain.Events;

public record ClientDeletedDomainEvent(Guid ClientId) : IDomainEvent;
```

- [ ] **Step 3: Create IClientRepository**

`src/ClientMicroservice.Domain/Abstractions/IClientRepository.cs`:
```csharp
using ClientMicroservice.Domain.Entities;

namespace ClientMicroservice.Domain.Abstractions;

public interface IClientRepository : IRepository<Client>
{
    Task<Client?> GetByEmailAsync(string email, CancellationToken ct = default);
}
```

- [ ] **Step 4: Build Domain project**

```bash
dotnet build src/ClientMicroservice.Domain/ClientMicroservice.Domain.csproj
```
Expected: `Build succeeded.`

- [ ] **Step 5: Commit**

```bash
git add \
  src/ClientMicroservice.Domain/Errors/ \
  src/ClientMicroservice.Domain/Events/ \
  src/ClientMicroservice.Domain/Abstractions/IClientRepository.cs
git commit -m "feat: add ClientErrors, domain events, and IClientRepository"
```

---

### Task 5: Contracts — Client Integration Events

**Files:**
- Create: `src/ClientMicroservice.Contracts/Clients/ClientCreatedEvent.cs`
- Create: `src/ClientMicroservice.Contracts/Clients/ClientUpdatedEvent.cs`
- Create: `src/ClientMicroservice.Contracts/Clients/ClientDeletedEvent.cs`

- [ ] **Step 1: Create contract events**

`src/ClientMicroservice.Contracts/Clients/ClientCreatedEvent.cs`:
```csharp
namespace ClientMicroservice.Contracts.Clients;

public record ClientCreatedEvent(Guid ClientId, string Name, string Email, DateTimeOffset CreatedAt);
```

`src/ClientMicroservice.Contracts/Clients/ClientUpdatedEvent.cs`:
```csharp
namespace ClientMicroservice.Contracts.Clients;

public record ClientUpdatedEvent(Guid ClientId);
```

`src/ClientMicroservice.Contracts/Clients/ClientDeletedEvent.cs`:
```csharp
namespace ClientMicroservice.Contracts.Clients;

public record ClientDeletedEvent(Guid ClientId);
```

- [ ] **Step 2: Build Contracts project**

```bash
dotnet build src/ClientMicroservice.Contracts/ClientMicroservice.Contracts.csproj
```
Expected: `Build succeeded.`

- [ ] **Step 3: Commit**

```bash
git add src/ClientMicroservice.Contracts/Clients/
git commit -m "feat: add Client integration event contracts"
```

---

### Task 6: Application — Interfaces, DTOs, FileData, Mapping Profile

**Files:**
- Create: `src/ClientMicroservice.Application/Common/Interfaces/ICacheService.cs`
- Create: `src/ClientMicroservice.Application/Common/Interfaces/IStorageService.cs`
- Create: `src/ClientMicroservice.Application/Common/FileData.cs`
- Create: `src/ClientMicroservice.Application/Common/DTOs/ClientDto.cs`
- Create: `src/ClientMicroservice.Application/Clients/Mappings/ClientMappingProfile.cs`

- [ ] **Step 1: Create ICacheService**

`src/ClientMicroservice.Application/Common/Interfaces/ICacheService.cs`:
```csharp
namespace ClientMicroservice.Application.Common.Interfaces;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct);
    Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct);
    Task RemoveAsync(string key, CancellationToken ct);
}
```

- [ ] **Step 2: Create IStorageService**

`src/ClientMicroservice.Application/Common/Interfaces/IStorageService.cs`:
```csharp
namespace ClientMicroservice.Application.Common.Interfaces;

public interface IStorageService
{
    Task<string> UploadAsync(Stream content, string fileName, string contentType, CancellationToken ct);
}
```

- [ ] **Step 3: Create FileData**

`src/ClientMicroservice.Application/Common/FileData.cs`:
```csharp
namespace ClientMicroservice.Application.Common;

public sealed record FileData(Stream Content, string FileName, string ContentType);
```

- [ ] **Step 4: Create ClientDto with nested DTOs**

`src/ClientMicroservice.Application/Common/DTOs/ClientDto.cs`:
```csharp
namespace ClientMicroservice.Application.Common.DTOs;

public record ClientDto(
    Guid Id,
    string Name,
    string Email,
    AddressDto Address,
    string? ProfilePictureUrl,
    BankingDetailsDto BankingDetails,
    DateTimeOffset CreatedAt);

public record AddressDto(string Street, string City, string State, string ZipCode, string Country);

public record BankingDetailsDto(string Agency, string AccountNumber);
```

- [ ] **Step 5: Create ClientMappingProfile**

`src/ClientMicroservice.Application/Clients/Mappings/ClientMappingProfile.cs`:
```csharp
using AutoMapper;
using ClientMicroservice.Application.Common.DTOs;
using ClientMicroservice.Domain.Entities;
using ClientMicroservice.Domain.ValueObjects;

namespace ClientMicroservice.Application.Clients.Mappings;

public sealed class ClientMappingProfile : Profile
{
    public ClientMappingProfile()
    {
        CreateMap<Address, AddressDto>();
        CreateMap<BankingDetails, BankingDetailsDto>();
        CreateMap<Client, ClientDto>();
    }
}
```

- [ ] **Step 6: Build Application project**

```bash
dotnet build src/ClientMicroservice.Application/ClientMicroservice.Application.csproj
```
Expected: `Build succeeded.`

- [ ] **Step 7: Commit**

```bash
git add \
  src/ClientMicroservice.Application/Common/Interfaces/ \
  src/ClientMicroservice.Application/Common/FileData.cs \
  src/ClientMicroservice.Application/Common/DTOs/ClientDto.cs \
  src/ClientMicroservice.Application/Clients/Mappings/ClientMappingProfile.cs
git commit -m "feat: add Application interfaces, DTOs, FileData, and ClientMappingProfile"
```

---

### Task 7: Application — CreateClient (TDD)

**Files:**
- Create: `tests/ClientMicroservice.UnitTests/Clients/Commands/CreateClientCommandHandlerTests.cs`
- Create: `src/ClientMicroservice.Application/Clients/Commands/CreateClient/CreateClientCommand.cs`
- Create: `src/ClientMicroservice.Application/Clients/Commands/CreateClient/CreateClientCommandHandler.cs`
- Create: `src/ClientMicroservice.Application/Clients/Commands/CreateClient/CreateClientCommandValidator.cs`

- [ ] **Step 1: Write the failing tests**

`tests/ClientMicroservice.UnitTests/Clients/Commands/CreateClientCommandHandlerTests.cs`:
```csharp
using ClientMicroservice.Application.Clients.Commands.CreateClient;
using ClientMicroservice.Domain.Abstractions;
using ClientMicroservice.Domain.Common;
using ClientMicroservice.Domain.Entities;
using ClientMicroservice.Domain.Errors;
using ClientMicroservice.Domain.Events;
using ClientMicroservice.Domain.ValueObjects;
using Moq;

namespace ClientMicroservice.UnitTests.Clients.Commands;

public sealed class CreateClientCommandHandlerTests
{
    private readonly Mock<IClientRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IEventBus> _eventBusMock = new();
    private readonly CreateClientCommandHandler _handler;

    private static readonly Address _address = new("123 Main St", "Springfield", "IL", "62701", "US");
    private static readonly BankingDetails _banking = new("0001", "12345-6");

    public CreateClientCommandHandlerTests()
    {
        _handler = new CreateClientCommandHandler(
            _repoMock.Object, _uowMock.Object, _eventBusMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidData_CreatesClientAndReturnsId()
    {
        _repoMock.Setup(r => r.GetByEmailAsync("john@example.com", It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Client?)null);

        var command = new CreateClientCommand("John Doe", "john@example.com", _address, _banking);
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<Client>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _eventBusMock.Verify(e => e.PublishAsync(
            It.IsAny<ClientCreatedDomainEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenEmailAlreadyTaken_ReturnsEmailTakenError()
    {
        var existing = Client.Create("Existing", "john@example.com", _address, _banking);
        _repoMock.Setup(r => r.GetByEmailAsync("john@example.com", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(existing);

        var command = new CreateClientCommand("John Doe", "john@example.com", _address, _banking);
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ClientErrors.EmailTaken.Code, result.Error.Code);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<Client>(), It.IsAny<CancellationToken>()), Times.Never);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
```

- [ ] **Step 2: Run tests to confirm compilation error (red)**

```bash
dotnet test tests/ClientMicroservice.UnitTests/ --filter "FullyQualifiedName~CreateClientCommandHandlerTests" 2>&1 | head -20
```
Expected: Build error — `CreateClientCommand`, `CreateClientCommandHandler` not found.

- [ ] **Step 3: Create CreateClientCommand**

`src/ClientMicroservice.Application/Clients/Commands/CreateClient/CreateClientCommand.cs`:
```csharp
using MediatR;
using ClientMicroservice.Domain.Common;
using ClientMicroservice.Domain.ValueObjects;

namespace ClientMicroservice.Application.Clients.Commands.CreateClient;

public sealed record CreateClientCommand(
    string Name,
    string Email,
    Address Address,
    BankingDetails BankingDetails
) : IRequest<Result<Guid>>;
```

- [ ] **Step 4: Create CreateClientCommandHandler**

`src/ClientMicroservice.Application/Clients/Commands/CreateClient/CreateClientCommandHandler.cs`:
```csharp
using MediatR;
using ClientMicroservice.Domain.Abstractions;
using ClientMicroservice.Domain.Common;
using ClientMicroservice.Domain.Entities;
using ClientMicroservice.Domain.Errors;
using ClientMicroservice.Domain.Events;

namespace ClientMicroservice.Application.Clients.Commands.CreateClient;

public sealed class CreateClientCommandHandler(
    IClientRepository repository,
    IUnitOfWork unitOfWork,
    IEventBus eventBus)
    : IRequestHandler<CreateClientCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateClientCommand command, CancellationToken ct)
    {
        var existing = await repository.GetByEmailAsync(command.Email, ct);
        if (existing is not null)
            return ClientErrors.EmailTaken;

        var client = Client.Create(command.Name, command.Email, command.Address, command.BankingDetails);
        await repository.AddAsync(client, ct);
        await unitOfWork.SaveChangesAsync(ct);

        await eventBus.PublishAsync(
            new ClientCreatedDomainEvent(client.Id, client.Name, client.Email, client.CreatedAt), ct);

        return client.Id;
    }
}
```

- [ ] **Step 5: Create CreateClientCommandValidator**

`src/ClientMicroservice.Application/Clients/Commands/CreateClient/CreateClientCommandValidator.cs`:
```csharp
using FluentValidation;

namespace ClientMicroservice.Application.Clients.Commands.CreateClient;

public sealed class CreateClientCommandValidator : AbstractValidator<CreateClientCommand>
{
    public CreateClientCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.Address).NotNull();
        RuleFor(x => x.Address.Street).NotEmpty().MaximumLength(200).When(x => x.Address is not null);
        RuleFor(x => x.Address.City).NotEmpty().MaximumLength(100).When(x => x.Address is not null);
        RuleFor(x => x.Address.State).NotEmpty().MaximumLength(100).When(x => x.Address is not null);
        RuleFor(x => x.Address.ZipCode).NotEmpty().MaximumLength(20).When(x => x.Address is not null);
        RuleFor(x => x.Address.Country).NotEmpty().MaximumLength(100).When(x => x.Address is not null);
        RuleFor(x => x.BankingDetails).NotNull();
        RuleFor(x => x.BankingDetails.Agency).NotEmpty().MaximumLength(50).When(x => x.BankingDetails is not null);
        RuleFor(x => x.BankingDetails.AccountNumber).NotEmpty().MaximumLength(50).When(x => x.BankingDetails is not null);
    }
}
```

- [ ] **Step 6: Run tests to confirm they pass (green)**

```bash
dotnet test tests/ClientMicroservice.UnitTests/ --filter "FullyQualifiedName~CreateClientCommandHandlerTests" -v
```
Expected: 2 tests pass.

- [ ] **Step 7: Commit**

```bash
git add \
  tests/ClientMicroservice.UnitTests/Clients/Commands/CreateClientCommandHandlerTests.cs \
  src/ClientMicroservice.Application/Clients/Commands/CreateClient/
git commit -m "feat: add CreateClient command with handler and validator (TDD)"
```

---

### Task 8: Application — UpdateClient (TDD)

**Files:**
- Create: `tests/ClientMicroservice.UnitTests/Clients/Commands/UpdateClientCommandHandlerTests.cs`
- Create: `src/ClientMicroservice.Application/Clients/Commands/UpdateClient/UpdateClientCommand.cs`
- Create: `src/ClientMicroservice.Application/Clients/Commands/UpdateClient/UpdateClientCommandHandler.cs`
- Create: `src/ClientMicroservice.Application/Clients/Commands/UpdateClient/UpdateClientCommandValidator.cs`

- [ ] **Step 1: Write the failing tests**

`tests/ClientMicroservice.UnitTests/Clients/Commands/UpdateClientCommandHandlerTests.cs`:
```csharp
using ClientMicroservice.Application.Clients.Commands.UpdateClient;
using ClientMicroservice.Application.Common.Interfaces;
using ClientMicroservice.Domain.Abstractions;
using ClientMicroservice.Domain.Common;
using ClientMicroservice.Domain.Entities;
using ClientMicroservice.Domain.Errors;
using ClientMicroservice.Domain.Events;
using ClientMicroservice.Domain.ValueObjects;
using Moq;

namespace ClientMicroservice.UnitTests.Clients.Commands;

public sealed class UpdateClientCommandHandlerTests
{
    private readonly Mock<IClientRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IEventBus> _eventBusMock = new();
    private readonly Mock<ICacheService> _cacheMock = new();
    private readonly UpdateClientCommandHandler _handler;

    private static readonly Address _address = new("123 Main St", "Springfield", "IL", "62701", "US");
    private static readonly BankingDetails _banking = new("0001", "12345-6");

    public UpdateClientCommandHandlerTests()
    {
        _handler = new UpdateClientCommandHandler(
            _repoMock.Object, _uowMock.Object, _eventBusMock.Object, _cacheMock.Object);
    }

    [Fact]
    public async Task Handle_WhenClientExists_UpdatesAndInvalidatesCache()
    {
        var clientId = Guid.NewGuid();
        var client = Client.Create("Old Name", "old@example.com", _address, _banking);

        _repoMock.Setup(r => r.GetByIdAsync(clientId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(client);

        var command = new UpdateClientCommand(clientId, "New Name", null, null, null);
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(c => c.RemoveAsync($"client:{clientId}", It.IsAny<CancellationToken>()), Times.Once);
        _eventBusMock.Verify(e => e.PublishAsync(
            It.IsAny<ClientUpdatedDomainEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenClientNotFound_ReturnsNotFoundError()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Client?)null);

        var result = await _handler.Handle(
            new UpdateClientCommand(Guid.NewGuid(), "Name", null, null, null), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ClientErrors.NotFound.Code, result.Error.Code);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
```

- [ ] **Step 2: Run tests to confirm compilation error (red)**

```bash
dotnet test tests/ClientMicroservice.UnitTests/ --filter "FullyQualifiedName~UpdateClientCommandHandlerTests" 2>&1 | head -20
```
Expected: Build error — types not found.

- [ ] **Step 3: Create UpdateClientCommand**

`src/ClientMicroservice.Application/Clients/Commands/UpdateClient/UpdateClientCommand.cs`:
```csharp
using MediatR;
using ClientMicroservice.Domain.Common;
using ClientMicroservice.Domain.ValueObjects;
using Unit = ClientMicroservice.Domain.Common.Unit;

namespace ClientMicroservice.Application.Clients.Commands.UpdateClient;

public sealed record UpdateClientCommand(
    Guid Id,
    string? Name,
    string? Email,
    Address? Address,
    BankingDetails? BankingDetails
) : IRequest<Result<Unit>>;
```

- [ ] **Step 4: Create UpdateClientCommandHandler**

`src/ClientMicroservice.Application/Clients/Commands/UpdateClient/UpdateClientCommandHandler.cs`:
```csharp
using MediatR;
using ClientMicroservice.Application.Common.Interfaces;
using ClientMicroservice.Domain.Abstractions;
using ClientMicroservice.Domain.Common;
using ClientMicroservice.Domain.Errors;
using ClientMicroservice.Domain.Events;
using Unit = ClientMicroservice.Domain.Common.Unit;

namespace ClientMicroservice.Application.Clients.Commands.UpdateClient;

public sealed class UpdateClientCommandHandler(
    IClientRepository repository,
    IUnitOfWork unitOfWork,
    IEventBus eventBus,
    ICacheService cacheService)
    : IRequestHandler<UpdateClientCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(UpdateClientCommand command, CancellationToken ct)
    {
        var client = await repository.GetByIdAsync(command.Id, ct);
        if (client is null)
            return ClientErrors.NotFound;

        client.Update(command.Name, command.Email, command.Address, command.BankingDetails);
        repository.Update(client);
        await unitOfWork.SaveChangesAsync(ct);

        await cacheService.RemoveAsync($"client:{command.Id}", ct);
        await eventBus.PublishAsync(new ClientUpdatedDomainEvent(client.Id), ct);

        return Unit.Value;
    }
}
```

- [ ] **Step 5: Create UpdateClientCommandValidator**

`src/ClientMicroservice.Application/Clients/Commands/UpdateClient/UpdateClientCommandValidator.cs`:
```csharp
using FluentValidation;

namespace ClientMicroservice.Application.Clients.Commands.UpdateClient;

public sealed class UpdateClientCommandValidator : AbstractValidator<UpdateClientCommand>
{
    public UpdateClientCommandValidator()
    {
        RuleFor(x => x)
            .Must(x => x.Name != null || x.Email != null || x.Address != null || x.BankingDetails != null)
            .WithMessage("At least one field must be provided for update.");

        RuleFor(x => x.Name).MaximumLength(100).When(x => x.Name is not null);
        RuleFor(x => x.Email).EmailAddress().MaximumLength(200).When(x => x.Email is not null);
    }
}
```

- [ ] **Step 6: Run tests to confirm they pass (green)**

```bash
dotnet test tests/ClientMicroservice.UnitTests/ --filter "FullyQualifiedName~UpdateClientCommandHandlerTests" -v
```
Expected: 2 tests pass.

- [ ] **Step 7: Commit**

```bash
git add \
  tests/ClientMicroservice.UnitTests/Clients/Commands/UpdateClientCommandHandlerTests.cs \
  src/ClientMicroservice.Application/Clients/Commands/UpdateClient/
git commit -m "feat: add UpdateClient command with handler and validator (TDD)"
```

---

### Task 9: Application — UpdateClientProfilePicture (TDD)

**Files:**
- Create: `tests/ClientMicroservice.UnitTests/Clients/Commands/UpdateClientProfilePictureCommandHandlerTests.cs`
- Create: `src/ClientMicroservice.Application/Clients/Commands/UpdateClientProfilePicture/UpdateClientProfilePictureCommand.cs`
- Create: `src/ClientMicroservice.Application/Clients/Commands/UpdateClientProfilePicture/UpdateClientProfilePictureCommandHandler.cs`

- [ ] **Step 1: Write the failing tests**

`tests/ClientMicroservice.UnitTests/Clients/Commands/UpdateClientProfilePictureCommandHandlerTests.cs`:
```csharp
using ClientMicroservice.Application.Clients.Commands.UpdateClientProfilePicture;
using ClientMicroservice.Application.Common;
using ClientMicroservice.Application.Common.Interfaces;
using ClientMicroservice.Domain.Abstractions;
using ClientMicroservice.Domain.Common;
using ClientMicroservice.Domain.Entities;
using ClientMicroservice.Domain.Errors;
using ClientMicroservice.Domain.ValueObjects;
using Moq;

namespace ClientMicroservice.UnitTests.Clients.Commands;

public sealed class UpdateClientProfilePictureCommandHandlerTests
{
    private readonly Mock<IClientRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IStorageService> _storageMock = new();
    private readonly Mock<ICacheService> _cacheMock = new();
    private readonly UpdateClientProfilePictureCommandHandler _handler;

    private static readonly Address _address = new("123 Main St", "Springfield", "IL", "62701", "US");
    private static readonly BankingDetails _banking = new("0001", "12345-6");

    public UpdateClientProfilePictureCommandHandlerTests()
    {
        _handler = new UpdateClientProfilePictureCommandHandler(
            _repoMock.Object, _uowMock.Object, _storageMock.Object, _cacheMock.Object);
    }

    [Fact]
    public async Task Handle_WhenClientExists_UploadsAndSavesUrl()
    {
        var clientId = Guid.NewGuid();
        var client = Client.Create("Alice", "alice@example.com", _address, _banking);
        var fileData = new FileData(new MemoryStream([1, 2, 3]), "photo.jpg", "image/jpeg");
        const string blobUrl = "https://storage.blob.core.windows.net/pics/photo.jpg";

        _repoMock.Setup(r => r.GetByIdAsync(clientId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(client);
        _storageMock.Setup(s => s.UploadAsync(
                fileData.Content, fileData.FileName, fileData.ContentType, It.IsAny<CancellationToken>()))
            .ReturnsAsync(blobUrl);

        var command = new UpdateClientProfilePictureCommand(clientId, fileData);
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(blobUrl, client.ProfilePictureUrl);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(c => c.RemoveAsync($"client:{clientId}", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenClientNotFound_ReturnsNotFoundError()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Client?)null);

        var command = new UpdateClientProfilePictureCommand(
            Guid.NewGuid(),
            new FileData(Stream.Null, "photo.jpg", "image/jpeg"));
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ClientErrors.NotFound.Code, result.Error.Code);
        _storageMock.Verify(s => s.UploadAsync(
            It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
```

- [ ] **Step 2: Run tests to confirm compilation error (red)**

```bash
dotnet test tests/ClientMicroservice.UnitTests/ --filter "FullyQualifiedName~UpdateClientProfilePictureCommandHandlerTests" 2>&1 | head -20
```
Expected: Build error — types not found.

- [ ] **Step 3: Create UpdateClientProfilePictureCommand**

`src/ClientMicroservice.Application/Clients/Commands/UpdateClientProfilePicture/UpdateClientProfilePictureCommand.cs`:
```csharp
using MediatR;
using ClientMicroservice.Application.Common;
using ClientMicroservice.Domain.Common;
using Unit = ClientMicroservice.Domain.Common.Unit;

namespace ClientMicroservice.Application.Clients.Commands.UpdateClientProfilePicture;

public sealed record UpdateClientProfilePictureCommand(
    Guid ClientId,
    FileData File
) : IRequest<Result<Unit>>;
```

- [ ] **Step 4: Create UpdateClientProfilePictureCommandHandler**

`src/ClientMicroservice.Application/Clients/Commands/UpdateClientProfilePicture/UpdateClientProfilePictureCommandHandler.cs`:
```csharp
using MediatR;
using ClientMicroservice.Application.Common.Interfaces;
using ClientMicroservice.Domain.Abstractions;
using ClientMicroservice.Domain.Common;
using ClientMicroservice.Domain.Errors;
using Unit = ClientMicroservice.Domain.Common.Unit;

namespace ClientMicroservice.Application.Clients.Commands.UpdateClientProfilePicture;

public sealed class UpdateClientProfilePictureCommandHandler(
    IClientRepository repository,
    IUnitOfWork unitOfWork,
    IStorageService storageService,
    ICacheService cacheService)
    : IRequestHandler<UpdateClientProfilePictureCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(UpdateClientProfilePictureCommand command, CancellationToken ct)
    {
        var client = await repository.GetByIdAsync(command.ClientId, ct);
        if (client is null)
            return ClientErrors.NotFound;

        var url = await storageService.UploadAsync(
            command.File.Content, command.File.FileName, command.File.ContentType, ct);

        client.SetProfilePicture(url);
        repository.Update(client);
        await unitOfWork.SaveChangesAsync(ct);

        await cacheService.RemoveAsync($"client:{command.ClientId}", ct);

        return Unit.Value;
    }
}
```

- [ ] **Step 5: Run tests to confirm they pass (green)**

```bash
dotnet test tests/ClientMicroservice.UnitTests/ --filter "FullyQualifiedName~UpdateClientProfilePictureCommandHandlerTests" -v
```
Expected: 2 tests pass.

- [ ] **Step 6: Commit**

```bash
git add \
  tests/ClientMicroservice.UnitTests/Clients/Commands/UpdateClientProfilePictureCommandHandlerTests.cs \
  src/ClientMicroservice.Application/Clients/Commands/UpdateClientProfilePicture/
git commit -m "feat: add UpdateClientProfilePicture command with handler (TDD)"
```

---

### Task 10: Application — DeleteClient (TDD)

**Files:**
- Create: `tests/ClientMicroservice.UnitTests/Clients/Commands/DeleteClientCommandHandlerTests.cs`
- Create: `src/ClientMicroservice.Application/Clients/Commands/DeleteClient/DeleteClientCommand.cs`
- Create: `src/ClientMicroservice.Application/Clients/Commands/DeleteClient/DeleteClientCommandHandler.cs`

- [ ] **Step 1: Write the failing tests**

`tests/ClientMicroservice.UnitTests/Clients/Commands/DeleteClientCommandHandlerTests.cs`:
```csharp
using ClientMicroservice.Application.Clients.Commands.DeleteClient;
using ClientMicroservice.Domain.Abstractions;
using ClientMicroservice.Domain.Common;
using ClientMicroservice.Domain.Entities;
using ClientMicroservice.Domain.Errors;
using ClientMicroservice.Domain.Events;
using ClientMicroservice.Domain.ValueObjects;
using Moq;

namespace ClientMicroservice.UnitTests.Clients.Commands;

public sealed class DeleteClientCommandHandlerTests
{
    private readonly Mock<IClientRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IEventBus> _eventBusMock = new();
    private readonly DeleteClientCommandHandler _handler;

    private static readonly Address _address = new("123 Main St", "Springfield", "IL", "62701", "US");
    private static readonly BankingDetails _banking = new("0001", "12345-6");

    public DeleteClientCommandHandlerTests()
    {
        _handler = new DeleteClientCommandHandler(
            _repoMock.Object, _uowMock.Object, _eventBusMock.Object);
    }

    [Fact]
    public async Task Handle_WhenClientExists_DeletesAndPublishesEvent()
    {
        var clientId = Guid.NewGuid();
        var client = Client.Create("Bob", "bob@example.com", _address, _banking);

        _repoMock.Setup(r => r.GetByIdAsync(clientId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(client);

        var result = await _handler.Handle(new DeleteClientCommand(clientId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _repoMock.Verify(r => r.Delete(client), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _eventBusMock.Verify(e => e.PublishAsync(
            It.IsAny<ClientDeletedDomainEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenClientNotFound_ReturnsNotFoundError()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Client?)null);

        var result = await _handler.Handle(
            new DeleteClientCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ClientErrors.NotFound.Code, result.Error.Code);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
```

- [ ] **Step 2: Run tests to confirm compilation error (red)**

```bash
dotnet test tests/ClientMicroservice.UnitTests/ --filter "FullyQualifiedName~DeleteClientCommandHandlerTests" 2>&1 | head -20
```
Expected: Build error — types not found.

- [ ] **Step 3: Create DeleteClientCommand**

`src/ClientMicroservice.Application/Clients/Commands/DeleteClient/DeleteClientCommand.cs`:
```csharp
using MediatR;
using ClientMicroservice.Domain.Common;
using Unit = ClientMicroservice.Domain.Common.Unit;

namespace ClientMicroservice.Application.Clients.Commands.DeleteClient;

public sealed record DeleteClientCommand(Guid Id) : IRequest<Result<Unit>>;
```

- [ ] **Step 4: Create DeleteClientCommandHandler**

`src/ClientMicroservice.Application/Clients/Commands/DeleteClient/DeleteClientCommandHandler.cs`:
```csharp
using MediatR;
using ClientMicroservice.Domain.Abstractions;
using ClientMicroservice.Domain.Common;
using ClientMicroservice.Domain.Errors;
using ClientMicroservice.Domain.Events;
using Unit = ClientMicroservice.Domain.Common.Unit;

namespace ClientMicroservice.Application.Clients.Commands.DeleteClient;

public sealed class DeleteClientCommandHandler(
    IClientRepository repository,
    IUnitOfWork unitOfWork,
    IEventBus eventBus)
    : IRequestHandler<DeleteClientCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(DeleteClientCommand command, CancellationToken ct)
    {
        var client = await repository.GetByIdAsync(command.Id, ct);
        if (client is null)
            return ClientErrors.NotFound;

        repository.Delete(client);
        await unitOfWork.SaveChangesAsync(ct);

        await eventBus.PublishAsync(new ClientDeletedDomainEvent(client.Id), ct);

        return Unit.Value;
    }
}
```

- [ ] **Step 5: Run tests to confirm they pass (green)**

```bash
dotnet test tests/ClientMicroservice.UnitTests/ --filter "FullyQualifiedName~DeleteClientCommandHandlerTests" -v
```
Expected: 2 tests pass.

- [ ] **Step 6: Commit**

```bash
git add \
  tests/ClientMicroservice.UnitTests/Clients/Commands/DeleteClientCommandHandlerTests.cs \
  src/ClientMicroservice.Application/Clients/Commands/DeleteClient/
git commit -m "feat: add DeleteClient command with handler (TDD)"
```

---

### Task 11: Application — GetClientById with Cache (TDD)

**Files:**
- Create: `tests/ClientMicroservice.UnitTests/Clients/Queries/GetClientByIdQueryHandlerTests.cs`
- Create: `src/ClientMicroservice.Application/Clients/Queries/GetClientById/GetClientByIdQuery.cs`
- Create: `src/ClientMicroservice.Application/Clients/Queries/GetClientById/GetClientByIdQueryHandler.cs`

- [ ] **Step 1: Write the failing tests**

`tests/ClientMicroservice.UnitTests/Clients/Queries/GetClientByIdQueryHandlerTests.cs`:
```csharp
using AutoMapper;
using ClientMicroservice.Application.Clients.Mappings;
using ClientMicroservice.Application.Clients.Queries.GetClientById;
using ClientMicroservice.Application.Common.DTOs;
using ClientMicroservice.Application.Common.Interfaces;
using ClientMicroservice.Domain.Abstractions;
using ClientMicroservice.Domain.Entities;
using ClientMicroservice.Domain.Errors;
using ClientMicroservice.Domain.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ClientMicroservice.UnitTests.Clients.Queries;

public sealed class GetClientByIdQueryHandlerTests
{
    private readonly Mock<IClientRepository> _repoMock = new();
    private readonly Mock<ICacheService> _cacheMock = new();
    private readonly IMapper _mapper;
    private readonly GetClientByIdQueryHandler _handler;

    private static readonly Address _address = new("123 Main St", "Springfield", "IL", "62701", "US");
    private static readonly BankingDetails _banking = new("0001", "12345-6");

    public GetClientByIdQueryHandlerTests()
    {
        _mapper = new MapperConfiguration(
            cfg => cfg.AddProfile<ClientMappingProfile>(),
            NullLoggerFactory.Instance)
            .CreateMapper();
        _handler = new GetClientByIdQueryHandler(_repoMock.Object, _mapper, _cacheMock.Object);
    }

    [Fact]
    public async Task Handle_WhenCacheHit_ReturnsCachedDtoWithoutQueryingDb()
    {
        var clientId = Guid.NewGuid();
        var cachedDto = new ClientDto(
            clientId, "Alice", "alice@example.com",
            new AddressDto("123 Main St", "Springfield", "IL", "62701", "US"),
            null,
            new BankingDetailsDto("0001", "12345-6"),
            DateTimeOffset.UtcNow);

        _cacheMock.Setup(c => c.GetAsync<ClientDto>($"client:{clientId}", It.IsAny<CancellationToken>()))
                  .ReturnsAsync(cachedDto);

        var result = await _handler.Handle(new GetClientByIdQuery(clientId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(cachedDto, result.Value);
        _repoMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenCacheMissAndClientExists_QueriesDbAndCachesResult()
    {
        var clientId = Guid.NewGuid();
        var client = Client.Create("Alice", "alice@example.com", _address, _banking);

        _cacheMock.Setup(c => c.GetAsync<ClientDto>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync((ClientDto?)null);
        _repoMock.Setup(r => r.GetByIdAsync(clientId, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(client);

        var result = await _handler.Handle(new GetClientByIdQuery(clientId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Alice", result.Value.Name);
        _cacheMock.Verify(c => c.SetAsync(
            $"client:{clientId}",
            It.IsAny<ClientDto>(),
            TimeSpan.FromMinutes(10),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCacheMissAndClientNotFound_ReturnsNotFoundError()
    {
        _cacheMock.Setup(c => c.GetAsync<ClientDto>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync((ClientDto?)null);
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync((Client?)null);

        var result = await _handler.Handle(
            new GetClientByIdQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ClientErrors.NotFound.Code, result.Error.Code);
    }
}
```

- [ ] **Step 2: Run tests to confirm compilation error (red)**

```bash
dotnet test tests/ClientMicroservice.UnitTests/ --filter "FullyQualifiedName~GetClientByIdQueryHandlerTests" 2>&1 | head -20
```
Expected: Build error — types not found.

- [ ] **Step 3: Create GetClientByIdQuery**

`src/ClientMicroservice.Application/Clients/Queries/GetClientById/GetClientByIdQuery.cs`:
```csharp
using MediatR;
using ClientMicroservice.Application.Common.DTOs;
using ClientMicroservice.Domain.Common;

namespace ClientMicroservice.Application.Clients.Queries.GetClientById;

public sealed record GetClientByIdQuery(Guid Id) : IRequest<Result<ClientDto>>;
```

- [ ] **Step 4: Create GetClientByIdQueryHandler**

`src/ClientMicroservice.Application/Clients/Queries/GetClientById/GetClientByIdQueryHandler.cs`:
```csharp
using AutoMapper;
using MediatR;
using ClientMicroservice.Application.Common.DTOs;
using ClientMicroservice.Application.Common.Interfaces;
using ClientMicroservice.Domain.Abstractions;
using ClientMicroservice.Domain.Common;
using ClientMicroservice.Domain.Errors;

namespace ClientMicroservice.Application.Clients.Queries.GetClientById;

public sealed class GetClientByIdQueryHandler(
    IClientRepository repository,
    IMapper mapper,
    ICacheService cacheService)
    : IRequestHandler<GetClientByIdQuery, Result<ClientDto>>
{
    public async Task<Result<ClientDto>> Handle(GetClientByIdQuery query, CancellationToken ct)
    {
        var cacheKey = $"client:{query.Id}";

        var cached = await cacheService.GetAsync<ClientDto>(cacheKey, ct);
        if (cached is not null)
            return cached;

        var client = await repository.GetByIdAsync(query.Id, ct);
        if (client is null)
            return ClientErrors.NotFound;

        var dto = mapper.Map<ClientDto>(client);
        await cacheService.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(10), ct);
        return dto;
    }
}
```

- [ ] **Step 5: Run tests to confirm they pass (green)**

```bash
dotnet test tests/ClientMicroservice.UnitTests/ --filter "FullyQualifiedName~GetClientByIdQueryHandlerTests" -v
```
Expected: 3 tests pass.

- [ ] **Step 6: Commit**

```bash
git add \
  tests/ClientMicroservice.UnitTests/Clients/Queries/GetClientByIdQueryHandlerTests.cs \
  src/ClientMicroservice.Application/Clients/Queries/GetClientById/
git commit -m "feat: add GetClientById query with Redis cache-aside (TDD)"
```

---

### Task 12: Application — GetClients

**Files:**
- Create: `src/ClientMicroservice.Application/Clients/Queries/GetClients/GetClientsQuery.cs`
- Create: `src/ClientMicroservice.Application/Clients/Queries/GetClients/GetClientsQueryHandler.cs`

- [ ] **Step 1: Create GetClientsQuery**

`src/ClientMicroservice.Application/Clients/Queries/GetClients/GetClientsQuery.cs`:
```csharp
using MediatR;
using ClientMicroservice.Application.Common.DTOs;
using ClientMicroservice.Domain.Common;

namespace ClientMicroservice.Application.Clients.Queries.GetClients;

public sealed record GetClientsQuery(int PageNumber = 1, int PageSize = 20)
    : IRequest<Result<PagedList<ClientDto>>>;
```

- [ ] **Step 2: Create GetClientsQueryHandler**

`src/ClientMicroservice.Application/Clients/Queries/GetClients/GetClientsQueryHandler.cs`:
```csharp
using AutoMapper;
using MediatR;
using ClientMicroservice.Application.Common.DTOs;
using ClientMicroservice.Domain.Abstractions;
using ClientMicroservice.Domain.Common;

namespace ClientMicroservice.Application.Clients.Queries.GetClients;

public sealed class GetClientsQueryHandler(IClientRepository repository, IMapper mapper)
    : IRequestHandler<GetClientsQuery, Result<PagedList<ClientDto>>>
{
    public async Task<Result<PagedList<ClientDto>>> Handle(GetClientsQuery query, CancellationToken ct)
    {
        var paged = await repository.GetPagedAsync(query.PageNumber, query.PageSize, ct);
        var dtos = mapper.Map<List<ClientDto>>(paged.Items);
        return new PagedList<ClientDto>(dtos, paged.PageNumber, paged.PageSize, paged.TotalCount);
    }
}
```

- [ ] **Step 3: Build Application and run all unit tests**

```bash
dotnet build src/ClientMicroservice.Application/ClientMicroservice.Application.csproj && \
dotnet test tests/ClientMicroservice.UnitTests/
```
Expected: Build succeeded. All tests pass.

- [ ] **Step 4: Commit**

```bash
git add src/ClientMicroservice.Application/Clients/Queries/GetClients/
git commit -m "feat: add GetClients query handler"
```

---

### Task 13: Infrastructure — NuGet Packages

**Files:**
- Modify: `src/ClientMicroservice.Infrastructure/ClientMicroservice.Infrastructure.csproj`

- [ ] **Step 1: Add Redis and Azure Blob Storage packages**

```bash
dotnet add src/ClientMicroservice.Infrastructure/ClientMicroservice.Infrastructure.csproj \
  package Microsoft.Extensions.Caching.StackExchangeRedis && \
dotnet add src/ClientMicroservice.Infrastructure/ClientMicroservice.Infrastructure.csproj \
  package Azure.Storage.Blobs
```

- [ ] **Step 2: Build to verify packages resolve**

```bash
dotnet build src/ClientMicroservice.Infrastructure/ClientMicroservice.Infrastructure.csproj
```
Expected: `Build succeeded.`

- [ ] **Step 3: Commit**

```bash
git add src/ClientMicroservice.Infrastructure/ClientMicroservice.Infrastructure.csproj
git commit -m "chore: add Redis and Azure.Storage.Blobs NuGet packages"
```

---

### Task 14: Infrastructure — ClientConfiguration, ClientRepository, DbContext

**Files:**
- Create: `src/ClientMicroservice.Infrastructure/Persistence/Configurations/ClientConfiguration.cs`
- Create: `src/ClientMicroservice.Infrastructure/Persistence/Repositories/ClientRepository.cs`
- Modify: `src/ClientMicroservice.Infrastructure/Persistence/ApplicationDbContext.cs`

- [ ] **Step 1: Create ClientConfiguration**

`src/ClientMicroservice.Infrastructure/Persistence/Configurations/ClientConfiguration.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ClientMicroservice.Domain.Entities;

namespace ClientMicroservice.Infrastructure.Persistence.Configurations;

internal sealed class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.ToTable("clients");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id");
        builder.Property(c => c.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(c => c.Email).HasColumnName("email").HasMaxLength(200).IsRequired();
        builder.HasIndex(c => c.Email).IsUnique();
        builder.Property(c => c.ProfilePictureUrl).HasColumnName("profile_picture_url");
        builder.Property(c => c.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.OwnsOne(c => c.Address, a =>
        {
            a.Property(x => x.Street).HasColumnName("address_street").HasMaxLength(200).IsRequired();
            a.Property(x => x.City).HasColumnName("address_city").HasMaxLength(100).IsRequired();
            a.Property(x => x.State).HasColumnName("address_state").HasMaxLength(100).IsRequired();
            a.Property(x => x.ZipCode).HasColumnName("address_zip_code").HasMaxLength(20).IsRequired();
            a.Property(x => x.Country).HasColumnName("address_country").HasMaxLength(100).IsRequired();
        });

        builder.OwnsOne(c => c.BankingDetails, b =>
        {
            b.Property(x => x.Agency).HasColumnName("banking_agency").HasMaxLength(50).IsRequired();
            b.Property(x => x.AccountNumber).HasColumnName("banking_account_number").HasMaxLength(50).IsRequired();
        });
    }
}
```

- [ ] **Step 2: Create ClientRepository**

`src/ClientMicroservice.Infrastructure/Persistence/Repositories/ClientRepository.cs`:
```csharp
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
```

- [ ] **Step 3: Update ApplicationDbContext**

Replace `src/ClientMicroservice.Infrastructure/Persistence/ApplicationDbContext.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using ClientMicroservice.Domain.Entities;

namespace ClientMicroservice.Infrastructure.Persistence;

internal sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options)
{
    public DbSet<Client> Clients => Set<Client>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
```

- [ ] **Step 4: Build Infrastructure**

```bash
dotnet build src/ClientMicroservice.Infrastructure/ClientMicroservice.Infrastructure.csproj
```
Expected: `Build succeeded.`

- [ ] **Step 5: Commit**

```bash
git add \
  src/ClientMicroservice.Infrastructure/Persistence/Configurations/ClientConfiguration.cs \
  src/ClientMicroservice.Infrastructure/Persistence/Repositories/ClientRepository.cs \
  src/ClientMicroservice.Infrastructure/Persistence/ApplicationDbContext.cs
git commit -m "feat: add ClientConfiguration, ClientRepository, update DbContext"
```

---

### Task 15: Infrastructure — RedisCacheService

**Files:**
- Create: `src/ClientMicroservice.Infrastructure/Caching/RedisCacheService.cs`

- [ ] **Step 1: Create RedisCacheService**

`src/ClientMicroservice.Infrastructure/Caching/RedisCacheService.cs`:
```csharp
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using ClientMicroservice.Application.Common.Interfaces;

namespace ClientMicroservice.Infrastructure.Caching;

internal sealed class RedisCacheService(IDistributedCache cache) : ICacheService
{
    public async Task<T?> GetAsync<T>(string key, CancellationToken ct)
    {
        var data = await cache.GetAsync(key, ct);
        return data is null ? default : JsonSerializer.Deserialize<T>(data);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct)
    {
        var data = JsonSerializer.SerializeToUtf8Bytes(value);
        await cache.SetAsync(key, data,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl }, ct);
    }

    public Task RemoveAsync(string key, CancellationToken ct)
        => cache.RemoveAsync(key, ct);
}
```

- [ ] **Step 2: Build to verify**

```bash
dotnet build src/ClientMicroservice.Infrastructure/ClientMicroservice.Infrastructure.csproj
```
Expected: `Build succeeded.`

- [ ] **Step 3: Commit**

```bash
git add src/ClientMicroservice.Infrastructure/Caching/RedisCacheService.cs
git commit -m "feat: add RedisCacheService implementing ICacheService"
```

---

### Task 16: Infrastructure — AzureBlobStorageService

**Files:**
- Create: `src/ClientMicroservice.Infrastructure/Storage/AzureBlobStorageService.cs`

- [ ] **Step 1: Create AzureBlobStorageService**

`src/ClientMicroservice.Infrastructure/Storage/AzureBlobStorageService.cs`:
```csharp
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ClientMicroservice.Application.Common.Interfaces;

namespace ClientMicroservice.Infrastructure.Storage;

internal sealed class AzureBlobStorageService(BlobServiceClient blobServiceClient, string containerName)
    : IStorageService
{
    public async Task<string> UploadAsync(Stream content, string fileName, string contentType, CancellationToken ct)
    {
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        var blobName = $"{Guid.NewGuid()}-{fileName}";
        var blobClient = containerClient.GetBlobClient(blobName);

        await blobClient.UploadAsync(content,
            new BlobHttpHeaders { ContentType = contentType },
            cancellationToken: ct);

        return blobClient.Uri.ToString();
    }
}
```

- [ ] **Step 2: Build to verify**

```bash
dotnet build src/ClientMicroservice.Infrastructure/ClientMicroservice.Infrastructure.csproj
```
Expected: `Build succeeded.`

- [ ] **Step 3: Commit**

```bash
git add src/ClientMicroservice.Infrastructure/Storage/AzureBlobStorageService.cs
git commit -m "feat: add AzureBlobStorageService implementing IStorageService"
```

---

### Task 17: Infrastructure — EventBus + Consumer + DI

**Files:**
- Modify: `src/ClientMicroservice.Infrastructure/Messaging/MassTransitEventBus.cs`
- Create: `src/ClientMicroservice.Infrastructure/Messaging/Consumers/ClientCreatedEventConsumer.cs`
- Modify: `src/ClientMicroservice.Infrastructure/DependencyInjection.cs`

- [ ] **Step 1: Replace MassTransitEventBus**

`src/ClientMicroservice.Infrastructure/Messaging/MassTransitEventBus.cs`:
```csharp
using MassTransit;
using ClientMicroservice.Contracts.Clients;
using ClientMicroservice.Domain.Abstractions;
using ClientMicroservice.Domain.Events;

namespace ClientMicroservice.Infrastructure.Messaging;

internal sealed class MassTransitEventBus(IPublishEndpoint endpoint) : IEventBus
{
    public Task PublishAsync<T>(T message, CancellationToken ct = default) where T : class, IDomainEvent
        => message switch
        {
            ClientCreatedDomainEvent e => endpoint.Publish(
                new ClientCreatedEvent(e.ClientId, e.Name, e.Email, e.CreatedAt), ct),
            ClientUpdatedDomainEvent e => endpoint.Publish(
                new ClientUpdatedEvent(e.ClientId), ct),
            ClientDeletedDomainEvent e => endpoint.Publish(
                new ClientDeletedEvent(e.ClientId), ct),
            _ => endpoint.Publish(message, ct)
        };
}
```

- [ ] **Step 2: Create ClientCreatedEventConsumer**

`src/ClientMicroservice.Infrastructure/Messaging/Consumers/ClientCreatedEventConsumer.cs`:
```csharp
using MassTransit;
using Microsoft.Extensions.Logging;
using ClientMicroservice.Contracts.Clients;

namespace ClientMicroservice.Infrastructure.Messaging.Consumers;

public sealed class ClientCreatedEventConsumer(ILogger<ClientCreatedEventConsumer> logger)
    : IConsumer<ClientCreatedEvent>
{
    public Task Consume(ConsumeContext<ClientCreatedEvent> context)
    {
        logger.LogInformation(
            "Received ClientCreatedEvent for ClientId={ClientId}, Name={Name}",
            context.Message.ClientId,
            context.Message.Name);

        return Task.CompletedTask;
    }
}
```

- [ ] **Step 3: Replace DependencyInjection.cs**

`src/ClientMicroservice.Infrastructure/DependencyInjection.cs`:
```csharp
using Azure.Storage.Blobs;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ClientMicroservice.Application.Common.Interfaces;
using ClientMicroservice.Domain.Abstractions;
using ClientMicroservice.Infrastructure.Caching;
using ClientMicroservice.Infrastructure.Messaging;
using ClientMicroservice.Infrastructure.Messaging.Consumers;
using ClientMicroservice.Infrastructure.Persistence;
using ClientMicroservice.Infrastructure.Persistence.Repositories;
using ClientMicroservice.Infrastructure.Storage;

namespace ClientMicroservice.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(opts =>
            opts.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IClientRepository, ClientRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IEventBus, MassTransitEventBus>();

        services.AddStackExchangeRedisCache(opts =>
            opts.Configuration = configuration["Redis:ConnectionString"]
                ?? throw new InvalidOperationException("Redis:ConnectionString is required"));
        services.AddScoped<ICacheService, RedisCacheService>();

        var blobConnectionString = configuration["Azure:BlobStorage:ConnectionString"]
            ?? throw new InvalidOperationException("Azure:BlobStorage:ConnectionString is required");
        var containerName = configuration["Azure:BlobStorage:ContainerName"]
            ?? throw new InvalidOperationException("Azure:BlobStorage:ContainerName is required");
        services.AddSingleton(new BlobServiceClient(blobConnectionString));
        services.AddScoped<IStorageService>(sp =>
            new AzureBlobStorageService(sp.GetRequiredService<BlobServiceClient>(), containerName));

        services.AddMassTransit(bus =>
        {
            bus.AddConsumer<ClientCreatedEventConsumer>();

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

- [ ] **Step 4: Build Infrastructure**

```bash
dotnet build src/ClientMicroservice.Infrastructure/ClientMicroservice.Infrastructure.csproj
```
Expected: `Build succeeded.`

- [ ] **Step 5: Commit**

```bash
git add \
  src/ClientMicroservice.Infrastructure/Messaging/MassTransitEventBus.cs \
  src/ClientMicroservice.Infrastructure/Messaging/Consumers/ClientCreatedEventConsumer.cs \
  src/ClientMicroservice.Infrastructure/DependencyInjection.cs
git commit -m "feat: update event bus, add consumer, wire Redis and Azure Blob in DI"
```

---

### Task 18: API — ClientsController

**Files:**
- Create: `src/ClientMicroservice.API/Controllers/ClientsController.cs`

- [ ] **Step 1: Create ClientsController**

`src/ClientMicroservice.API/Controllers/ClientsController.cs`:
```csharp
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ClientMicroservice.API.Extensions;
using ClientMicroservice.Application.Clients.Commands.CreateClient;
using ClientMicroservice.Application.Clients.Commands.DeleteClient;
using ClientMicroservice.Application.Clients.Commands.UpdateClient;
using ClientMicroservice.Application.Clients.Commands.UpdateClientProfilePicture;
using ClientMicroservice.Application.Clients.Queries.GetClientById;
using ClientMicroservice.Application.Clients.Queries.GetClients;
using ClientMicroservice.Application.Common;
using ClientMicroservice.Domain.ValueObjects;

namespace ClientMicroservice.API.Controllers;

[ApiController]
[Authorize]
[Route("clients")]
public sealed class ClientsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetClients(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetClientsQuery(pageNumber, pageSize), ct);
        return this.ToOkResult(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetClientById(Guid id, CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetClientByIdQuery(id), ct);
        return this.ToOkResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateClient(
        [FromBody] CreateClientCommand command,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(command, ct);
        return this.ToCreatedResult(
            result,
            actionName: nameof(GetClientById),
            routeValues: new { id = result.IsSuccess ? result.Value : Guid.Empty });
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> UpdateClient(
        Guid id,
        [FromBody] UpdateClientRequest request,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(
            new UpdateClientCommand(id, request.Name, request.Email, request.Address, request.BankingDetails), ct);
        return this.ToNoContentResult(result);
    }

    [HttpPatch("{id:guid}/profile-picture")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UpdateProfilePicture(
        Guid id,
        IFormFile profilePicture,
        CancellationToken ct = default)
    {
        var command = new UpdateClientProfilePictureCommand(
            id,
            new FileData(profilePicture.OpenReadStream(), profilePicture.FileName, profilePicture.ContentType));
        var result = await mediator.Send(command, ct);
        return this.ToNoContentResult(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteClient(Guid id, CancellationToken ct = default)
    {
        var result = await mediator.Send(new DeleteClientCommand(id), ct);
        return this.ToNoContentResult(result);
    }
}

public sealed record UpdateClientRequest(
    string? Name,
    string? Email,
    Address? Address,
    BankingDetails? BankingDetails);
```

- [ ] **Step 2: Build full solution**

```bash
dotnet build ClientMicroservice.slnx
```
Expected: `Build succeeded, 0 Error(s).`

- [ ] **Step 3: Commit**

```bash
git add src/ClientMicroservice.API/Controllers/ClientsController.cs
git commit -m "feat: add ClientsController with 6 endpoints including PATCH profile-picture"
```

---

### Task 19: Configuration — appsettings

**Files:**
- Modify: `src/ClientMicroservice.API/appsettings.json`
- Modify: `src/ClientMicroservice.API/appsettings.Development.json`

- [ ] **Step 1: Update appsettings.json**

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
  "Redis": {
    "ConnectionString": "redis:6379"
  },
  "Azure": {
    "BlobStorage": {
      "ConnectionString": "REPLACE_ME_WITH_AZURE_STORAGE_CONNECTION_STRING",
      "ContainerName": "profile-pictures"
    }
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

- [ ] **Step 2: Update appsettings.Development.json**

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
  "Redis": {
    "ConnectionString": "localhost:6379"
  },
  "Azure": {
    "BlobStorage": {
      "ConnectionString": "UseDevelopmentStorage=true",
      "ContainerName": "profile-pictures"
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information"
    }
  }
}
```

- [ ] **Step 3: Commit**

```bash
git add \
  src/ClientMicroservice.API/appsettings.json \
  src/ClientMicroservice.API/appsettings.Development.json
git commit -m "config: add Redis and Azure Blob Storage configuration keys"
```

---

### Task 20: EF Core Migration

- [ ] **Step 1: Confirm Migrations folder is absent (removed in Task 1)**

```bash
ls src/ClientMicroservice.Infrastructure/Persistence/Migrations/ 2>&1
```
Expected: No such file or directory.

- [ ] **Step 2: Add AddClientTable migration**

```bash
dotnet ef migrations add AddClientTable \
  --project src/ClientMicroservice.Infrastructure/ClientMicroservice.Infrastructure.csproj \
  --startup-project src/ClientMicroservice.API/ClientMicroservice.API.csproj \
  --output-dir Persistence/Migrations
```
Expected: Migration created with a `clients` table.

- [ ] **Step 3: Spot-check the generated migration**

```bash
grep -E "CreateTable|HasColumnName|profile_picture_url|banking_agency|address_street|clients" \
  src/ClientMicroservice.Infrastructure/Persistence/Migrations/*_AddClientTable.cs
```
Expected: Lines referencing `clients` table, `address_street`, `banking_agency`, `profile_picture_url`.

- [ ] **Step 4: Commit**

```bash
git add src/ClientMicroservice.Infrastructure/Persistence/Migrations/
git commit -m "feat: add AddClientTable EF Core migration"
```

---

### Task 21: Final Build and Test Verification

- [ ] **Step 1: Build full solution**

```bash
dotnet build ClientMicroservice.slnx
```
Expected: `Build succeeded, 0 Error(s).`

- [ ] **Step 2: Run all unit tests**

```bash
dotnet test tests/ClientMicroservice.UnitTests/ -v
```
Expected: 11+ tests pass (CreateClient ×2, UpdateClient ×2, UpdateClientProfilePicture ×2, DeleteClient ×2, GetClientById ×3).

- [ ] **Step 3: Push to remote**

```bash
git push origin main
```
