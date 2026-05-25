using MediatR;
using ClientMicroservice.Application.Common.Interfaces;
using ClientMicroservice.Domain.Abstractions;
using ClientMicroservice.Domain.Common;
using ClientMicroservice.Domain.Errors;
using ClientMicroservice.Domain.Events;
using Unit = ClientMicroservice.Domain.Common.Unit;

namespace ClientMicroservice.Application.Clients.Commands.UpdateClient;

public sealed class UpdateClientCommandHandler(
    IClientRepository repository,
    IUnitOfWork unitOfWork,
    IEventBus eventBus,
    ICacheService cacheService)
    : IRequestHandler<UpdateClientCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(UpdateClientCommand command, CancellationToken ct)
    {
        var client = await repository.GetByIdAsync(command.Id, ct);
        if (client is null)
            return ClientErrors.NotFound;

        client.Update(command.Name, command.Email, command.Address, command.BankingDetails);
        repository.Update(client);
        await unitOfWork.SaveChangesAsync(ct);

        await cacheService.RemoveAsync($"client:{command.Id}", ct);
        await eventBus.PublishAsync(new ClientUpdatedDomainEvent(client.Id), ct);

        return Unit.Value;
    }
}
