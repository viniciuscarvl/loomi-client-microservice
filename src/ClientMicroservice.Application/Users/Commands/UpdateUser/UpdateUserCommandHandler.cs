using MediatR;
using ClientMicroservice.Domain.Abstractions;
using ClientMicroservice.Domain.Common;
using ClientMicroservice.Domain.Errors;
using ClientMicroservice.Domain.Events;
using Unit = ClientMicroservice.Domain.Common.Unit;

namespace ClientMicroservice.Application.Users.Commands.UpdateUser;

public sealed class UpdateUserCommandHandler(
    IUserRepository repository,
    IUnitOfWork unitOfWork,
    IEventBus eventBus)
    : IRequestHandler<UpdateUserCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(UpdateUserCommand command, CancellationToken ct)
    {
        var user = await repository.GetByIdAsync(command.Id, ct);
        if (user is null)
            return UserErrors.NotFound;

        user.Update(command.Name, command.Email);
        repository.Update(user);
        await unitOfWork.SaveChangesAsync(ct);

        await eventBus.PublishAsync(
            new UserUpdatedDomainEvent(user.Id, user.Name, user.Email), ct);

        return Unit.Value;
    }
}
