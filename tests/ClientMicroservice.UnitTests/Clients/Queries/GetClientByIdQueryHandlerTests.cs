using AutoMapper;
using ClientMicroservice.Application.Clients.Mappings;
using ClientMicroservice.Application.Clients.Queries.GetClientById;
using ClientMicroservice.Application.Common.DTOs;
using ClientMicroservice.Application.Common.Interfaces;
using ClientMicroservice.Domain.Abstractions;
using ClientMicroservice.Domain.Entities;
using ClientMicroservice.Domain.Errors;
using ClientMicroservice.Domain.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ClientMicroservice.UnitTests.Clients.Queries;

public sealed class GetClientByIdQueryHandlerTests
{
    private readonly Mock<IClientRepository> _repoMock = new();
    private readonly Mock<ICacheService> _cacheMock = new();
    private readonly IMapper _mapper;
    private readonly GetClientByIdQueryHandler _handler;

    private readonly Address _address = new("123 Main St", "Springfield", "IL", "62701", "US");
    private readonly BankingDetails _banking = new("0001", "12345-6");

    public GetClientByIdQueryHandlerTests()
    {
        _mapper = new MapperConfiguration(
            cfg => cfg.AddProfile<ClientMappingProfile>(),
            NullLoggerFactory.Instance)
            .CreateMapper();
        _handler = new GetClientByIdQueryHandler(_repoMock.Object, _mapper, _cacheMock.Object);
    }

    [Fact]
    public async Task Handle_WhenCacheHit_ReturnsCachedDtoWithoutQueryingDb()
    {
        var clientId = Guid.NewGuid();
        var cachedDto = new ClientDto(
            clientId, "Alice", "alice@example.com",
            new AddressDto("123 Main St", "Springfield", "IL", "62701", "US"),
            null,
            new BankingDetailsDto("0001", "12345-6"),
            DateTimeOffset.UtcNow);

        _cacheMock.Setup(c => c.GetAsync<ClientDto>($"client:{clientId}", It.IsAny<CancellationToken>()))
                  .ReturnsAsync(cachedDto);

        var result = await _handler.Handle(new GetClientByIdQuery(clientId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(cachedDto, result.Value);
        _repoMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenCacheMissAndClientExists_QueriesDbAndCachesResult()
    {
        var clientId = Guid.NewGuid();
        var client = Client.Create("Alice", "alice@example.com", _address, _banking);

        _cacheMock.Setup(c => c.GetAsync<ClientDto>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync((ClientDto?)null);
        _repoMock.Setup(r => r.GetByIdAsync(clientId, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(client);

        var result = await _handler.Handle(new GetClientByIdQuery(clientId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Alice", result.Value.Name);
        _cacheMock.Verify(c => c.SetAsync(
            $"client:{clientId}",
            It.IsAny<ClientDto>(),
            TimeSpan.FromMinutes(10),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCacheMissAndClientNotFound_ReturnsNotFoundError()
    {
        _cacheMock.Setup(c => c.GetAsync<ClientDto>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync((ClientDto?)null);
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync((Client?)null);

        var result = await _handler.Handle(
            new GetClientByIdQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ClientErrors.NotFound.Code, result.Error.Code);
    }
}
