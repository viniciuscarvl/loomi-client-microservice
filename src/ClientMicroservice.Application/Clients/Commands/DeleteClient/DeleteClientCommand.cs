using MediatR;
using ClientMicroservice.Domain.Common;
using Unit = ClientMicroservice.Domain.Common.Unit;

namespace ClientMicroservice.Application.Clients.Commands.DeleteClient;

public sealed record DeleteClientCommand(Guid Id) : IRequest<Result<Unit>>;
