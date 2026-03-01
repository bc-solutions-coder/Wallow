using Foundry.Shared.Contracts.Metering;

namespace Foundry.Tests.Common.Fakes;

public sealed class FakeMeteringQueryService : IMeteringQueryService
{
    public Task<QuotaStatus?> CheckQuotaAsync(Guid tenantId, string meterCode, CancellationToken ct = default)
        => Task.FromResult<QuotaStatus?>(null);
}
