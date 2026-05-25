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
