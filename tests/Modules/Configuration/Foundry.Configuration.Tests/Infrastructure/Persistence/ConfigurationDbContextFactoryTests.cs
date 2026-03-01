using Foundry.Configuration.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Foundry.Configuration.Tests.Infrastructure.Persistence;

public class ConfigurationDbContextFactoryTests
{
    [Fact]
    public void CreateDbContext_ReturnsConfiguredDbContext()
    {
        ConfigurationDbContextFactory factory = new();

        ConfigurationDbContext context = factory.CreateDbContext([]);

        context.Should().NotBeNull();
        context.Should().BeOfType<ConfigurationDbContext>();
    }

    [Fact]
    public void CreateDbContext_ConfiguresNpgsqlProvider()
    {
        ConfigurationDbContextFactory factory = new();

        ConfigurationDbContext context = factory.CreateDbContext(Array.Empty<string>());

        context.Database.ProviderName.Should().Be("Npgsql.EntityFrameworkCore.PostgreSQL");
    }
}
