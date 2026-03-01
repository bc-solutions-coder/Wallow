namespace Foundry.Billing.Application.Metering.DTOs;

/// <summary>
/// Summary of current usage across meters.
/// </summary>
public sealed record UsageSummaryDto(
    string MeterCode,
    string DisplayName,
    string Unit,
    decimal CurrentValue,
    decimal? Limit,
    decimal? PercentUsed,
    string Period);
