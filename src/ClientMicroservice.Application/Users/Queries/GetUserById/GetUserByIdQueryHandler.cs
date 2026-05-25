using AutoMapper;
using MediatR;
using ClientMicroservice.Application.Common.DTOs;
using ClientMicroservice.Domain.Abstractions;
using ClientMicroservice.Domain.Common;
using ClientMicroservice.Domain.Errors;

namespace ClientMicroservice.Application.Users.Queries.GetUserById;

public sealed class GetUserByIdQueryHandler(IUserRepository repository, IMapper mapper)
    : IRequestHandler<GetUserByIdQuery, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(GetUserByIdQuery query, CancellationToken ct)
    {
        var user = await repository.GetByIdAsync(query.Id, ct);
        if (user is null)
            return UserErrors.NotFound;

        return mapper.Map<UserDto>(user);
    }
}
