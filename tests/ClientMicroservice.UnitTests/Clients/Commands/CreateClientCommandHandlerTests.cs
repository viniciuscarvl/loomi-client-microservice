using ClientMicroservice.Application.Clients.Commands.CreateClient;
using ClientMicroservice.Domain.Abstractions;
using ClientMicroservice.Domain.Common;
using ClientMicroservice.Domain.Entities;
using ClientMicroservice.Domain.Errors;
using ClientMicroservice.Domain.Events;
using ClientMicroservice.Domain.ValueObjects;
using Moq;

namespace ClientMicroservice.UnitTests.Clients.Commands;

public sealed class CreateClientCommandHandlerTests
{
    private readonly Mock<IClientRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IEventBus> _eventBusMock = new();
    private readonly CreateClientCommandHandler _handler;

    private static readonly Address _address = new("123 Main St", "Springfield", "IL", "62701", "US");
    private static readonly BankingDetails _banking = new("0001", "12345-6");

    public CreateClientCommandHandlerTests()
    {
        _handler = new CreateClientCommandHandler(
            _repoMock.Object, _uowMock.Object, _eventBusMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidData_CreatesClientAndReturnsId()
    {
        _repoMock.Setup(r => r.GetByEmailAsync("john@example.com", It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Client?)null);

        var command = new CreateClientCommand("John Doe", "john@example.com", _address, _banking);
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<Client>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _eventBusMock.Verify(e => e.PublishAsync(
            It.IsAny<ClientCreatedDomainEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenEmailAlreadyTaken_ReturnsEmailTakenError()
    {
        var existing = Client.Create("Existing", "john@example.com", _address, _banking);
        _repoMock.Setup(r => r.GetByEmailAsync("john@example.com", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(existing);

        var command = new CreateClientCommand("John Doe", "john@example.com", _address, _banking);
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ClientErrors.EmailTaken.Code, result.Error.Code);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<Client>(), It.IsAny<CancellationToken>()), Times.Never);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
