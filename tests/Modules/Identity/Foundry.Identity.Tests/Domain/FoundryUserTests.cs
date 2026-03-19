using Foundry.Identity.Domain.Entities;
using Foundry.Shared.Kernel.Domain;
using Microsoft.Extensions.Time.Testing;

namespace Foundry.Identity.Tests.Domain;

public class FoundryUserTests
{
    private readonly FakeTimeProvider _timeProvider = new(new DateTimeOffset(2025, 1, 15, 10, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Create_WithValidData_ReturnsUserWithCorrectProperties()
    {
        Guid tenantId = Guid.NewGuid();
        string firstName = "John";
        string lastName = "Doe";
        string email = "john.doe@example.com";

        FoundryUser user = FoundryUser.Create(tenantId, firstName, lastName, email, _timeProvider);

        user.Id.Should().NotBe(Guid.Empty);
        user.TenantId.Should().Be(tenantId);
        user.FirstName.Should().Be(firstName);
        user.LastName.Should().Be(lastName);
        user.Email.Should().Be(email);
        user.UserName.Should().Be(email);
        user.IsActive.Should().BeTrue();
        user.CreatedAt.Should().Be(_timeProvider.GetUtcNow());
        user.DeactivatedAt.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithBlankFirstName_ThrowsBusinessRuleException(string? firstName)
    {
        Func<FoundryUser> act = () => FoundryUser.Create(Guid.NewGuid(), firstName!, "Doe", "test@example.com", _timeProvider);

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*first name*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithBlankLastName_ThrowsBusinessRuleException(string? lastName)
    {
        Func<FoundryUser> act = () => FoundryUser.Create(Guid.NewGuid(), "John", lastName!, "test@example.com", _timeProvider);

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*last name*");
    }

    [Fact]
    public void Create_IsActiveDefaultsToTrue()
    {
        FoundryUser user = FoundryUser.Create(Guid.NewGuid(), "Jane", "Smith", "jane@example.com", _timeProvider);

        user.IsActive.Should().BeTrue();
    }
}
