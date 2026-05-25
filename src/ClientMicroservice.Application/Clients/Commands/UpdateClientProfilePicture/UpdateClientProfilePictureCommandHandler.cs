using MediatR;
using ClientMicroservice.Application.Common.Interfaces;
using ClientMicroservice.Domain.Abstractions;
using ClientMicroservice.Domain.Common;
using ClientMicroservice.Domain.Errors;
using Unit = ClientMicroservice.Domain.Common.Unit;

namespace ClientMicroservice.Application.Clients.Commands.UpdateClientProfilePicture;

public sealed class UpdateClientProfilePictureCommandHandler(
    IClientRepository repository,
    IUnitOfWork unitOfWork,
    IStorageService storageService,
    ICacheService cacheService)
    : IRequestHandler<UpdateClientProfilePictureCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(UpdateClientProfilePictureCommand command, CancellationToken ct)
    {
        var client = await repository.GetByIdAsync(command.ClientId, ct);
        if (client is null)
            return ClientErrors.NotFound;

        var url = await storageService.UploadAsync(
            command.File.Content, command.File.FileName, command.File.ContentType, ct);

        client.SetProfilePicture(url);
        repository.Update(client);
        await unitOfWork.SaveChangesAsync(ct);

        await cacheService.RemoveAsync($"client:{command.ClientId}", ct);

        return Unit.Value;
    }
}
