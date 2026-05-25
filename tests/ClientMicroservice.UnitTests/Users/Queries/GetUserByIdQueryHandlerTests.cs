using AutoMapper;
using ClientMicroservice.Application.Common.DTOs;
using ClientMicroservice.Application.Users.Mappings;
using ClientMicroservice.Application.Users.Queries.GetUserById;
using ClientMicroservice.Domain.Abstractions;
using ClientMicroservice.Domain.Entities;
using ClientMicroservice.Domain.Errors;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ClientMicroservice.UnitTests.Users.Queries;

public sealed class GetUserByIdQueryHandlerTests
{
    private readonly Mock<IUserRepository> _repoMock = new();
    private readonly IMapper _mapper;
    private readonly GetUserByIdQueryHandler _handler;

    public GetUserByIdQueryHandlerTests()
    {
        _mapper = new MapperConfiguration(
            cfg => cfg.AddProfile<UserMappingProfile>(),
            NullLoggerFactory.Instance)
            .CreateMapper();
        _handler = new GetUserByIdQueryHandler(_repoMock.Object, _mapper);
    }

    [Fact]
    public async Task Handle_WhenUserExists_ReturnsUserDto()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("Alice", "alice@example.com");
        _repoMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(user);

        var result = await _handler.Handle(new GetUserByIdQuery(userId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(user.Id, result.Value.Id);
        Assert.Equal("Alice", result.Value.Name);
        Assert.Equal("alice@example.com", result.Value.Email);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsNotFoundError()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((User?)null);

        var result = await _handler.Handle(
            new GetUserByIdQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(UserErrors.NotFound.Code, result.Error.Code);
    }
}
