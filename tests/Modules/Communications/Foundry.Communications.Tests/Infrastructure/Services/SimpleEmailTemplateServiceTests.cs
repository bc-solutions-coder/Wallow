using Foundry.Communications.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace Foundry.Communications.Tests.Infrastructure.Services;

public class SimpleEmailTemplateServiceTests
{
    private readonly SimpleEmailTemplateService _service;

    public SimpleEmailTemplateServiceTests()
    {
        ILogger<SimpleEmailTemplateService> logger = Substitute.For<ILogger<SimpleEmailTemplateService>>();
        _service = new SimpleEmailTemplateService(logger);
    }

    [Fact]
    public async Task RenderAsync_WelcomeEmail_ReplacesPlaceholders()
    {
        object model = new { FirstName = "John", LastName = "Doe", Email = "john@example.com" };

        string result = await _service.RenderAsync("welcomeemail", model);

        result.Should().Contain("John");
        result.Should().Contain("Doe");
        result.Should().Contain("john@example.com");
        result.Should().Contain("Welcome to Foundry!");
    }

    [Fact]
    public async Task RenderAsync_TaskCreated_ReplacesPlaceholders()
    {
        object model = new { TaskTitle = "Build Feature", TaskDescription = "Implement login", AssignedTo = "Jane" };

        string result = await _service.RenderAsync("taskcreated", model);

        result.Should().Contain("Build Feature");
        result.Should().Contain("Implement login");
        result.Should().Contain("Jane");
        result.Should().Contain("New Task Created");
    }

    [Fact]
    public async Task RenderAsync_TaskAssigned_ReplacesPlaceholders()
    {
        object model = new { TaskTitle = "Code Review", TaskDescription = "Review PR #42", DueDate = "2026-03-01" };

        string result = await _service.RenderAsync("taskassigned", model);

        result.Should().Contain("Code Review");
        result.Should().Contain("Review PR #42");
        result.Should().Contain("2026-03-01");
        result.Should().Contain("Task Assigned to You");
    }

    [Fact]
    public async Task RenderAsync_TaskCompleted_ReplacesPlaceholders()
    {
        object model = new { TaskTitle = "Deploy", CompletedBy = "Alice", CompletedAt = "2026-02-27" };

        string result = await _service.RenderAsync("taskcompleted", model);

        result.Should().Contain("Deploy");
        result.Should().Contain("Alice");
        result.Should().Contain("2026-02-27");
        result.Should().Contain("Task Completed");
    }

    [Fact]
    public async Task RenderAsync_BillingInvoice_ReplacesPlaceholders()
    {
        object model = new { InvoiceNumber = "INV-001", Amount = "$500.00", DueDate = "2026-04-01" };

        string result = await _service.RenderAsync("billinginvoice", model);

        result.Should().Contain("INV-001");
        result.Should().Contain("$500.00");
        result.Should().Contain("2026-04-01");
        result.Should().Contain("New Invoice");
    }

    [Fact]
    public async Task RenderAsync_SystemNotification_ReplacesPlaceholders()
    {
        object model = new { Message = "System will undergo maintenance" };

        string result = await _service.RenderAsync("systemnotification", model);

        result.Should().Contain("System will undergo maintenance");
        result.Should().Contain("System Notification");
    }

    [Fact]
    public async Task RenderAsync_PasswordReset_ReplacesPlaceholders()
    {
        object model = new { Email = "user@test.com", ResetToken = "abc-123-xyz" };

        string result = await _service.RenderAsync("passwordreset", model);

        result.Should().Contain("user@test.com");
        result.Should().Contain("abc-123-xyz");
        result.Should().Contain("Password Reset Request");
    }

    [Fact]
    public async Task RenderAsync_DataRequestReceived_ReplacesPlaceholders()
    {
        object model = new { RequestType = "export", RequestId = "REQ-001", RequestedAt = "2026-02-27" };

        string result = await _service.RenderAsync("datarequestreceived", model);

        result.Should().Contain("export");
        result.Should().Contain("REQ-001");
        result.Should().Contain("2026-02-27");
        result.Should().Contain("Data Request Received");
    }

    [Fact]
    public async Task RenderAsync_DataExportReady_ReplacesPlaceholders()
    {
        object model = new
        {
            RequestId = "REQ-002",
            FileSizeFormatted = "15 MB",
            DownloadUrl = "https://example.com/download",
            ExpiresAt = "2026-03-06"
        };

        string result = await _service.RenderAsync("dataexportready", model);

        result.Should().Contain("REQ-002");
        result.Should().Contain("15 MB");
        result.Should().Contain("https://example.com/download");
        result.Should().Contain("2026-03-06");
        result.Should().Contain("Your Data Export is Ready");
    }

    [Fact]
    public async Task RenderAsync_DataErasureComplete_ReplacesPlaceholders()
    {
        object model = new { RequestId = "REQ-003", CompletedAt = "2026-02-28" };

        string result = await _service.RenderAsync("dataerasurecomplete", model);

        result.Should().Contain("REQ-003");
        result.Should().Contain("2026-02-28");
        result.Should().Contain("Data Erasure Completed");
    }

    [Fact]
    public async Task RenderAsync_DataRequestRejected_ReplacesPlaceholders()
    {
        object model = new { RequestType = "erasure", RequestId = "REQ-004", RejectionReason = "Identity could not be verified" };

        string result = await _service.RenderAsync("datarequestrejected", model);

        result.Should().Contain("erasure");
        result.Should().Contain("REQ-004");
        result.Should().Contain("Identity could not be verified");
        result.Should().Contain("Data Request Update");
    }

    [Fact]
    public async Task RenderAsync_DataRequestVerificationRequired_ReplacesPlaceholders()
    {
        object model = new { RequestType = "export", RequestId = "REQ-005", VerificationToken = "verify-xyz" };

        string result = await _service.RenderAsync("datarequestverificationrequired", model);

        result.Should().Contain("export");
        result.Should().Contain("REQ-005");
        result.Should().Contain("verify-xyz");
        result.Should().Contain("Verification Required");
    }

    [Fact]
    public async Task RenderAsync_UnknownTemplate_ReturnsDefaultWithMessage()
    {
        object model = new { Message = "Fallback content" };

        string result = await _service.RenderAsync("unknowntemplate", model);

        result.Should().Contain("Fallback content");
        result.Should().Contain("<html>");
    }

    [Fact]
    public async Task RenderAsync_IsCaseInsensitive()
    {
        object model = new { FirstName = "Test", LastName = "User", Email = "test@example.com" };

        string result = await _service.RenderAsync("WelcomeEmail", model);

        result.Should().Contain("Welcome to Foundry!");
        result.Should().Contain("Test");
    }

    [Fact]
    public async Task RenderAsync_PropertyNotInModel_LeavesPlaceholderUnchanged()
    {
        object model = new { FirstName = "John" };

        string result = await _service.RenderAsync("welcomeemail", model);

        result.Should().Contain("John");
        result.Should().Contain("{{LastName}}");
    }

    [Fact]
    public async Task RenderAsync_ReturnsCompletedTask()
    {
        object model = new { Message = "Test" };

        Task<string> task = _service.RenderAsync("systemnotification", model);

        task.IsCompleted.Should().BeTrue();
        string result = await task;
        result.Should().NotBeEmpty();
    }
}
