using Foundry.Shared.Kernel.Domain;

namespace Foundry.Shared.Kernel.Tests.Domain;

public class DomainExceptionTests
{
    private sealed class TestDomainException : DomainException
    {
        public TestDomainException() { }
        public TestDomainException(string message) : base(message) { }
        public TestDomainException(string message, Exception innerException) : base(message, innerException) { }
        public TestDomainException(string code, string message) : base(code, message) { }
        public TestDomainException(string code, string message, Exception innerException) : base(code, message, innerException) { }
    }

    // --- DomainException ---

    [Fact]
    public void DomainException_Parameterless_HasDefaults()
    {
        TestDomainException ex = new();

        ex.Code.Should().Be(string.Empty);
        ex.InnerException.Should().BeNull();
    }

    [Fact]
    public void DomainException_WithMessage_SetsMessage()
    {
        TestDomainException ex = new("something failed");

        ex.Message.Should().Be("something failed");
        ex.Code.Should().Be(string.Empty);
    }

    [Fact]
    public void DomainException_WithMessageAndInner_SetsBoth()
    {
        InvalidOperationException inner = new("root cause");

        TestDomainException ex = new("wrapper", inner);

        ex.Message.Should().Be("wrapper");
        ex.InnerException.Should().BeSameAs(inner);
    }

    [Fact]
    public void DomainException_WithCodeAndMessage_SetsBoth()
    {
        TestDomainException ex = new("ERR_CODE", "detailed message");

        ex.Code.Should().Be("ERR_CODE");
        ex.Message.Should().Be("detailed message");
    }

    [Fact]
    public void DomainException_WithCodeMessageAndInner_SetsAll()
    {
        InvalidOperationException inner = new("root cause");

        TestDomainException ex = new("ERR_CODE", "detailed message", inner);

        ex.Code.Should().Be("ERR_CODE");
        ex.Message.Should().Be("detailed message");
        ex.InnerException.Should().BeSameAs(inner);
    }

    // --- EntityNotFoundException ---

    [Fact]
    public void EntityNotFoundException_WithEntityNameAndId_SetsProperties()
    {
        Guid id = Guid.NewGuid();

        EntityNotFoundException ex = new("Order", id);

        ex.EntityName.Should().Be("Order");
        ex.EntityId.Should().Be(id);
        ex.Code.Should().Be("Order.NotFound");
        ex.Message.Should().Contain("Order");
        ex.Message.Should().Contain(id.ToString());
    }

    [Fact]
    public void EntityNotFoundException_WithMessage_SetsMessage()
    {
        EntityNotFoundException ex = new("something went wrong");

        ex.Message.Should().Be("something went wrong");
    }

    [Fact]
    public void EntityNotFoundException_WithMessageAndInner_SetsInnerException()
    {
        InvalidOperationException inner = new("inner");

        EntityNotFoundException ex = new("outer", inner);

        ex.Message.Should().Be("outer");
        ex.InnerException.Should().BeSameAs(inner);
    }

    [Fact]
    public void EntityNotFoundException_Parameterless_HasDefaults()
    {
        EntityNotFoundException ex = new();

        ex.EntityName.Should().Be(string.Empty);
        ex.EntityId.Should().Be(Guid.Empty);
        ex.Code.Should().Be(string.Empty);
    }

    [Fact]
    public void EntityNotFoundException_InheritsFromDomainException()
    {
        EntityNotFoundException ex = new("Order", Guid.NewGuid());

        ex.Should().BeAssignableTo<DomainException>();
        ex.Should().BeAssignableTo<Exception>();
    }

    // --- BusinessRuleException ---

    [Fact]
    public void BusinessRuleException_WithCodeAndMessage_SetsProperties()
    {
        BusinessRuleException ex = new("INSUFFICIENT_FUNDS", "Not enough balance");

        ex.Code.Should().Be("INSUFFICIENT_FUNDS");
        ex.Message.Should().Be("Not enough balance");
    }

    [Fact]
    public void BusinessRuleException_WithMessage_SetsMessage()
    {
        BusinessRuleException ex = new("something went wrong");

        ex.Message.Should().Be("something went wrong");
    }

    [Fact]
    public void BusinessRuleException_WithMessageAndInner_SetsInnerException()
    {
        InvalidOperationException inner = new("inner");

        BusinessRuleException ex = new("outer", inner);

        ex.Message.Should().Be("outer");
        ex.InnerException.Should().BeSameAs(inner);
    }

    [Fact]
    public void BusinessRuleException_Parameterless_HasDefaults()
    {
        BusinessRuleException ex = new();

        ex.Code.Should().Be(string.Empty);
    }

    [Fact]
    public void BusinessRuleException_InheritsFromDomainException()
    {
        BusinessRuleException ex = new("CODE", "msg");

        ex.Should().BeAssignableTo<DomainException>();
        ex.Should().BeAssignableTo<Exception>();
    }
}
