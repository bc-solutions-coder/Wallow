using Foundry.Communications.Application.Channels.Email.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Foundry.Communications.Tests.Application.Extensions;

public class EmailApplicationExtensionsTests
{
    [Fact]
    public void AddEmailApplication_RegistersValidators()
    {
        ServiceCollection services = new();

        services.AddEmailApplication();

        services.Should().NotBeEmpty();
    }

    [Fact]
    public void AddEmailApplication_ReturnsServiceCollection()
    {
        ServiceCollection services = new();

        IServiceCollection result = services.AddEmailApplication();

        result.Should().BeSameAs(services);
    }
}
