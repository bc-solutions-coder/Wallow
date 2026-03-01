using Foundry.Shared.Infrastructure.Workflows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;

namespace Foundry.Shared.Infrastructure.Tests.Workflows;

public class ElsaExtensionsTests
{
    private static IConfiguration CreateConfigurationWithConnectionString()
    {
        Dictionary<string, string?> settings = new()
        {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test;Username=test;Password=test"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
    }

    private static IHostEnvironment CreateEnvironment(string environmentName)
    {
        IHostEnvironment environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(environmentName);
        return environment;
    }

    [Fact]
    public void AddFoundryWorkflows_RegistersElsaServices_InServiceCollection()
    {
        ServiceCollection services = new();
        IConfiguration configuration = CreateConfigurationWithConnectionString();
        IHostEnvironment environment = CreateEnvironment(Environments.Development);

        services.AddFoundryWorkflows(configuration, environment);

        // Elsa registers many services; verify some well-known Elsa types are present
        List<Type> registeredServiceTypes = services.Select(sd => sd.ServiceType).ToList();

        registeredServiceTypes.Should().Contain(t => t.FullName != null && t.FullName.Contains("Elsa"),
            "AddElsa should register Elsa-namespaced services");
    }

    [Fact]
    public void AddFoundryWorkflows_WithoutConnectionString_ThrowsInvalidOperationException()
    {
        ServiceCollection services = new();
        IConfiguration configuration = new ConfigurationBuilder().Build();
        IHostEnvironment environment = CreateEnvironment(Environments.Development);

        Action act = () => services.AddFoundryWorkflows(configuration, environment);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*DefaultConnection*");
    }

    [Fact]
    public void AddFoundryWorkflows_ReturnsServiceCollection()
    {
        ServiceCollection services = new();
        IConfiguration configuration = CreateConfigurationWithConnectionString();
        IHostEnvironment environment = CreateEnvironment(Environments.Development);

        IServiceCollection result = services.AddFoundryWorkflows(configuration, environment);

        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddFoundryWorkflows_RegistersMultipleElsaServices()
    {
        ServiceCollection services = new();
        IConfiguration configuration = CreateConfigurationWithConnectionString();
        IHostEnvironment environment = CreateEnvironment(Environments.Development);

        services.AddFoundryWorkflows(configuration, environment);

        int elsaServiceCount = services.Count(sd =>
            sd.ServiceType.FullName != null && sd.ServiceType.FullName.Contains("Elsa"));

        elsaServiceCount.Should().BeGreaterThan(10,
            "AddElsa with management, runtime, identity, scheduling, http, and email should register many services");
    }

    [Fact]
    public void AddFoundryWorkflows_InDevelopment_WithoutSigningKey_UsesDefaultKey()
    {
        ServiceCollection services = new();
        IConfiguration configuration = CreateConfigurationWithConnectionString();
        IHostEnvironment environment = CreateEnvironment(Environments.Development);

        Action act = () => services.AddFoundryWorkflows(configuration, environment);

        act.Should().NotThrow();
    }

    [Fact]
    public void AddFoundryWorkflows_InProduction_WithoutSigningKey_ThrowsInvalidOperationException()
    {
        ServiceCollection services = new();
        IConfiguration configuration = CreateConfigurationWithConnectionString();
        IHostEnvironment environment = CreateEnvironment(Environments.Production);

        Action act = () => services.AddFoundryWorkflows(configuration, environment);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*SigningKey*");
    }

    [Fact]
    public void AddFoundryWorkflows_InStaging_WithoutSigningKey_ThrowsInvalidOperationException()
    {
        ServiceCollection services = new();
        IConfiguration configuration = CreateConfigurationWithConnectionString();
        IHostEnvironment environment = CreateEnvironment(Environments.Staging);

        Action act = () => services.AddFoundryWorkflows(configuration, environment);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*SigningKey*");
    }

    [Fact]
    public void AddFoundryWorkflows_InProduction_WithSigningKey_DoesNotThrow()
    {
        ServiceCollection services = new();
        Dictionary<string, string?> settings = new()
        {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test;Username=test;Password=test",
            ["Elsa:Identity:SigningKey"] = "my-production-signing-key-that-is-configured"
        };
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        IHostEnvironment environment = CreateEnvironment(Environments.Production);

        Action act = () => services.AddFoundryWorkflows(configuration, environment);

        act.Should().NotThrow();
    }
}
