using Foundry.Shared.Kernel.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Foundry.Storage.Api.Extensions;

public static class ResultExtensions
{
    public static IActionResult ToActionResult(this Result result)
    {
        if (result.IsSuccess)
        {
            return new OkResult();
        }

        return ToErrorResult(result.Error);
    }

    public static IActionResult ToActionResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
        {
            return new OkObjectResult(result.Value);
        }

        return ToErrorResult(result.Error);
    }

    public static IActionResult ToCreatedResult<T>(this Result<T> result, string location)
    {
        if (result.IsSuccess)
        {
            return new CreatedResult(location, result.Value);
        }

        return ToErrorResult(result.Error);
    }

    private static ObjectResult ToErrorResult(Error error)
    {
        int statusCode = error.Code switch
        {
            _ when error.Code.EndsWith(".NotFound", StringComparison.Ordinal) => StatusCodes.Status404NotFound,
            _ when error.Code.StartsWith("Validation", StringComparison.Ordinal) => StatusCodes.Status400BadRequest,
            _ when error.Code.StartsWith("Unauthorized", StringComparison.Ordinal) => StatusCodes.Status401Unauthorized,
            _ when error.Code.StartsWith("Forbidden", StringComparison.Ordinal) => StatusCodes.Status403Forbidden,
            _ when error.Code.StartsWith("Conflict", StringComparison.Ordinal) => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status422UnprocessableEntity
        };

        ProblemDetails problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Detail = error.Message,
            Extensions = { ["code"] = error.Code }
        };

        return new ObjectResult(problemDetails) { StatusCode = statusCode };
    }
}
