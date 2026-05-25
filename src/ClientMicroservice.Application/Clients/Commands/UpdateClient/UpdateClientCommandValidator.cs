using FluentValidation;

namespace ClientMicroservice.Application.Clients.Commands.UpdateClient;

public sealed class UpdateClientCommandValidator : AbstractValidator<UpdateClientCommand>
{
    public UpdateClientCommandValidator()
    {
        RuleFor(x => x)
            .Must(x => x.Name != null || x.Email != null || x.Address != null || x.BankingDetails != null)
            .WithMessage("At least one field must be provided for update.");

        RuleFor(x => x.Name).MaximumLength(100).When(x => x.Name is not null);
        RuleFor(x => x.Email).EmailAddress().MaximumLength(200).When(x => x.Email is not null);
    }
}
