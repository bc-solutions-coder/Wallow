using Dapper;
using Foundry.Shared.Contracts.Billing;
using Foundry.Shared.Kernel.MultiTenancy;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Foundry.Billing.Infrastructure.Services;

public sealed class InvoiceReportService : IInvoiceReportService
{
    private readonly string _connectionString;
    private readonly ITenantContext _tenantContext;

    public InvoiceReportService(IConfiguration configuration, ITenantContext tenantContext)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        _tenantContext = tenantContext;
    }

    public async Task<IReadOnlyList<InvoiceReportRow>> GetInvoicesAsync(
        DateTime from,
        DateTime to,
        CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                i."InvoiceNumber",
                'User_' || i."UserId" as "CustomerName",
                i."TotalAmount_Amount" as "Amount",
                i."TotalAmount_Currency" as "Currency",
                i."Status"::text as "Status",
                i."CreatedAt" as "IssueDate",
                i."DueDate"
            FROM billing."Invoices" i
            WHERE i."TenantId" = @TenantId
              AND i."Status" != 0
              AND i."CreatedAt" >= @From
              AND i."CreatedAt" < @To
            ORDER BY i."CreatedAt" DESC
            """;

        await using NpgsqlConnection connection = new NpgsqlConnection(_connectionString);
        IEnumerable<InvoiceReportRow> results = await connection.QueryAsync<InvoiceReportRow>(
            sql,
            new { TenantId = _tenantContext.TenantId.Value, From = from, To = to });

        return results.AsList();
    }
}
