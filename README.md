# ClientMicroservice

Template de microserviço .NET 10 com Clean Architecture, CQRS, EF Core, MassTransit e testes de integração com Testcontainers.

---

## Tecnologias

| Categoria | Biblioteca | Versão |
|---|---|---|
| Runtime | .NET | 10 |
| CQRS / Mediator | MediatR | 14.1 |
| Validação | FluentValidation | 12.1 |
| Mapeamento | AutoMapper | 16.1 |
| ORM | Entity Framework Core + Npgsql | 10.0 |
| Mensageria | MassTransit + RabbitMQ | 9.1 |
| Autenticação | Microsoft.AspNetCore.Authentication.JwtBearer | 10.0 |
| Documentação API | Scalar.AspNetCore | 2.x |
| Observabilidade | OpenTelemetry (OTLP) | 1.x |
| Testes unitários | xUnit + Moq | 2.x / 4.x |
| Testes de integração | xUnit + Testcontainers.PostgreSql + Respawn | 2.x / 3.x / 6.x |

---

## Arquitetura

O projeto segue **Clean Architecture** com cinco camadas, com regras estritas de dependência:

```
Domain ← Application ← Infrastructure
                      ← Contracts (independente)
                      ← API (raiz de composição)
```

### Camadas

| Projeto | Referencia | Responsabilidade |
|---|---|---|
| `Domain` | nada | Entidades, `Result<T>`, abstrações, eventos de domínio |
| `Application` | Domain | Handlers CQRS, validadores, DTOs, behaviors de pipeline |
| `Infrastructure` | Application, Domain, Contracts | EF Core, MassTransit, implementações de repositório |
| `Contracts` | nada | Eventos de integração (publicável como pacote NuGet) |
| `API` | Application, Infrastructure, Contracts | Controllers, middleware, composição de DI |

### CQRS

Cada operação vive na sua própria pasta em `Application/Users/Commands/<Nome>/` ou `Application/Users/Queries/<Nome>/`, contendo o record (command/query), seu handler e opcionalmente o validador FluentValidation.

**Endpoints disponíveis:**

| Método | Rota | Operação |
|---|---|---|
| `POST` | `/users` | Criar usuário |
| `GET` | `/users/{id}` | Buscar usuário por ID |
| `GET` | `/users` | Listar usuários (paginado) |
| `PUT` | `/users/{id}` | Atualizar usuário |
| `DELETE` | `/users/{id}` | Remover usuário |

### Pipeline de behaviors (MediatR)

Registrados em ordem em `Application/DependencyInjection.cs`:

1. **`LoggingBehavior`** — registra nome da request e tempo de execução
2. **`ValidationBehavior`** — executa todos os `IValidator<TRequest>`; lança `AppValidationException` em caso de falha (nunca retorna `Result.Failure`)

`AppValidationException` é capturada pelo `GlobalExceptionHandler` e mapeada para HTTP 422 `ValidationProblemDetails`.

### Result Monad

Handlers retornam `Result<T>` (readonly struct). Use os operadores implícitos:

```csharp
return UserErrors.NotFound;   // Error → Result<T>
return user.Id;               // T → Result<T>
```

### Eventos de domínio vs. integração

- **Eventos de domínio** (`UserCreatedDomainEvent`) são publicados pelos handlers via `IEventBus` (definido no Domain).
- `MassTransitEventBus` na Infrastructure traduz para **eventos de integração** do Contracts (`UserCreatedEvent`) antes de publicar no RabbitMQ.

---

## Estrutura de pastas

```
src/
  ClientMicroservice.Domain/
    Abstractions/          # IRepository<T>, IUnitOfWork, IEventBus, IUserRepository
    Common/                # Result<T>, PagedList<T>, Unit
    Entities/              # User
    Errors/                # UserErrors
    Events/                # UserCreatedDomainEvent, etc.
  ClientMicroservice.Application/
    Common/
      Behaviors/           # LoggingBehavior, ValidationBehavior
      Exceptions/          # AppValidationException
    Users/
      Commands/            # CreateUser, UpdateUser, DeleteUser
      Queries/             # GetUserById, GetUsers
      Mappings/            # AutoMapper profile
  ClientMicroservice.Infrastructure/
    Messaging/             # MassTransitEventBus
    Persistence/
      Migrations/          # EF Core migrations
      Repositories/        # Repository<T>, UserRepository, UnitOfWork
      Configurations/      # IEntityTypeConfiguration<User>
      ApplicationDbContext.cs
  ClientMicroservice.Contracts/
    Users/                 # UserCreatedEvent, UserUpdatedEvent, UserDeletedEvent
  ClientMicroservice.API/
    Controllers/           # UsersController
    Extensions/            # ControllerExtensions (Result<T> → IActionResult)
    Middleware/            # GlobalExceptionHandler

tests/
  ClientMicroservice.UnitTests/       # Handlers com Moq (14 testes)
  ClientMicroservice.IntegrationTests/ # Repositórios com Testcontainers (9 testes)
```

---

## Executar localmente

**Pré-requisitos:** Docker, .NET 10 SDK

```bash
# Subir dependências (Postgres + RabbitMQ)
docker-compose up -d postgres rabbitmq

# Aplicar migrations
dotnet ef database update \
  --project src/ClientMicroservice.Infrastructure \
  --startup-project src/ClientMicroservice.API

# Rodar a API
dotnet run --project src/ClientMicroservice.API
```

API disponível em `http://localhost:5000`. Documentação interativa em `http://localhost:5000/scalar`.

**Ou rodar tudo com Docker:**

```bash
docker-compose up
```

---

## Testes

```bash
# Testes unitários (sem dependências externas)
dotnet test tests/ClientMicroservice.UnitTests/

# Testes de integração (requer Docker)
dotnet test tests/ClientMicroservice.IntegrationTests/
```

Os testes de integração sobem um container `postgres:17-alpine` via Testcontainers, aplicam as migrations e usam Respawn para resetar os dados entre cada teste.

---

## Configuração

Todas as chaves são obrigatórias — valores ausentes lançam `InvalidOperationException` na inicialização.

| Chave | Descrição |
|---|---|
| `ConnectionStrings:DefaultConnection` | Connection string Npgsql para o Postgres |
| `RabbitMq:Host` / `Username` / `Password` | Transporte MassTransit |
| `Jwt:Secret` / `Issuer` / `Audience` | JWT Bearer (Secret mínimo 32 chars) |
| `OpenTelemetry:Endpoint` | Endpoint OTLP (ex: `http://localhost:4317`) |

Para desenvolvimento local, os valores padrão estão em `appsettings.Development.json`. O `Jwt:Secret` deve ser substituído por um valor seguro em produção.

---

## Adicionar uma nova entidade

1. **Domain**: classe da entidade, `I<Entidade>Repository : IRepository<T>`, eventos de domínio, classe de erros estática
2. **Application**: commands/queries, DTOs, perfil AutoMapper, validadores
3. **Infrastructure**: `IEntityTypeConfiguration<T>`, `DbSet` no `ApplicationDbContext`, classe de repositório, registrar em `DependencyInjection.cs`, adicionar migration
4. **Contracts**: eventos de integração
5. **API**: controller, estender switch em `MassTransitEventBus`
6. **Tests**: testes unitários mockando `IRepository<T>`, `IUnitOfWork`, `IEventBus` via Moq
