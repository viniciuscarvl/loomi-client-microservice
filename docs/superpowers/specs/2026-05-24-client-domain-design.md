# Spec: Client Domain + Redis Cache + Azure Blob Storage

**Date:** 2026-05-24  
**Status:** Approved

---

## Overview

Adapt the microservice domain from `User` to `Client`, introducing two new value objects (`Address`, `BankingDetails`), Redis caching for the GET-by-id endpoint, and Azure Blob Storage for profile picture uploads. All endpoints require JWT Bearer authentication.

---

## Domain Layer

### Entity: `Client`

Sealed class with private constructor and factory method pattern.

| Property | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK |
| `Name` | `string` | |
| `Email` | `string` | unique |
| `Address` | `Address` | owned value object |
| `ProfilePictureUrl` | `string?` | URL returned by Azure Blob Storage after upload |
| `BankingDetails` | `BankingDetails` | owned value object |
| `CreatedAt` | `DateTimeOffset` | set on creation |

**Methods:**
- `static Create(name, email, address, bankingDetails) → Client`
- `Update(name?, email?, address?, bankingDetails?)` — updates only non-null fields
- `SetProfilePicture(url)` — updates `ProfilePictureUrl`

### Value Object: `Address`

```csharp
public record Address(string Street, string City, string State, string ZipCode, string Country);
```

Stored as owned entity in EF Core with column prefix `address_`.

### Value Object: `BankingDetails`

```csharp
public record BankingDetails(string Agency, string AccountNumber);
```

Stored as owned entity in EF Core with column prefix `banking_`.

### Errors: `ClientErrors`

- `ClientErrors.NotFound` — `"Client.NotFound"` / `"Client was not found."`
- `ClientErrors.EmailTaken` — `"Client.EmailTaken"` / `"Email address is already in use."`

### Domain Events

- `ClientCreatedDomainEvent(ClientId)`
- `ClientUpdatedDomainEvent(ClientId)`
- `ClientDeletedDomainEvent(ClientId)`

### Repository Abstraction

```csharp
public interface IClientRepository : IRepository<Client>
{
    Task<Client?> GetByEmailAsync(string email, CancellationToken ct);
}
```

---

## Application Layer

### Interfaces (defined in Application, implemented in Infrastructure)

```csharp
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct);
    Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct);
    Task RemoveAsync(string key, CancellationToken ct);
}

public interface IStorageService
{
    Task<string> UploadAsync(Stream content, string fileName, string contentType, CancellationToken ct);
}
```

### DTO: `ClientDto`

```csharp
public record ClientDto(
    Guid Id,
    string Name,
    string Email,
    AddressDto Address,
    string? ProfilePictureUrl,
    BankingDetailsDto BankingDetails,
    DateTimeOffset CreatedAt
);

public record AddressDto(string Street, string City, string State, string ZipCode, string Country);
public record BankingDetailsDto(string Agency, string AccountNumber);
```

### Commands & Queries

| Name | Input | Output | Notes |
|---|---|---|---|
| `CreateClientCommand` | `name, email, address, bankingDetails` | `Result<Guid>` | validates email uniqueness, publishes domain event |
| `UpdateClientCommand` | `id, name?, email?, address?, bankingDetails?` | `Result<Unit>` | updates only non-null fields, invalidates Redis cache |
| `UpdateClientProfilePictureCommand` | `id, IFormFile profilePicture` | `Result<Unit>` | uploads to Azure Blob, saves URL, invalidates Redis cache |
| `DeleteClientCommand` | `id` | `Result<Unit>` | publishes domain event |
| `GetClientByIdQuery` | `id` | `Result<ClientDto>` | checks Redis first, falls back to DB, caches for 10 min |
| `GetClientsQuery` | `pageNumber, pageSize` | `Result<PagedList<ClientDto>>` | no caching (paginated list changes frequently) |

### Cache Strategy (GetClientByIdQueryHandler)

```
key: "client:{id}"
TTL: 10 minutes

GET flow:
  1. Try Redis → if hit, return deserialized ClientDto
  2. Miss → query DB → if not found, return ClientErrors.NotFound
  3. Store result in Redis with TTL
  4. Return ClientDto

PATCH/DELETE flow:
  1. ICacheService.RemoveAsync("client:{id}") before or after DB update
```

