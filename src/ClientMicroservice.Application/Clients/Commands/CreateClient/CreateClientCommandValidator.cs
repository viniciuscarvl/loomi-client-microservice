using FluentValidation;

namespace ClientMicroservice.Application.Clients.Commands.CreateClient;

public sealed class CreateClientCommandValidator : AbstractValidator<CreateClientCommand>
{
    public CreateClientCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.Address).NotNull();
        RuleFor(x => x.Address.Street).NotEmpty().MaximumLength(200).When(x => x.Address is not null);
        RuleFor(x => x.Address.City).NotEmpty().MaximumLength(100).When(x => x.Address is not null);
        RuleFor(x => x.Address.State).NotEmpty().MaximumLength(100).When(x => x.Address is not null);
        RuleFor(x => x.Address.ZipCode).NotEmpty().MaximumLength(20).When(x => x.Address is not null);
        RuleFor(x => x.Address.Country).NotEmpty().MaximumLength(100).When(x => x.Address is not null);
        RuleFor(x => x.BankingDetails).NotNull();
        RuleFor(x => x.BankingDetails.Agency).NotEmpty().MaximumLength(50).When(x => x.BankingDetails is not null);
        RuleFor(x => x.BankingDetails.AccountNumber).NotEmpty().MaximumLength(50).When(x => x.BankingDetails is not null);
    }
}
