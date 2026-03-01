using Foundry.Communications.Application.Channels.InApp.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Foundry.Communications.Tests.Application.Extensions;

public class InAppApplicationExtensionsTests
{
    [Fact]
    public void AddNotificationsApplication_RegistersValidators()
    {
        ServiceCollection services = new();

        services.AddNotificationsApplication();

        services.Should().NotBeEmpty();
    }

    [Fact]
    public void AddNotificationsApplication_ReturnsServiceCollection()
    {
        ServiceCollection services = new();

        IServiceCollection result = services.AddNotificationsApplication();

        result.Should().BeSameAs(services);
    }
}
