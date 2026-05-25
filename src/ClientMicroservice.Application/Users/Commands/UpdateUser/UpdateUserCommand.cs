using MediatR;
using ClientMicroservice.Domain.Common;
using Unit = ClientMicroservice.Domain.Common.Unit;

namespace ClientMicroservice.Application.Users.Commands.UpdateUser;

public sealed record UpdateUserCommand(Guid Id, string Name, string Email) : IRequest<Result<Unit>>;
