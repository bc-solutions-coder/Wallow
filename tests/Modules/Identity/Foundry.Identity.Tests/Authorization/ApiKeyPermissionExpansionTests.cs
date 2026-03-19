using System.Security.Claims;
using Foundry.Identity.Application.Constants;
using Foundry.Identity.Infrastructure.Authorization;
using Foundry.Shared.Kernel.Identity.Authorization;
using Microsoft.AspNetCore.Http;

namespace Foundry.Identity.Tests.Authorization;

public class ApiKeyPermissionExpansionTests
{
    [Fact]
    public async Task InvokeAsync_WithApiKeyAuthMethod_ExpandsScopesToPermissions()
    {
        Claim[] claims = new[]
        {
            new Claim("auth_method", "api_key"),
            new Claim("scope", "invoices.read invoices.write")
        };

        ClaimsIdentity identity = new(claims, "ApiKey");
        DefaultHttpContext context = new DefaultHttpContext()
        {
            User = new ClaimsPrincipal(identity)
        };

        PermissionExpansionMiddleware middleware = new(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        List<string> permissions = context.User.FindAll("permission").Select(c => c.Value).ToList();
        permissions.Should().Contain(PermissionType.InvoicesRead);
        permissions.Should().Contain(PermissionType.InvoicesWrite);
    }

    [Fact]
    public async Task InvokeAsync_WithApiKeyAuthMethod_IgnoresUnknownScopes()
    {
        Claim[] claims = new[]
        {
            new Claim("auth_method", "api_key"),
            new Claim("scope", "unknown.scope invoices.read bogus.permission")
        };

        ClaimsIdentity identity = new(claims, "ApiKey");
        DefaultHttpContext context = new DefaultHttpContext()
        {
            User = new ClaimsPrincipal(identity)
        };

        PermissionExpansionMiddleware middleware = new(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        List<string> permissions = context.User.FindAll("permission").Select(c => c.Value).ToList();
        permissions.Should().ContainSingle();
        permissions.Should().Contain(PermissionType.InvoicesRead);
    }

    [Fact]
    public async Task InvokeAsync_WithApiKeyAuthMethod_ExpandsAllScopeTypes()
    {
        Claim[] claims = new[]
        {
            new Claim("auth_method", "api_key"),
            new Claim("scope", "invoices.read payments.write users.read notifications.write webhooks.manage")
        };

        ClaimsIdentity identity = new(claims, "ApiKey");
        DefaultHttpContext context = new DefaultHttpContext()
        {
            User = new ClaimsPrincipal(identity)
        };

        PermissionExpansionMiddleware middleware = new(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        List<string> permissions = context.User.FindAll("permission").Select(c => c.Value).ToList();
        permissions.Should().HaveCount(5);
        permissions.Should().Contain(PermissionType.InvoicesRead);
        permissions.Should().Contain(PermissionType.PaymentsWrite);
        permissions.Should().Contain(PermissionType.UsersRead);
        permissions.Should().Contain(PermissionType.NotificationsWrite);
        permissions.Should().Contain(PermissionType.WebhooksManage);
    }

    [Fact]
    public void ValidScopes_ContainsExactlyFourteenEntries()
    {
        ApiScopes.ValidScopes.Should().HaveCount(14);
    }

    [Theory]
    [InlineData("invoices.read")]
    [InlineData("invoices.write")]
    [InlineData("payments.read")]
    [InlineData("payments.write")]
    [InlineData("subscriptions.read")]
    [InlineData("subscriptions.write")]
    [InlineData("users.read")]
    [InlineData("users.write")]
    [InlineData("notifications.read")]
    [InlineData("notifications.write")]
    [InlineData("webhooks.manage")]
    public void ValidScopes_ContainsExpectedScope(string scope)
    {
        ApiScopes.ValidScopes.Should().Contain(scope);
    }

    [Theory]
    [InlineData("billing.read", PermissionType.BillingRead)]
    [InlineData("billing.manage", PermissionType.BillingManage)]
    [InlineData("invoices.read", PermissionType.InvoicesRead)]
    [InlineData("invoices.write", PermissionType.InvoicesWrite)]
    [InlineData("payments.read", PermissionType.PaymentsRead)]
    [InlineData("payments.write", PermissionType.PaymentsWrite)]
    [InlineData("users.read", PermissionType.UsersRead)]
    [InlineData("users.write", PermissionType.UsersUpdate)]
    [InlineData("roles.read", PermissionType.RolesRead)]
    [InlineData("storage.read", PermissionType.StorageRead)]
    [InlineData("storage.write", PermissionType.StorageWrite)]
    [InlineData("messaging.access", PermissionType.MessagingAccess)]
    [InlineData("notifications.read", PermissionType.NotificationsRead)]
    [InlineData("notifications.write", PermissionType.NotificationsWrite)]
    [InlineData("showcases.read", PermissionType.ShowcasesRead)]
    [InlineData("showcases.manage", PermissionType.ShowcasesManage)]
    [InlineData("inquiries.read", PermissionType.InquiriesRead)]
    [InlineData("inquiries.write", PermissionType.InquiriesWrite)]
    [InlineData("webhooks.manage", PermissionType.WebhooksManage)]
    [InlineData("serviceaccounts.read", PermissionType.ServiceAccountsRead)]
    public void MapScopeToPermission_KnownScope_ReturnsExpectedPermission(string scope, string expectedPermission)
    {
        string? result = ScopePermissionMapper.MapScopeToPermission(scope);

        result.Should().Be(expectedPermission);
    }

    [Theory]
    [InlineData("unknown.scope")]
    [InlineData("bogus.permission")]
    [InlineData("")]
    [InlineData("not.a.real.scope")]
    public void MapScopeToPermission_UnknownScope_ReturnsNull(string scope)
    {
        string? result = ScopePermissionMapper.MapScopeToPermission(scope);

        result.Should().BeNull();
    }
}
