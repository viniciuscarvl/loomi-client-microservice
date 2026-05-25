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
