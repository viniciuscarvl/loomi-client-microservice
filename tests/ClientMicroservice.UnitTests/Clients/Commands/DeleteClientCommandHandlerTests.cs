using ClientMicroservice.Application.Clients.Commands.DeleteClient;
using ClientMicroservice.Domain.Abstractions;
using ClientMicroservice.Domain.Common;
using ClientMicroservice.Domain.Entities;
using ClientMicroservice.Domain.Errors;
using ClientMicroservice.Domain.Events;
using ClientMicroservice.Domain.ValueObjects;
using Moq;

namespace ClientMicroservice.UnitTests.Clients.Commands;

public sealed class DeleteClientCommandHandlerTests
{
    private readonly Mock<IClientRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IEventBus> _eventBusMock = new();
    private readonly DeleteClientCommandHandler _handler;

    private readonly Address _address = new("123 Main St", "Springfield", "IL", "62701", "US");
    private readonly BankingDetails _banking = new("0001", "12345-6");

    public DeleteClientCommandHandlerTests()
    {
        _handler = new DeleteClientCommandHandler(
            _repoMock.Object, _uowMock.Object, _eventBusMock.Object);
    }

    [Fact]
    public async Task Handle_WhenClientExists_DeletesAndPublishesEvent()
    {
        var clientId = Guid.NewGuid();
        var client = Client.Create("Bob", "bob@example.com", _address, _banking);

        _repoMock.Setup(r => r.GetByIdAsync(clientId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(client);

        var result = await _handler.Handle(new DeleteClientCommand(clientId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _repoMock.Verify(r => r.Delete(client), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _eventBusMock.Verify(e => e.PublishAsync(
            It.IsAny<ClientDeletedDomainEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenClientNotFound_ReturnsNotFoundError()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Client?)null);

        var result = await _handler.Handle(
            new DeleteClientCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ClientErrors.NotFound.Code, result.Error.Code);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