### AutoMapper Profile: `ClientMappingProfile`

Maps `Client → ClientDto`, `Address → AddressDto`, `BankingDetails → BankingDetailsDto`.

### Validators

- `CreateClientCommandValidator`: Name required (max 100), Email required valid format (max 200), Address fields required, BankingDetails fields required
- `UpdateClientCommandValidator`: At least one field must be non-null; if Email provided, valid format

---

## Infrastructure Layer

### PostgreSQL (EF Core)

**Table:** `clients`

| Column | Type | Notes |
|---|---|---|
| `id` | uuid | PK |
| `name` | varchar(100) | not null |
| `email` | varchar(200) | not null, unique index |
| `address_street` | varchar(200) | not null |
| `address_city` | varchar(100) | not null |
| `address_state` | varchar(100) | not null |
| `address_zip_code` | varchar(20) | not null |
| `address_country` | varchar(100) | not null |
| `profile_picture_url` | text | nullable |
| `banking_agency` | varchar(50) | not null |
| `banking_account_number` | varchar(50) | not null |
| `created_at` | timestamptz | not null |

`ClientConfiguration` uses `OwnsOne` for `Address` and `BankingDetails`.  
Migration name: `AddClientTable`.

### Redis

- Package: `StackExchange.Redis` + `Microsoft.Extensions.Caching.StackExchangeRedis`
- `RedisCacheService` implements `ICacheService`
- Serialization: `System.Text.Json`
- Config key: `Redis:ConnectionString`
- Registered as scoped `ICacheService`

### Azure Blob Storage

- Package: `Azure.Storage.Blobs`
- `AzureBlobStorageService` implements `IStorageService`
- Uploads stream to configured container, returns blob public URL
- Config keys: `Azure:BlobStorage:ConnectionString`, `Azure:BlobStorage:ContainerName`
- Registered as scoped `IStorageService`

### Event Bus

Extend `MassTransitEventBus` switch to map:
- `ClientCreatedDomainEvent → ClientCreatedEvent`
- `ClientUpdatedDomainEvent → ClientUpdatedEvent`
- `ClientDeletedDomainEvent → ClientDeletedEvent`

Contracts: `ClientCreatedEvent`, `ClientUpdatedEvent`, `ClientDeletedEvent` in `ClientMicroservice.Contracts/Clients/`.

---

## API Layer

**Controller:** `ClientsController` — `[Route("clients")]`, `[Authorize]`

| Method | Route | Command/Query | Response |
|---|---|---|---|
| `POST` | `/clients` | `CreateClientCommand` | 201 Created + Location header |
| `GET` | `/clients/{id:guid}` | `GetClientByIdQuery` | 200 OK `ClientDto` |
| `GET` | `/clients` | `GetClientsQuery` | 200 OK `PagedList<ClientDto>` |
| `PATCH` | `/clients/{id:guid}` | `UpdateClientCommand` | 204 No Content |
| `PATCH` | `/clients/{id:guid}/profile-picture` | `UpdateClientProfilePictureCommand` | 204 No Content |
| `DELETE` | `/clients/{id:guid}` | `DeleteClientCommand` | 204 No Content |

Profile picture endpoint uses `[Consumes("multipart/form-data")]` and `IFormFile`.

---

## Configuration Keys

```
# Existing
ConnectionStrings:DefaultConnection    Npgsql connection string (PostgreSQL)
RabbitMq:Host / Username / Password   MassTransit transport
Jwt:Secret / Issuer / Audience        JWT Bearer
OpenTelemetry:Endpoint                OTLP exporter

# New
Redis:ConnectionString                 StackExchange.Redis connection string
Azure:BlobStorage:ConnectionString     Azure Storage account connection string
Azure:BlobStorage:ContainerName        Blob container name for profile pictures
```

---

## What Is Removed / Replaced

- All `User*` files replaced by `Client*` equivalents
- `PUT /users/{id}` (full update) replaced by `PATCH /clients/{id}` (partial update)
- `UsersController` replaced by `ClientsController`

---

## Out of Scope

- Client authentication (this service consumes JWT issued elsewhere)
- Blob container creation (assumed pre-created in Azure)
- Image resizing / validation beyond content-type check
- Pagination caching for `GetClientsQuery`
