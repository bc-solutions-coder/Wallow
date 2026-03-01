using Foundry.Api.Jobs;

namespace Foundry.Api.Tests.Jobs;

public class SystemHeartbeatJobTests
{
    private readonly SystemHeartbeatJob _sut = new();

    [Fact]
    public async Task ExecuteAsync_WhenCalled_CompletesSuccessfully()
    {
        Task result = _sut.ExecuteAsync();

        await result;

        result.IsCompletedSuccessfully.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WhenCalledMultipleTimes_CompletesEachTime()
    {
        await _sut.ExecuteAsync();
        await _sut.ExecuteAsync();
        await _sut.ExecuteAsync();

        // No exceptions thrown across multiple invocations
    }
}
