using MediatR;
using ClientMicroservice.Domain.Abstractions;
using ClientMicroservice.Domain.Common;
using ClientMicroservice.Domain.Entities;
using ClientMicroservice.Domain.Errors;
using ClientMicroservice.Domain.Events;

namespace ClientMicroservice.Application.Clients.Commands.CreateClient;

public sealed class CreateClientCommandHandler(
    IClientRepository repository,
    IUnitOfWork unitOfWork,
    IEventBus eventBus)
    : IRequestHandler<CreateClientCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateClientCommand command, CancellationToken ct)
    {
        var existing = await repository.GetByEmailAsync(command.Email, ct);
        if (existing is not null)
            return ClientErrors.EmailTaken;

        var client = Client.Create(command.Name, command.Email, command.Address, command.BankingDetails);
        await repository.AddAsync(client, ct);
        await unitOfWork.SaveChangesAsync(ct);

        await eventBus.PublishAsync(
            new ClientCreatedDomainEvent(client.Id, client.Name, client.Email, client.CreatedAt), ct);

        return client.Id;
    }
}
