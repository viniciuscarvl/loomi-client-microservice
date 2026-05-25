using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ClientMicroservice.API.Extensions;
using ClientMicroservice.Application.Users.Commands.CreateUser;
using ClientMicroservice.Application.Users.Commands.DeleteUser;
using ClientMicroservice.Application.Users.Commands.UpdateUser;
using ClientMicroservice.Application.Users.Queries.GetUserById;
using ClientMicroservice.Application.Users.Queries.GetUsers;

namespace ClientMicroservice.API.Controllers;

[ApiController]
[Authorize]
[Route("users")]
public sealed class UsersController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetUsersQuery(pageNumber, pageSize), ct);
        return this.ToOkResult(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetUserById(Guid id, CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetUserByIdQuery(id), ct);
        return this.ToOkResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser(
        [FromBody] CreateUserCommand command,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(command, ct);
        return this.ToCreatedResult(
            result,
            actionName: nameof(GetUserById),
            routeValues: new { id = result.IsSuccess ? result.Value : Guid.Empty });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateUser(
        Guid id,
        [FromBody] UpdateUserRequest request,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(
            new UpdateUserCommand(id, request.Name, request.Email), ct);
        return this.ToNoContentResult(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteUser(Guid id, CancellationToken ct = default)
    {
        var result = await mediator.Send(new DeleteUserCommand(id), ct);
        return this.ToNoContentResult(result);
    }
}

public sealed record UpdateUserRequest(string Name, string Email);
