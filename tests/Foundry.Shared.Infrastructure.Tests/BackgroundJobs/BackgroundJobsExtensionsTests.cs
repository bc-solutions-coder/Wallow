using Foundry.Shared.Infrastructure.BackgroundJobs;
using Foundry.Shared.Kernel.BackgroundJobs;
using Microsoft.Extensions.DependencyInjection;

namespace Foundry.Shared.Infrastructure.Tests.BackgroundJobs;

public class BackgroundJobsExtensionsTests
{
    [Fact]
    public void AddFoundryBackgroundJobs_RegistersIJobScheduler()
    {
        ServiceCollection services = new();

        services.AddFoundryBackgroundJobs();

        services.Should().ContainSingle(sd => sd.ServiceType == typeof(IJobScheduler));
    }

    [Fact]
    public void AddFoundryBackgroundJobs_RegistersHangfireJobSchedulerAsImplementation()
    {
        ServiceCollection services = new();

        services.AddFoundryBackgroundJobs();

        ServiceDescriptor descriptor = services.Single(sd => sd.ServiceType == typeof(IJobScheduler));
        descriptor.ImplementationType.Should().Be<HangfireJobScheduler>();
    }

    [Fact]
    public void AddFoundryBackgroundJobs_RegistersAsSingleton()
    {
        ServiceCollection services = new();

        services.AddFoundryBackgroundJobs();

        ServiceDescriptor descriptor = services.Single(sd => sd.ServiceType == typeof(IJobScheduler));
        descriptor.Lifetime.Should().Be(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddFoundryBackgroundJobs_ReturnsServiceCollection()
    {
        ServiceCollection services = new();

        IServiceCollection result = services.AddFoundryBackgroundJobs();

        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddFoundryBackgroundJobs_ResolvesCorrectly()
    {
        ServiceCollection services = new();
        services.AddFoundryBackgroundJobs();
        ServiceProvider provider = services.BuildServiceProvider();

        IJobScheduler resolved = provider.GetRequiredService<IJobScheduler>();

        resolved.Should().NotBeNull();
        resolved.Should().BeOfType<HangfireJobScheduler>();
    }
}
