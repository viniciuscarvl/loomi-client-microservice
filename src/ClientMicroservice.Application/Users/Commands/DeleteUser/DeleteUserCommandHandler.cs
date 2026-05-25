using MediatR;
using ClientMicroservice.Domain.Abstractions;
using ClientMicroservice.Domain.Common;
using ClientMicroservice.Domain.Errors;
using ClientMicroservice.Domain.Events;
using Unit = ClientMicroservice.Domain.Common.Unit;

namespace ClientMicroservice.Application.Users.Commands.DeleteUser;

public sealed class DeleteUserCommandHandler(
    IUserRepository repository,
    IUnitOfWork unitOfWork,
    IEventBus eventBus)
    : IRequestHandler<DeleteUserCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(DeleteUserCommand command, CancellationToken ct)
    {
        var user = await repository.GetByIdAsync(command.Id, ct);
        if (user is null)
            return UserErrors.NotFound;

        repository.Delete(user);
        await unitOfWork.SaveChangesAsync(ct);

        await eventBus.PublishAsync(new UserDeletedDomainEvent(user.Id), ct);

        return Unit.Value;
    }
}
