using FluentValidation;
using FluentValidation.Results;
using MediatR;
using ClientMicroservice.Application.Common.Behaviors;
using ClientMicroservice.Application.Common.Exceptions;
using ClientMicroservice.Domain.Common;
using Moq;

namespace ClientMicroservice.UnitTests.Common.Behaviors;

public sealed class ValidationBehaviorTests
{
    private sealed record TestRequest(string Value) : IRequest<Result<string>>;

    private sealed class FailingValidator : AbstractValidator<TestRequest>
    {
        public FailingValidator()
        {
            RuleFor(x => x.Value).NotEmpty().WithMessage("Value is required.");
        }
    }

    [Fact]
    public async Task Handle_WhenValidationFails_ThrowsAppValidationException()
    {
        var validators = new List<IValidator<TestRequest>> { new FailingValidator() };
        var behavior = new ValidationBehavior<TestRequest, Result<string>>(validators);
        var next = new Mock<RequestHandlerDelegate<Result<string>>>();

        var act = async () => await behavior.Handle(
            new TestRequest(string.Empty), next.Object, CancellationToken.None);

        await Assert.ThrowsAsync<AppValidationException>(act);
        next.Verify(n => n(), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenValidationPasses_CallsNext()
    {
        var validators = new List<IValidator<TestRequest>> { new FailingValidator() };
        var behavior = new ValidationBehavior<TestRequest, Result<string>>(validators);
        var next = new Mock<RequestHandlerDelegate<Result<string>>>();
        next.Setup(n => n()).ReturnsAsync(Result<string>.Success("ok"));

        var result = await behavior.Handle(
            new TestRequest("valid"), next.Object, CancellationToken.None);

        Assert.True(result.IsSuccess);
        next.Verify(n => n(), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNoValidators_CallsNext()
    {
        var behavior = new ValidationBehavior<TestRequest, Result<string>>(
            Enumerable.Empty<IValidator<TestRequest>>());
        var next = new Mock<RequestHandlerDelegate<Result<string>>>();
        next.Setup(n => n()).ReturnsAsync(Result<string>.Success("ok"));

        var result = await behavior.Handle(
            new TestRequest("any"), next.Object, CancellationToken.None);

        Assert.True(result.IsSuccess);
        next.Verify(n => n(), Times.Once);
    }
}
