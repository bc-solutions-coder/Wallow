using Asp.Versioning;
using Foundry.Communications.Api.Contracts.Push;
using Foundry.Communications.Application.Channels.Push.Commands.RemoveTenantPushConfig;
using Foundry.Communications.Application.Channels.Push.Commands.SetTenantPushEnabled;
using Foundry.Communications.Application.Channels.Push.Commands.UpsertTenantPushConfig;
using Foundry.Communications.Application.Channels.Push.DTOs;
using Foundry.Communications.Application.Channels.Push.Queries.GetTenantPushConfig;
using Foundry.Communications.Domain.Channels.Push.Enums;
using Foundry.Shared.Api.Extensions;
using Foundry.Shared.Kernel.Identity;
using Foundry.Shared.Kernel.Identity.Authorization;
using Foundry.Shared.Kernel.MultiTenancy;
using Foundry.Shared.Kernel.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Wolverine;

namespace Foundry.Communications.Api.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/admin/push/config")]
[Tags("Admin - Push Configuration")]
[Produces("application/json")]
public class PushConfigurationController(IMessageBus bus, ITenantContext tenantContext) : ControllerBase
{
    [HttpGet]
    [HasPermission(PermissionType.PushRead)]
    [ProducesResponseType(typeof(TenantPushConfigResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> GetTenantPushConfig(CancellationToken ct)
    {
        Guid tenantId = tenantContext.TenantId.Value;

        Result<TenantPushConfigDto?> result = await bus.InvokeAsync<Result<TenantPushConfigDto?>>(
            new GetTenantPushConfigQuery(tenantId), ct);

        if (!result.IsSuccess)
        {
            return result.ToActionResult();
        }

        if (result.Value is null)
        {
            return NoContent();
        }

        TenantPushConfigDto dto = result.Value;
        TenantPushConfigResponse response = new(dto.Id, dto.TenantId, dto.Platform, dto.EncryptedCredentials, dto.IsEnabled);
        return Ok(response);
    }

    [HttpPut]
    [HasPermission(PermissionType.PushConfigWrite)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpsertTenantPushConfig(
        [FromBody] UpsertTenantPushConfigRequest request,
        CancellationToken ct)
    {
        TenantId tenantId = tenantContext.TenantId;

        Result result = await bus.InvokeAsync<Result>(
            new UpsertTenantPushConfigCommand(tenantId, request.Platform, request.Credentials), ct);

        return result.ToNoContentResult();
    }

    [HttpPatch("enabled")]
    [HasPermission(PermissionType.PushConfigWrite)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetTenantPushEnabled(
        [FromBody] SetTenantPushEnabledRequest request,
        CancellationToken ct)
    {
        TenantId tenantId = tenantContext.TenantId;

        Result result = await bus.InvokeAsync<Result>(
            new SetTenantPushEnabledCommand(tenantId, request.Platform, request.IsEnabled), ct);

        return result.ToNoContentResult();
    }

    [HttpDelete("{platform}")]
    [HasPermission(PermissionType.PushConfigWrite)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveTenantPushConfig(
        PushPlatform platform,
        CancellationToken ct)
    {
        TenantId tenantId = tenantContext.TenantId;

        Result result = await bus.InvokeAsync<Result>(
            new RemoveTenantPushConfigCommand(tenantId, platform), ct);

        return result.ToNoContentResult();
    }
}

public sealed record SetTenantPushEnabledRequest(
    PushPlatform Platform,
    bool IsEnabled);
