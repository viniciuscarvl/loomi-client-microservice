using MediatR;
using ClientMicroservice.Domain.Common;
using Unit = ClientMicroservice.Domain.Common.Unit;

namespace ClientMicroservice.Application.Users.Commands.DeleteUser;

public sealed record DeleteUserCommand(Guid Id) : IRequest<Result<Unit>>;
