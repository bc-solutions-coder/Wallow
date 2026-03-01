using Dapper;
using Foundry.Shared.Contracts.Billing;
using Foundry.Shared.Kernel.MultiTenancy;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Foundry.Billing.Infrastructure.Services;

public sealed class PaymentReportService : IPaymentReportService
{
    private readonly string _connectionString;
    private readonly ITenantContext _tenantContext;

    public PaymentReportService(IConfiguration configuration, ITenantContext tenantContext)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        _tenantContext = tenantContext;
    }

    public async Task<IReadOnlyList<PaymentReportRow>> GetPaymentsAsync(
        DateTime from,
        DateTime to,
        CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                p."Id" as "PaymentId",
                i."InvoiceNumber",
                p."Amount_Amount" as "Amount",
                p."Amount_Currency" as "Currency",
                p."Method"::text as "Method",
                p."Status"::text as "Status",
                p."CompletedAt" as "PaymentDate"
            FROM billing."Payments" p
            JOIN billing."Invoices" i ON i."Id" = p."InvoiceId"
            WHERE p."TenantId" = @TenantId
              AND p."CreatedAt" >= @From
              AND p."CreatedAt" < @To
            ORDER BY p."CreatedAt" DESC
            """;

        await using NpgsqlConnection connection = new NpgsqlConnection(_connectionString);
        IEnumerable<PaymentReportRow> results = await connection.QueryAsync<PaymentReportRow>(
            sql,
            new { TenantId = _tenantContext.TenantId.Value, From = from, To = to });

        return results.AsList();
    }
}
