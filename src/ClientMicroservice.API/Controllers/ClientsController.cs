using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ClientMicroservice.API.Extensions;
using ClientMicroservice.Application.Clients.Commands.CreateClient;
using ClientMicroservice.Application.Clients.Commands.DeleteClient;
using ClientMicroservice.Application.Clients.Commands.UpdateClient;
using ClientMicroservice.Application.Clients.Commands.UpdateClientProfilePicture;
using ClientMicroservice.Application.Clients.Queries.GetClientById;
using ClientMicroservice.Application.Clients.Queries.GetClients;
using ClientMicroservice.Application.Common;
using ClientMicroservice.Domain.ValueObjects;

namespace ClientMicroservice.API.Controllers;

[ApiController]
[Authorize]
[Route("clients")]
public sealed class ClientsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetClients(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetClientsQuery(pageNumber, pageSize), ct);
        return this.ToOkResult(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetClientById(Guid id, CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetClientByIdQuery(id), ct);
        return this.ToOkResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateClient(
        [FromBody] CreateClientCommand command,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(command, ct);
        return this.ToCreatedResult(
            result,
            actionName: nameof(GetClientById),
            routeValues: new { id = result.IsSuccess ? result.Value : Guid.Empty });
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> UpdateClient(
        Guid id,
        [FromBody] UpdateClientRequest request,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(
            new UpdateClientCommand(id, request.Name, request.Email, request.Address, request.BankingDetails), ct);
        return this.ToNoContentResult(result);
    }

    [HttpPatch("{id:guid}/profile-picture")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UpdateProfilePicture(
        Guid id,
        IFormFile profilePicture,
        CancellationToken ct = default)
    {
        var command = new UpdateClientProfilePictureCommand(
            id,
            new FileData(profilePicture.OpenReadStream(), profilePicture.FileName, profilePicture.ContentType));
        var result = await mediator.Send(command, ct);
        return this.ToNoContentResult(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteClient(Guid id, CancellationToken ct = default)
    {
        var result = await mediator.Send(new DeleteClientCommand(id), ct);
        return this.ToNoContentResult(result);
    }
}

public sealed record UpdateClientRequest(
    string? Name,
    string? Email,
    Address? Address,
    BankingDetails? BankingDetails);
