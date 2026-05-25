using ClientMicroservice.Application.Clients.Commands.UpdateClientProfilePicture;
using ClientMicroservice.Application.Common;
using ClientMicroservice.Application.Common.Interfaces;
using ClientMicroservice.Domain.Abstractions;
using ClientMicroservice.Domain.Common;
using ClientMicroservice.Domain.Entities;
using ClientMicroservice.Domain.Errors;
using ClientMicroservice.Domain.ValueObjects;
using Moq;

namespace ClientMicroservice.UnitTests.Clients.Commands;

public sealed class UpdateClientProfilePictureCommandHandlerTests
{
    private readonly Mock<IClientRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IStorageService> _storageMock = new();
    private readonly Mock<ICacheService> _cacheMock = new();
    private readonly UpdateClientProfilePictureCommandHandler _handler;

    private readonly Address _address = new("123 Main St", "Springfield", "IL", "62701", "US");
    private readonly BankingDetails _banking = new("0001", "12345-6");

    public UpdateClientProfilePictureCommandHandlerTests()
    {
        _handler = new UpdateClientProfilePictureCommandHandler(
            _repoMock.Object, _uowMock.Object, _storageMock.Object, _cacheMock.Object);
    }

    [Fact]
    public async Task Handle_WhenClientExists_UploadsAndSavesUrl()
    {
        var clientId = Guid.NewGuid();
        var client = Client.Create("Alice", "alice@example.com", _address, _banking);
        var fileData = new FileData(new MemoryStream([1, 2, 3]), "photo.jpg", "image/jpeg");
        const string blobUrl = "https://storage.blob.core.windows.net/pics/photo.jpg";

        _repoMock.Setup(r => r.GetByIdAsync(clientId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(client);
        _storageMock.Setup(s => s.UploadAsync(
                fileData.Content, fileData.FileName, fileData.ContentType, It.IsAny<CancellationToken>()))
            .ReturnsAsync(blobUrl);

        var command = new UpdateClientProfilePictureCommand(clientId, fileData);
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(blobUrl, client.ProfilePictureUrl);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(c => c.RemoveAsync($"client:{clientId}", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenClientNotFound_ReturnsNotFoundError()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Client?)null);

        var command = new UpdateClientProfilePictureCommand(
            Guid.NewGuid(),
            new FileData(Stream.Null, "photo.jpg", "image/jpeg"));
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ClientErrors.NotFound.Code, result.Error.Code);
        _storageMock.Verify(s => s.UploadAsync(
            It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
