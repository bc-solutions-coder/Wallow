using System.Diagnostics;
using System.Diagnostics.Metrics;
using Foundry.Shared.Kernel;

namespace Foundry.Billing.Application.Telemetry;

public static class BillingModuleTelemetry
{
    public static readonly ActivitySource ActivitySource = Diagnostics.CreateActivitySource("Billing");
    public static readonly Meter Meter = Diagnostics.CreateMeter("Billing");

    public static readonly Counter<long> InvoicesCreatedTotal =
        Meter.CreateCounter<long>("foundry.billing.invoices_created_total");

    public static readonly Histogram<double> InvoiceAmount =
        Meter.CreateHistogram<double>("foundry.billing.invoice_amount");
}
