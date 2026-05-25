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
