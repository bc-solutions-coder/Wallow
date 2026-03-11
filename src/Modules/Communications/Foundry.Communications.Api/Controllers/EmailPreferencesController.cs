using Asp.Versioning;
using Foundry.Communications.Api.Contracts.Email.Requests;
using Foundry.Communications.Api.Contracts.Email.Responses;
using Foundry.Communications.Api.Mappings;
using Foundry.Communications.Application.Channels.Email.Commands.UpdateEmailPreferences;
using Foundry.Communications.Application.Channels.Email.DTOs;
using Foundry.Communications.Application.Channels.Email.Queries.GetEmailPreferences;
using Foundry.Shared.Api.Extensions;
using Foundry.Shared.Kernel.Results;
using Foundry.Shared.Kernel.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Wolverine;

namespace Foundry.Communications.Api.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/email-preferences")]
[Authorize]
[Tags("Email Preferences")]
[Produces("application/json")]
[Consumes("application/json")]
public class EmailPreferencesController(IMessageBus bus, ICurrentUserService currentUserService) : ControllerBase
{
    /// <summary>
    /// Get the current user's email notification preferences.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<EmailPreferenceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetPreferences(CancellationToken cancellationToken)
    {
        Guid? userId = currentUserService.GetCurrentUserId();
        if (userId is null)
        {
            return Problem(statusCode: 401, title: "Unauthorized", detail: "User context is required");
        }

        Result<IReadOnlyList<EmailPreferenceDto>> result = await bus.InvokeAsync<Result<IReadOnlyList<EmailPreferenceDto>>>(
            new GetEmailPreferencesQuery(userId.Value), cancellationToken);

        return result.Map(prefs => (IReadOnlyList<EmailPreferenceResponse>)prefs.Select(ToResponse).ToList())
            .ToActionResult();
    }

    /// <summary>
    /// Update an email notification preference for the current user.
    /// </summary>
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdatePreference(
        [FromBody] UpdateEmailPreferenceRequest request,
        CancellationToken cancellationToken)
    {
        Guid? userId = currentUserService.GetCurrentUserId();
        if (userId is null)
        {
            return Problem(statusCode: 401, title: "Unauthorized", detail: "User context is required");
        }

        Result result = await bus.InvokeAsync<Result>(
            new UpdateEmailPreferencesCommand(userId.Value, request.NotificationType.ToDomain(), request.IsEnabled),
            cancellationToken);

        if (result.IsFailure)
        {
            return result.ToActionResult();
        }

        return NoContent();
    }

    private static EmailPreferenceResponse ToResponse(EmailPreferenceDto dto) => new(
        dto.Id,
        dto.UserId,
        dto.NotificationType.ToApi(),
        dto.IsEnabled,
        dto.CreatedAt,
        dto.UpdatedAt);
}
