using Foundry.Shared.Infrastructure.Core.Auditing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Foundry.Shared.Infrastructure.Tests.Auditing;

public class AuditingExtensionsAdditionalTests
{
    [Fact]
    public void AddFoundryAuditing_RegistersAuditInterceptorAsSingleton()
    {
        ServiceCollection services = new();
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection([new KeyValuePair<string, string?>("ConnectionStrings:DefaultConnection", "Host=localhost;Database=test")])
            .Build();

        services.AddFoundryAuditing(configuration);

        ServiceProvider provider = services.BuildServiceProvider();
        AuditInterceptor? interceptor = provider.GetService<AuditInterceptor>();
        interceptor.Should().NotBeNull();
    }

    [Fact]
    public void AddFoundryAuditing_ReturnsServiceCollectionForChaining()
    {
        ServiceCollection services = new();
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection([new KeyValuePair<string, string?>("ConnectionStrings:DefaultConnection", "Host=localhost;Database=test")])
            .Build();

        IServiceCollection result = services.AddFoundryAuditing(configuration);

        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddFoundryAuditing_RegistersAuditDbContext()
    {
        ServiceCollection services = new();
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection([new KeyValuePair<string, string?>("ConnectionStrings:DefaultConnection", "Host=localhost;Database=test")])
            .Build();

        services.AddFoundryAuditing(configuration);

        ServiceDescriptor? descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(AuditDbContext));
        descriptor.Should().NotBeNull();
    }
}
