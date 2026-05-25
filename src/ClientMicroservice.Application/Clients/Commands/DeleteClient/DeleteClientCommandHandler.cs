using MediatR;
using ClientMicroservice.Domain.Abstractions;
using ClientMicroservice.Domain.Common;
using ClientMicroservice.Domain.Errors;
using ClientMicroservice.Domain.Events;
using Unit = ClientMicroservice.Domain.Common.Unit;

namespace ClientMicroservice.Application.Clients.Commands.DeleteClient;

public sealed class DeleteClientCommandHandler(
    IClientRepository repository,
    IUnitOfWork unitOfWork,
    IEventBus eventBus)
    : IRequestHandler<DeleteClientCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(DeleteClientCommand command, CancellationToken ct)
    {
        var client = await repository.GetByIdAsync(command.Id, ct);
        if (client is null)
            return ClientErrors.NotFound;

        repository.Delete(client);
        await unitOfWork.SaveChangesAsync(ct);

        await eventBus.PublishAsync(new ClientDeletedDomainEvent(client.Id), ct);

        return Unit.Value;
    }
}
