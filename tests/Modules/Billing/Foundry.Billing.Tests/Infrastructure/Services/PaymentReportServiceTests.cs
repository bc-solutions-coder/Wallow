using Foundry.Billing.Infrastructure.Services;
using Foundry.Shared.Kernel.MultiTenancy;
using Microsoft.Extensions.Configuration;

namespace Foundry.Billing.Tests.Infrastructure.Services;

public class PaymentReportServiceTests
{
    [Fact]
    public void Constructor_WithValidConnectionString_CreatesInstance()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test"
            })
            .Build();
        ITenantContext tenantContext = Substitute.For<ITenantContext>();

        PaymentReportService service = new PaymentReportService(configuration, tenantContext);

        service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithMissingConnectionString_ThrowsInvalidOperationException()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
        ITenantContext tenantContext = Substitute.For<ITenantContext>();

        Action act = () => _ = new PaymentReportService(configuration, tenantContext);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*DefaultConnection*");
    }
}
