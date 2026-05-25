using ClientMicroservice.Application.Users.Commands.CreateUser;

namespace ClientMicroservice.UnitTests.Users.Validators;

public sealed class CreateUserCommandValidatorTests
{
    private readonly CreateUserCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidData_Passes()
    {
        var result = _validator.Validate(new CreateUserCommand("Jane", "jane@example.com"));
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("", "jane@example.com")]
    [InlineData(null, "jane@example.com")]
    public void Validate_WithEmptyName_Fails(string? name, string email)
    {
        var result = _validator.Validate(new CreateUserCommand(name!, email));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateUserCommand.Name));
    }

    [Theory]
    [InlineData("Jane", "")]
    [InlineData("Jane", "not-an-email")]
    [InlineData("Jane", null)]
    public void Validate_WithInvalidEmail_Fails(string name, string? email)
    {
        var result = _validator.Validate(new CreateUserCommand(name, email!));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateUserCommand.Email));
    }

    [Fact]
    public void Validate_WithNameExceedingMaxLength_Fails()
    {
        var result = _validator.Validate(new CreateUserCommand(new string('A', 101), "a@b.com"));
        Assert.False(result.IsValid);
    }
}
