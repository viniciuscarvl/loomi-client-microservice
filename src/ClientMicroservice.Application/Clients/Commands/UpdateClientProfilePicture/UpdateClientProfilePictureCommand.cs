using MediatR;
using ClientMicroservice.Application.Common;
using ClientMicroservice.Domain.Common;
using Unit = ClientMicroservice.Domain.Common.Unit;

namespace ClientMicroservice.Application.Clients.Commands.UpdateClientProfilePicture;

public sealed record UpdateClientProfilePictureCommand(
    Guid ClientId,
    FileData File
) : IRequest<Result<Unit>>;
