using MediatR;
using ClientMicroservice.Domain.Common;

namespace ClientMicroservice.Application.Users.Commands.CreateUser;

public sealed record CreateUserCommand(string Name, string Email) : IRequest<Result<Guid>>;
