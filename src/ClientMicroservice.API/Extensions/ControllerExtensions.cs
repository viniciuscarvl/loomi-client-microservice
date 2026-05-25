using Microsoft.AspNetCore.Mvc;
using ClientMicroservice.Domain.Common;

namespace ClientMicroservice.API.Extensions;

public static class ControllerExtensions
{
    private const string NotFoundCode = "NotFound";

    public static IActionResult ToOkResult<T>(this ControllerBase controller, Result<T> result)
        => result.IsSuccess
            ? controller.Ok(result.Value)
            : ToErrorResult(controller, result.Error);

    public static IActionResult ToCreatedResult<T>(
        this ControllerBase controller,
        Result<T> result,
        string actionName,
        object? routeValues = null)
        => result.IsSuccess
            ? controller.CreatedAtAction(actionName, routeValues, result.Value)
            : ToErrorResult(controller, result.Error);

    public static IActionResult ToNoContentResult<T>(
        this ControllerBase controller, Result<T> result)
        => result.IsSuccess
            ? controller.NoContent()
            : ToErrorResult(controller, result.Error);

    private static IActionResult ToErrorResult(ControllerBase controller, Error error)
    {
        if (error.Code.EndsWith(NotFoundCode, StringComparison.OrdinalIgnoreCase) ||
            error.Code.Contains(".NotFound", StringComparison.OrdinalIgnoreCase))
        {
            return controller.NotFound(new ProblemDetails
            {
                Title = "Not Found",
                Detail = error.Message,
                Status = StatusCodes.Status404NotFound
            });
        }

        return controller.BadRequest(new ProblemDetails
        {
            Title = "Bad Request",
            Detail = error.Message,
            Status = StatusCodes.Status400BadRequest
        });
    }
}
