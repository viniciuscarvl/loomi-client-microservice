using MediatR;
using ClientMicroservice.Domain.Abstractions;
using ClientMicroservice.Domain.Common;
using ClientMicroservice.Domain.Entities;
using ClientMicroservice.Domain.Errors;
using ClientMicroservice.Domain.Events;

namespace ClientMicroservice.Application.Users.Commands.CreateUser;

public sealed class CreateUserCommandHandler(
    IUserRepository repository,
    IUnitOfWork unitOfWork,
    IEventBus eventBus)
    : IRequestHandler<CreateUserCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateUserCommand command, CancellationToken ct)
    {
        var existing = await repository.GetByEmailAsync(command.Email, ct);
        if (existing is not null)
            return UserErrors.EmailTaken;

        var user = User.Create(command.Name, command.Email);
        await repository.AddAsync(user, ct);
        await unitOfWork.SaveChangesAsync(ct);

        await eventBus.PublishAsync(
            new UserCreatedDomainEvent(user.Id, user.Name, user.Email, user.CreatedAt), ct);

        return user.Id;
    }
}
