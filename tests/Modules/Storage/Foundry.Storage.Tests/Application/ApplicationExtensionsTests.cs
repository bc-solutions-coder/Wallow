using FluentValidation;
using Foundry.Storage.Application.Commands.CreateBucket;
using Foundry.Storage.Application.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Foundry.Storage.Tests.Application;

public class ApplicationExtensionsTests
{
    [Fact]
    public void AddStorageApplication_RegistersValidators()
    {
        ServiceCollection services = new();

        services.AddStorageApplication();

        ServiceProvider provider = services.BuildServiceProvider();
        IValidator<CreateBucketCommand> validator = provider.GetRequiredService<IValidator<CreateBucketCommand>>();
        validator.Should().NotBeNull();
        validator.Should().BeOfType<CreateBucketValidator>();
    }
}
