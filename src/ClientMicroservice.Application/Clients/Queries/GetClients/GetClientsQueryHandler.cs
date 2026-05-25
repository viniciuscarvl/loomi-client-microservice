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
