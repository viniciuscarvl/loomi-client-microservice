using MediatR;
using ClientMicroservice.Application.Common.DTOs;
using ClientMicroservice.Domain.Common;

namespace ClientMicroservice.Application.Clients.Queries.GetClientById;

public sealed record GetClientByIdQuery(Guid Id) : IRequest<Result<ClientDto>>;
