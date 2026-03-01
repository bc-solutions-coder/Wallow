using Foundry.Billing.Application.Metering.DTOs;
using Foundry.Billing.Application.Metering.Services;
using Foundry.Billing.Domain.Metering.Enums;
using Microsoft.AspNetCore.Http;

namespace Foundry.Billing.Api.Middleware;

/// <summary>
/// Middleware that checks quotas before requests and increments counters after successful responses.
/// Only tracks API routes (/api/*).
/// </summary>
public sealed class MeteringMiddleware
{
    private readonly RequestDelegate _next;
    private const string ApiCallsMeterCode = "api.calls";

    public MeteringMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IMeteringService meteringService)
    {
        // Skip non-API routes
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            await _next(context);
            return;
        }

        // Check quota before processing request
        QuotaCheckResult quotaCheck = await meteringService.CheckQuotaAsync(ApiCallsMeterCode);

        if (!quotaCheck.IsAllowed && quotaCheck.ActionIfExceeded == QuotaAction.Block)
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers["X-RateLimit-Limit"] = quotaCheck.Limit.ToString("F0");
            context.Response.Headers["X-RateLimit-Remaining"] = "0";
            context.Response.Headers["Retry-After"] = GetSecondsUntilReset().ToString();
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Quota exceeded",
                limit = quotaCheck.Limit,
                currentUsage = quotaCheck.CurrentUsage,
                percentUsed = quotaCheck.PercentUsed
            });
            return;
        }

        // Add warning header if approaching limit (>80%)
        if (quotaCheck.PercentUsed > 80)
        {
            context.Response.Headers["X-Quota-Warning"] = $"{quotaCheck.PercentUsed:F0}% used";
        }

        // Add rate limit headers
        if (quotaCheck.Limit < decimal.MaxValue)
        {
            decimal remaining = Math.Max(0, quotaCheck.Limit - quotaCheck.CurrentUsage - 1);
            context.Response.Headers["X-RateLimit-Limit"] = quotaCheck.Limit.ToString("F0");
            context.Response.Headers["X-RateLimit-Remaining"] = remaining.ToString("F0");
            context.Response.Headers["X-RateLimit-Reset"] = GetResetTimestamp().ToString();
        }

        // Process the request
        await _next(context);

        // Only count successful requests (status < 400)
        if (context.Response.StatusCode < 400)
        {
            await meteringService.IncrementAsync(ApiCallsMeterCode);
        }
    }

    private static int GetSecondsUntilReset()
    {
        DateTime now = DateTime.UtcNow;
        DateTime nextMonth = new DateTime(now.Year, now.Month, 1).AddMonths(1);
        return (int)(nextMonth - now).TotalSeconds;
    }

    private static long GetResetTimestamp()
    {
        DateTime now = DateTime.UtcNow;
        DateTime nextMonth = new DateTime(now.Year, now.Month, 1).AddMonths(1);
        return new DateTimeOffset(nextMonth).ToUnixTimeSeconds();
    }
}
