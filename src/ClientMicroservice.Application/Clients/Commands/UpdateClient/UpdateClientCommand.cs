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
