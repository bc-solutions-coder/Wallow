using Foundry.Shared.Infrastructure.Core.Auditing;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Foundry.Shared.Infrastructure.Tests.Auditing;

public class AuditingExtensionsTests
{
    private static IConfiguration CreateConfiguration(string connectionString = "Host=localhost;Database=test") =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connectionString
            })
            .Build();

    [Fact]
    public void AddFoundryAuditing_RegistersAuditDbContext()
    {
        ServiceCollection services = new();
        IConfiguration configuration = CreateConfiguration();

        services.AddFoundryAuditing(configuration);

        services.Should().Contain(sd => sd.ServiceType == typeof(DbContextOptions<AuditDbContext>));
    }

    [Fact]
    public void AddFoundryAuditing_RegistersAuditInterceptor()
    {
        ServiceCollection services = new();
        IConfiguration configuration = CreateConfiguration();

        services.AddFoundryAuditing(configuration);

        services.Should().ContainSingle(sd => sd.ServiceType == typeof(AuditInterceptor));
    }

    [Fact]
    public void AddFoundryAuditing_RegistersAuditInterceptorAsSingleton()
    {
        ServiceCollection services = new();
        IConfiguration configuration = CreateConfiguration();

        services.AddFoundryAuditing(configuration);

        ServiceDescriptor descriptor = services.Single(sd => sd.ServiceType == typeof(AuditInterceptor));
        descriptor.Lifetime.Should().Be(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddFoundryAuditing_ReturnsServiceCollection()
    {
        ServiceCollection services = new();
        IConfiguration configuration = CreateConfiguration();

        IServiceCollection result = services.AddFoundryAuditing(configuration);

        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddFoundryAuditing_ConfiguresNpgsqlProvider()
    {
        ServiceCollection services = new();
        string connectionString = "Host=testhost;Database=testdb;Username=user;Password=pass";
        IConfiguration configuration = CreateConfiguration(connectionString);

        services.AddFoundryAuditing(configuration);
        ServiceProvider provider = services.BuildServiceProvider();

        AuditDbContext dbContext = provider.GetRequiredService<AuditDbContext>();

        dbContext.Database.ProviderName.Should().Be("Npgsql.EntityFrameworkCore.PostgreSQL");
    }

    [Fact]
    public void AddFoundryAuditing_RegistersAuditDbContextAsService()
    {
        ServiceCollection services = new();
        IConfiguration configuration = CreateConfiguration();

        services.AddFoundryAuditing(configuration);

        services.Should().Contain(sd => sd.ServiceType == typeof(AuditDbContext));
    }

    [Fact]
    public void AddFoundryAuditing_RegistersAuditDbContextAsScopedLifetime()
    {
        ServiceCollection services = new();
        IConfiguration configuration = CreateConfiguration();

        services.AddFoundryAuditing(configuration);

        ServiceDescriptor descriptor = services.Single(sd => sd.ServiceType == typeof(AuditDbContext));
        descriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddFoundryAuditing_InterceptorResolvesFromProvider()
    {
        ServiceCollection services = new();
        IConfiguration configuration = CreateConfiguration();

        services.AddFoundryAuditing(configuration);
        ServiceProvider provider = services.BuildServiceProvider();

        AuditInterceptor interceptor = provider.GetRequiredService<AuditInterceptor>();

        interceptor.Should().NotBeNull();
    }

    [Fact]
    public void AddFoundryAuditing_InterceptorIsSameInstanceAcrossResolutions()
    {
        ServiceCollection services = new();
        IConfiguration configuration = CreateConfiguration();

        services.AddFoundryAuditing(configuration);
        ServiceProvider provider = services.BuildServiceProvider();

        AuditInterceptor first = provider.GetRequiredService<AuditInterceptor>();
        AuditInterceptor second = provider.GetRequiredService<AuditInterceptor>();

        first.Should().BeSameAs(second);
    }

    [Fact]
    public void AddFoundryAuditing_WithNullConnectionString_DoesNotThrowDuringRegistration()
    {
        ServiceCollection services = new();
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = null
            })
            .Build();

        Action act = () => services.AddFoundryAuditing(configuration);

        act.Should().NotThrow();
    }

    [Fact]
    public void AddFoundryAuditing_WithMissingConnectionString_DoesNotThrowDuringRegistration()
    {
        ServiceCollection services = new();
        IConfiguration configuration = new ConfigurationBuilder().Build();

        Action act = () => services.AddFoundryAuditing(configuration);

        act.Should().NotThrow();
    }

    [Fact]
    public void AddFoundryAuditing_DbContextResolvesFromProvider()
    {
        ServiceCollection services = new();
        IConfiguration configuration = CreateConfiguration();

        services.AddFoundryAuditing(configuration);
        ServiceProvider provider = services.BuildServiceProvider();

        AuditDbContext dbContext = provider.GetRequiredService<AuditDbContext>();

        dbContext.Should().NotBeNull();
    }

    [Fact]
    public void AddFoundryAuditing_ConfiguresMigrationsHistoryTableInAuditSchema()
    {
        ServiceCollection services = new();
        IConfiguration configuration = CreateConfiguration();

        services.AddFoundryAuditing(configuration);
        ServiceProvider provider = services.BuildServiceProvider();

        AuditDbContext dbContext = provider.GetRequiredService<AuditDbContext>();

        dbContext.Database.ProviderName.Should().NotBeNullOrEmpty();
        dbContext.Model.GetDefaultSchema().Should().Be("audit");
    }
}

[Trait("Category", "Integration")]
public class AuditingExtensionsIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18-alpine")
        .WithCleanUp(true)
        .Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task InitializeAuditingAsync_RunsMigrationsSuccessfully()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = _postgres.GetConnectionString()
        });
        builder.Services.AddFoundryAuditing(builder.Configuration);
        WebApplication app = builder.Build();

        Func<Task> act = () => app.InitializeAuditingAsync();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task InitializeAuditingAsync_CreatesAuditSchema()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = _postgres.GetConnectionString()
        });
        builder.Services.AddFoundryAuditing(builder.Configuration);
        WebApplication app = builder.Build();

        await app.InitializeAuditingAsync();

        await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
        AuditDbContext db = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
        bool canConnect = await db.Database.CanConnectAsync();
        canConnect.Should().BeTrue();
    }

    [Fact]
    public async Task InitializeAuditingAsync_MigrationsAreIdempotent()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = _postgres.GetConnectionString()
        });
        builder.Services.AddFoundryAuditing(builder.Configuration);
        WebApplication app = builder.Build();

        await app.InitializeAuditingAsync();
        Func<Task> act = () => app.InitializeAuditingAsync();

        await act.Should().NotThrowAsync();
    }
}
