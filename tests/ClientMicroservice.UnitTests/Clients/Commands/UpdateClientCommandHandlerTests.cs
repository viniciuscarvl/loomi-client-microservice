using ClientMicroservice.Application.Clients.Commands.UpdateClient;
using ClientMicroservice.Application.Common.Interfaces;
using ClientMicroservice.Domain.Abstractions;
using ClientMicroservice.Domain.Common;
using ClientMicroservice.Domain.Entities;
using ClientMicroservice.Domain.Errors;
using ClientMicroservice.Domain.Events;
using ClientMicroservice.Domain.ValueObjects;
using Moq;

namespace ClientMicroservice.UnitTests.Clients.Commands;

public sealed class UpdateClientCommandHandlerTests
{
    private readonly Mock<IClientRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IEventBus> _eventBusMock = new();
    private readonly Mock<ICacheService> _cacheMock = new();
    private readonly UpdateClientCommandHandler _handler;

    private readonly Address _address = new("123 Main St", "Springfield", "IL", "62701", "US");
    private readonly BankingDetails _banking = new("0001", "12345-6");

    public UpdateClientCommandHandlerTests()
    {
        _handler = new UpdateClientCommandHandler(
            _repoMock.Object, _uowMock.Object, _eventBusMock.Object, _cacheMock.Object);
    }

    [Fact]
    public async Task Handle_WhenClientExists_UpdatesAndInvalidatesCache()
    {
        var clientId = Guid.NewGuid();
        var client = Client.Create("Old Name", "old@example.com", _address, _banking);

        _repoMock.Setup(r => r.GetByIdAsync(clientId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(client);

        var command = new UpdateClientCommand(clientId, "New Name", null, null, null);
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(c => c.RemoveAsync($"client:{clientId}", It.IsAny<CancellationToken>()), Times.Once);
        _eventBusMock.Verify(e => e.PublishAsync(
            It.IsAny<ClientUpdatedDomainEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenClientNotFound_ReturnsNotFoundError()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Client?)null);

        var result = await _handler.Handle(
            new UpdateClientCommand(Guid.NewGuid(), "Name", null, null, null), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ClientErrors.NotFound.Code, result.Error.Code);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
