using MediatR;
using ClientMicroservice.Application.Common.DTOs;
using ClientMicroservice.Domain.Common;

namespace ClientMicroservice.Application.Users.Queries.GetUsers;

public sealed record GetUsersQuery(int PageNumber = 1, int PageSize = 20)
    : IRequest<Result<PagedList<UserDto>>>;
