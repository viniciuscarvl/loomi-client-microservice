using MediatR;
using ClientMicroservice.Application.Common.DTOs;
using ClientMicroservice.Domain.Common;

namespace ClientMicroservice.Application.Clients.Queries.GetClients;

public sealed record GetClientsQuery(int PageNumber = 1, int PageSize = 20)
    : IRequest<Result<PagedList<ClientDto>>>;
