# Authentication Architecture — Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the full authentication & authorization architecture from the spec (`docs/superpowers/specs/2026-03-18-authentication-architecture-design.md`), covering developer self-service DCR, API key durability, security hardening, and permission model completeness.

**Architecture:** Six epics covering independent subsystems. Each epic produces working, testable software. Epics can be executed in parallel where dependencies allow. The existing DCR infrastructure (Epic 1) is already implemented — this plan covers Epics 2–6.

**Tech Stack:** .NET 10, EF Core, Keycloak 26 DCR, Valkey/Redis, FluentValidation, NSubstitute, xUnit

**Spec:** `docs/superpowers/specs/2026-03-18-authentication-architecture-design.md`

---

## Epic Overview

| Epic | Name | Priority | Status | Beads |
|------|------|----------|--------|-------|
| 1 | DCR Infrastructure (Keycloak + Docker) | P1 | **DONE** | foundry-h7u2, foundry-ajiz, foundry-bw9o, foundry-fz9a (all closed) |
| 2 | Developer Self-Service DCR Proxy (`app-*`) | P1 | Not started | foundry-hsef, foundry-i5y6 |
| 3 | API Key Durability (PostgreSQL Persistence) | P1 | Not started | foundry-u49d |
| 4 | API Key Security Hardening | P0–P2 | Not started | foundry-hsp5, foundry-s5hr, foundry-5osr |
| 5 | Permission Model Completeness | P1 | Not started | (new bead needed) |
| 6 | Anonymous Access Removal | P1 | Not started | foundry-oopm |

**Dependency graph:**
```
Epic 5 (scopes completeness) ──► Epic 4 (scope subset validation needs complete scope list)
Epic 2 (app-* prefix) has no hard deps on others
Epic 3 (API key durability) has no hard deps on others
Epic 4 (API key security) depends on Epic 5
Epic 6 (anonymous removal) has no hard deps
```

**Recommended execution order:** Epics 2, 3, 5, 6 in parallel → Epic 4 after Epic 5.

---

## Epic 1: DCR Infrastructure (DONE)

Already implemented in commit `4651eb67`. See `docs/superpowers/plans/2026-03-16-dynamic-client-registration.md` for the original plan. All 8 tasks completed:
- Realm audience scope promoted to default
- `configure-dcr.sh` created and wired into docker-compose
- `TenantId.Platform` sentinel added
- Lazy metadata sync in `ServiceAccountTrackingMiddleware`
- Integration tests and docs

**Remaining gap:** `KeycloakFixture.CreateServiceAccountClientAsync` helper is missing from integration tests. This is a minor test infrastructure issue, not blocking.

---

## Epic 2: Developer Self-Service DCR Proxy (`app-*` Prefix)

**Goal:** Enable third-party developers to register their own OAuth2 clients via a Foundry proxy endpoint that enforces the `app-*` prefix, rate limits, and scope whitelisting.

**Beads:** foundry-hsef (P1), foundry-i5y6 (P2), foundry-pb5t (P1)

### Feature 2.1: Middleware `app-*` Prefix Recognition

Update existing middleware to treat `app-*` clients as scope-based (same as `sa-*`), but with restricted trust level.

#### Task 2.1.1: PermissionExpansionMiddleware — recognize `app-*` prefix

**Files:**
- Modify: `src/Modules/Identity/Foundry.Identity.Infrastructure/Authorization/PermissionExpansionMiddleware.cs:19`
- Test: `tests/Modules/Identity/Foundry.Identity.Tests/Infrastructure/PermissionExpansionMiddlewareTests.cs`

- [ ] **Step 1: Write failing test for `app-*` client scope expansion**

```csharp
[Fact]
public async Task InvokeAsync_AppPrefixClient_ExpandsScopesToPermissions()
{
    // Arrange
    DefaultHttpContext context = CreateAuthenticatedContext(
        azp: "app-cool-viewer",
        scopes: "showcases.read inquiries.read");

    // Act
    await _middleware.InvokeAsync(context);

    // Assert
    List<string> permissions = context.User.FindAll("permission").Select(c => c.Value).ToList();
    permissions.Should().Contain(PermissionType.ShowcasesRead);
    permissions.Should().Contain(PermissionType.InquiriesRead);
}

[Fact]
public async Task InvokeAsync_AppPrefixClient_DoesNotExpandRoles()
{
    // Arrange — app-* client with realm_access roles should NOT use role expansion
    DefaultHttpContext context = CreateAuthenticatedContext(
        azp: "app-cool-viewer",
        scopes: "showcases.read",
        realmRoles: ["admin"]);

    // Act
    await _middleware.InvokeAsync(context);

    // Assert — should only have showcases.read permission, not all admin permissions
    List<string> permissions = context.User.FindAll("permission").Select(c => c.Value).ToList();
    permissions.Should().Contain(PermissionType.ShowcasesRead);
    permissions.Should().NotContain(PermissionType.AdminAccess);
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/Modules/Identity/Foundry.Identity.Tests --filter "PermissionExpansionMiddlewareTests" -v n
```
Expected: FAIL — `app-*` falls through to role expansion, gets zero permissions (no roles).

- [ ] **Step 3: Update `PermissionExpansionMiddleware` to recognize `app-*`**

In `PermissionExpansionMiddleware.cs:19`, change:
```csharp
if (clientId?.StartsWith("sa-", StringComparison.Ordinal) == true)
```
to:
```csharp
if (clientId?.StartsWith("sa-", StringComparison.Ordinal) == true ||
    clientId?.StartsWith("app-", StringComparison.Ordinal) == true)
```

- [ ] **Step 4: Run test to verify it passes**

```bash
dotnet test tests/Modules/Identity/Foundry.Identity.Tests --filter "PermissionExpansionMiddlewareTests" -v n
```

- [ ] **Step 5: Commit**

```bash
git add src/Modules/Identity/Foundry.Identity.Infrastructure/Authorization/PermissionExpansionMiddleware.cs \
        tests/Modules/Identity/Foundry.Identity.Tests/Infrastructure/PermissionExpansionMiddlewareTests.cs
git commit -m "feat(identity): recognize app-* prefix in PermissionExpansionMiddleware

Treats app-* clients the same as sa-* for scope-based permission expansion.
Both prefixes use OAuth2 scope-to-permission mapping rather than role expansion."
```

---

#### Task 2.1.2: ServiceAccountTrackingMiddleware — track `app-*` clients

**Files:**
- Modify: `src/Modules/Identity/Foundry.Identity.Infrastructure/Middleware/ServiceAccountTrackingMiddleware.cs:28`
- Test: `tests/Modules/Identity/Foundry.Identity.Tests/Infrastructure/ServiceAccountTrackingMiddlewareTests.cs`

- [ ] **Step 1: Write failing test for `app-*` tracking**

```csharp
[Fact]
public async Task InvokeAsync_AppPrefixClient_CreatesMetadata()
{
    _repository.GetByKeycloakClientIdAsync("app-cool-viewer", Arg.Any<CancellationToken>())
        .Returns(Task.FromResult<ServiceAccountMetadata?>(null));

    ServiceAccountMetadata? captured = null;
    _repository.When(r => r.Add(Arg.Any<ServiceAccountMetadata>()))
        .Do(call => captured = call.Arg<ServiceAccountMetadata>());

    DefaultHttpContext context = CreateHttpContextWithScopes("app-cool-viewer", 200, "showcases.read");
    ServiceAccountTrackingMiddleware middleware = CreateMiddleware();

    await middleware.InvokeAsync(context);
    await WaitForReceivedCallAsync(
        () => _repository.Received().Add(Arg.Any<ServiceAccountMetadata>()));

    captured.Should().NotBeNull();
    captured!.KeycloakClientId.Should().Be("app-cool-viewer");
    captured.TenantId.Should().Be(TenantId.Platform);
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/Modules/Identity/Foundry.Identity.Tests --filter "ServiceAccountTrackingMiddlewareTests" -v n
```

- [ ] **Step 3: Update condition to include `app-*`**

In `ServiceAccountTrackingMiddleware.cs:28`, change:
```csharp
if (clientId?.StartsWith("sa-", StringComparison.Ordinal) == true)
```
to:
```csharp
if (clientId?.StartsWith("sa-", StringComparison.Ordinal) == true ||
    clientId?.StartsWith("app-", StringComparison.Ordinal) == true)
```

- [ ] **Step 4: Run test to verify it passes**

```bash
dotnet test tests/Modules/Identity/Foundry.Identity.Tests --filter "ServiceAccountTrackingMiddlewareTests" -v n
```

- [ ] **Step 5: Commit**

```bash
git add src/Modules/Identity/Foundry.Identity.Infrastructure/Middleware/ServiceAccountTrackingMiddleware.cs \
        tests/Modules/Identity/Foundry.Identity.Tests/Infrastructure/ServiceAccountTrackingMiddlewareTests.cs
git commit -m "feat(identity): track app-* clients in ServiceAccountTrackingMiddleware

Lazily creates ServiceAccountMetadata for app-* DCR-registered clients,
same as existing sa-* behavior."
```

---

#### Task 2.1.3: TenantResolutionMiddleware — document `app-*` exclusion from X-Tenant-Id override

**Files:**
- Modify: `src/Modules/Identity/Foundry.Identity.Infrastructure/MultiTenancy/TenantResolutionMiddleware.cs`

**Note:** The current `IsServiceAccount()` method only matches `sa-*`, which correctly excludes `app-*` from `X-Tenant-Id` override. This task adds a clarifying comment only.

- [ ] **Step 1: Add clarifying comment to `IsServiceAccount`**

```csharp
/// <summary>
/// Checks if the principal is an OPERATOR service account (sa-* prefix).
/// Developer apps (app-* prefix) are intentionally excluded — they cannot
/// override tenant context via X-Tenant-Id header.
/// </summary>
private static bool IsServiceAccount(ClaimsPrincipal user)
```

- [ ] **Step 2: Commit**

```bash
git add src/Modules/Identity/Foundry.Identity.Infrastructure/MultiTenancy/TenantResolutionMiddleware.cs
git commit -m "docs(identity): clarify app-* exclusion from X-Tenant-Id override"
```

---

### Feature 2.2: Developer App Registration Proxy

New endpoint: `POST /api/v1/identity/apps/register` — validates `app-` prefix, rate-limits, whitelists scopes, forwards to Keycloak DCR.

#### Task 2.2.1: Define developer app scope whitelist

**Files:**
- Modify: `src/Modules/Identity/Foundry.Identity.Application/Constants/ApiScopes.cs`

- [ ] **Step 1: Write failing test**

```csharp
[Fact]
public void DeveloperAppScopes_ContainsExpectedScopes()
{
    ApiScopes.DeveloperAppScopes.Should().Contain("showcases.read");
    ApiScopes.DeveloperAppScopes.Should().Contain("inquiries.read");
    ApiScopes.DeveloperAppScopes.Should().Contain("inquiries.write");
    ApiScopes.DeveloperAppScopes.Should().NotContain("billing.manage");
    ApiScopes.DeveloperAppScopes.Should().NotContain("users.manage");
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/Modules/Identity/Foundry.Identity.Tests --filter "DeveloperAppScopes" -v n
```

- [ ] **Step 3: Add `DeveloperAppScopes` to `ApiScopes.cs`**

```csharp
/// <summary>
/// Scopes that developer-registered apps (app-* prefix) can request.
/// Restricted to safe, read-heavy operations. No admin/billing/identity management.
/// </summary>
public static readonly IReadOnlySet<string> DeveloperAppScopes = new HashSet<string>
{
    "showcases.read",
    "inquiries.read",
    "inquiries.write",
    "announcements.read",
    "storage.read"
};
```

- [ ] **Step 4: Run test to verify it passes**

- [ ] **Step 5: Commit**

```bash
git add src/Modules/Identity/Foundry.Identity.Application/Constants/ApiScopes.cs \
        tests/Modules/Identity/Foundry.Identity.Tests/Application/ApiScopesTests.cs
git commit -m "feat(identity): add DeveloperAppScopes whitelist for app-* DCR registration"
```

---

#### Task 2.2.2: Create `IDeveloperAppService` interface and Keycloak implementation

**Files:**
- Create: `src/Modules/Identity/Foundry.Identity.Application/Interfaces/IDeveloperAppService.cs`
- Create: `src/Modules/Identity/Foundry.Identity.Infrastructure/Services/KeycloakDeveloperAppService.cs`

- [ ] **Step 1: Define the interface**

```csharp
namespace Foundry.Identity.Application.Interfaces;

public interface IDeveloperAppService
{
    Task<DeveloperAppRegistrationResult> RegisterAppAsync(
        string clientId,
        string clientName,
        IReadOnlyList<string> requestedScopes,
        string developerAccessToken,
        CancellationToken ct = default);
}

public sealed record DeveloperAppRegistrationResult(
    bool Success,
    string? ClientId,
    string? ClientSecret,
    string? RegistrationAccessToken,
    string? Error);
```

- [ ] **Step 2: Write failing test for Keycloak implementation**

```csharp
[Fact]
public async Task RegisterAppAsync_ValidRequest_ForwardsToDcrEndpoint()
{
    // Arrange
    _mockHttpHandler.SetupResponse(HttpStatusCode.Created, new
    {
        client_id = "app-test-app",
        client_secret = "generated-secret",
        registration_access_token = "reg-token"
    });

    // Act
    DeveloperAppRegistrationResult result = await _service.RegisterAppAsync(
        "app-test-app", "Test App", ["showcases.read"], "dev-jwt-token");

    // Assert
    result.Success.Should().BeTrue();
    result.ClientId.Should().Be("app-test-app");
    result.ClientSecret.Should().Be("generated-secret");
}
```

- [ ] **Step 3: Implement `KeycloakDeveloperAppService`**

```csharp
namespace Foundry.Identity.Infrastructure.Services;

public sealed class KeycloakDeveloperAppService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<KeycloakDeveloperAppService> logger) : IDeveloperAppService
{
    public async Task<DeveloperAppRegistrationResult> RegisterAppAsync(
        string clientId,
        string clientName,
        IReadOnlyList<string> requestedScopes,
        string developerAccessToken,
        CancellationToken ct = default)
    {
        string keycloakUrl = configuration["Authentication:Authority"]
            ?? throw new InvalidOperationException("Authentication:Authority not configured");
        string dcrEndpoint = $"{keycloakUrl}/clients-registrations/openid-connect";

        HttpClient http = httpClientFactory.CreateClient("keycloak-dcr");
        http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", developerAccessToken);

        object payload = new
        {
            client_id = clientId,
            client_name = clientName,
            grant_types = new[] { "client_credentials" },
            token_endpoint_auth_method = "client_secret_basic",
            default_scopes = requestedScopes
        };

        HttpResponseMessage response = await http.PostAsJsonAsync(dcrEndpoint, payload, ct);

        if (!response.IsSuccessStatusCode)
        {
            string error = await response.Content.ReadAsStringAsync(ct);
            logger.LogWarning("DCR registration failed for {ClientId}: {StatusCode} {Error}",
                clientId, response.StatusCode, error);
            return new DeveloperAppRegistrationResult(false, null, null, null,
                $"Keycloak DCR failed: {response.StatusCode}");
        }

        JsonDocument doc = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        JsonElement root = doc.RootElement;

        return new DeveloperAppRegistrationResult(
            true,
            root.GetProperty("client_id").GetString(),
            root.GetProperty("client_secret").GetString(),
            root.GetProperty("registration_access_token").GetString(),
            null);
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

- [ ] **Step 5: Register in DI**

Add to `IdentityModuleExtensions.cs`:
```csharp
services.AddScoped<IDeveloperAppService, KeycloakDeveloperAppService>();
```

- [ ] **Step 6: Commit**

```bash
git add src/Modules/Identity/Foundry.Identity.Application/Interfaces/IDeveloperAppService.cs \
        src/Modules/Identity/Foundry.Identity.Infrastructure/Services/KeycloakDeveloperAppService.cs \
        tests/Modules/Identity/Foundry.Identity.Tests/Infrastructure/KeycloakDeveloperAppServiceTests.cs
git commit -m "feat(identity): add KeycloakDeveloperAppService for app-* DCR proxy"
```

---

#### Task 2.2.3: Create `AppsController` with registration endpoint

**Files:**
- Create: `src/Modules/Identity/Foundry.Identity.Api/Controllers/AppsController.cs`
- Create: `src/Modules/Identity/Foundry.Identity.Api/Contracts/Requests/RegisterDeveloperAppRequest.cs`
- Create: `src/Modules/Identity/Foundry.Identity.Api/Contracts/Responses/DeveloperAppRegisteredResponse.cs`

- [ ] **Step 1: Create request/response contracts**

```csharp
namespace Foundry.Identity.Api.Contracts.Requests;

public sealed record RegisterDeveloperAppRequest(
    string ClientId,
    string ClientName,
    List<string> RequestedScopes);

namespace Foundry.Identity.Api.Contracts.Responses;

public sealed record DeveloperAppRegisteredResponse(
    string ClientId,
    string ClientSecret,
    string RegistrationAccessToken);
```

- [ ] **Step 2: Write failing controller test**

```csharp
[Fact]
public async Task Register_ValidRequest_Returns201()
{
    // Arrange
    _developerAppService.RegisterAppAsync(
        "app-test", "Test", Arg.Any<IReadOnlyList<string>>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
        .Returns(new DeveloperAppRegistrationResult(true, "app-test", "secret", "reg-token", null));

    RegisterDeveloperAppRequest request = new("app-test", "Test", ["showcases.read"]);

    // Act
    IActionResult result = await _controller.Register(request, CancellationToken.None);

    // Assert
    CreatedResult created = result.Should().BeOfType<CreatedResult>().Subject;
    created.StatusCode.Should().Be(201);
}

[Fact]
public async Task Register_MissingAppPrefix_Returns400()
{
    RegisterDeveloperAppRequest request = new("bad-name", "Test", ["showcases.read"]);

    IActionResult result = await _controller.Register(request, CancellationToken.None);

    ObjectResult obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(400);
}

[Fact]
public async Task Register_DisallowedScopes_Returns400()
{
    RegisterDeveloperAppRequest request = new("app-evil", "Evil", ["billing.manage"]);

    IActionResult result = await _controller.Register(request, CancellationToken.None);

    ObjectResult obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(400);
}
```

- [ ] **Step 3: Implement `AppsController`**

```csharp
namespace Foundry.Identity.Api.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/identity/apps")]
[Authorize]
public sealed class AppsController(
    IDeveloperAppService developerAppService,
    ICurrentUserService currentUserService) : ControllerBase
{
    [HttpPost("register")]
    [HasPermission(PermissionType.ApiKeyManage)] // reuse — developer needs basic API access
    [ProducesResponseType(typeof(DeveloperAppRegisteredResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterDeveloperAppRequest request,
        CancellationToken ct)
    {
        // Validate app-* prefix
        if (!request.ClientId.StartsWith("app-", StringComparison.Ordinal))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid client ID",
                Detail = "Developer app client IDs must start with 'app-'",
                Status = StatusCodes.Status400BadRequest
            });
        }

        // Validate scopes against whitelist
        List<string> disallowed = request.RequestedScopes
            .Where(s => !ApiScopes.DeveloperAppScopes.Contains(s))
            .ToList();

        if (disallowed.Count > 0)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid scopes",
                Detail = $"The following scopes are not allowed for developer apps: {string.Join(", ", disallowed)}",
                Status = StatusCodes.Status400BadRequest
            });
        }

        // Extract the caller's JWT to forward to Keycloak
        string? accessToken = HttpContext.Request.Headers.Authorization
            .FirstOrDefault()?.Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase);

        if (string.IsNullOrEmpty(accessToken))
        {
            return Problem(statusCode: 401, title: "Unauthorized");
        }

        DeveloperAppRegistrationResult result = await developerAppService.RegisterAppAsync(
            request.ClientId, request.ClientName, request.RequestedScopes, accessToken, ct);

        if (!result.Success)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Registration failed",
                Detail = result.Error,
                Status = StatusCodes.Status400BadRequest
            });
        }

        return Created($"/api/v1/identity/apps/{result.ClientId}", new DeveloperAppRegisteredResponse(
            result.ClientId!, result.ClientSecret!, result.RegistrationAccessToken!));
    }
}
```

- [ ] **Step 4: Run tests**

```bash
dotnet test tests/Modules/Identity/Foundry.Identity.Tests --filter "AppsControllerTests" -v n
```

- [ ] **Step 5: Commit**

```bash
git add src/Modules/Identity/Foundry.Identity.Api/Controllers/AppsController.cs \
        src/Modules/Identity/Foundry.Identity.Api/Contracts/Requests/RegisterDeveloperAppRequest.cs \
        src/Modules/Identity/Foundry.Identity.Api/Contracts/Responses/DeveloperAppRegisteredResponse.cs \
        tests/Modules/Identity/Foundry.Identity.Tests/Api/AppsControllerTests.cs
git commit -m "feat(identity): add POST /api/v1/identity/apps/register for developer DCR proxy

Validates app-* prefix, whitelists scopes, and forwards to Keycloak DCR.
Closes foundry-hsef."
```

---

#### Task 2.2.4: Add rate limiting for app registration

**Files:**
- Modify: `src/Foundry.Api/Extensions/RateLimitDefaults.cs` (or equivalent rate limit setup)
- Modify: `AppsController.cs` — apply `[EnableRateLimiting("app-registration")]`

- [ ] **Step 1: Add `app-registration` rate limit policy**

```csharp
// In rate limit configuration
options.AddFixedWindowLimiter("app-registration", limiter =>
{
    limiter.PermitLimit = 5;
    limiter.Window = TimeSpan.FromHours(1);
    limiter.QueueLimit = 0;
});
```

- [ ] **Step 2: Apply to controller**

```csharp
[EnableRateLimiting("app-registration")]
[HttpPost("register")]
```

- [ ] **Step 3: Commit**

```bash
git add src/Foundry.Api/Extensions/RateLimitDefaults.cs \
        src/Modules/Identity/Foundry.Identity.Api/Controllers/AppsController.cs
git commit -m "feat(identity): rate-limit app registration to 5/hr per user

Closes foundry-i5y6."
```

---

## Epic 3: API Key Durability (PostgreSQL Persistence)

**Goal:** Add PostgreSQL as durable storage for API keys with Valkey as cache. Keys must survive Valkey restarts.

**Bead:** foundry-u49d (P1)

### Feature 3.1: API Key Domain Entity

#### Task 3.1.1: Create `ApiKeyId` strongly-typed ID

**Files:**
- Create: `src/Modules/Identity/Foundry.Identity.Domain/Identity/ApiKeyId.cs`

- [ ] **Step 1: Write failing test**

```csharp
[Fact]
public void ApiKeyId_New_GeneratesUniqueIds()
{
    ApiKeyId id1 = ApiKeyId.New();
    ApiKeyId id2 = ApiKeyId.New();
    id1.Should().NotBe(id2);
}

[Fact]
public void ApiKeyId_Create_RoundTrips()
{
    Guid guid = Guid.NewGuid();
    ApiKeyId id = ApiKeyId.Create(guid);
    id.Value.Should().Be(guid);
}
```

- [ ] **Step 2: Implement**

```csharp
namespace Foundry.Identity.Domain.Identity;

public readonly record struct ApiKeyId(Guid Value) : IStronglyTypedId<ApiKeyId>
{
    public static ApiKeyId Create(Guid value) => new(value);
    public static ApiKeyId New() => new(Guid.NewGuid());
}
```

- [ ] **Step 3: Run tests, commit**

```bash
git add src/Modules/Identity/Foundry.Identity.Domain/Identity/ApiKeyId.cs \
        tests/Modules/Identity/Foundry.Identity.Domain.Tests/Identity/ApiKeyIdTests.cs
git commit -m "feat(identity): add ApiKeyId strongly-typed ID"
```

---

#### Task 3.1.2: Create `ApiKey` domain entity

**Files:**
- Create: `src/Modules/Identity/Foundry.Identity.Domain/Entities/ApiKey.cs`

- [ ] **Step 1: Write failing tests**

```csharp
[Fact]
public void Create_ValidInput_CreatesApiKey()
{
    ApiKey key = ApiKey.Create(
        TenantId.New(), "test-key-id", "Test Key", "sk_live_abc12345",
        "hash123", ["showcases.read"], Guid.NewGuid(),
        null, TimeProvider.System);

    key.Name.Should().Be("Test Key");
    key.KeyId.Should().Be("test-key-id");
    key.Prefix.Should().Be("sk_live_abc12345");
    key.Scopes.Should().Contain("showcases.read");
    key.IsExpired.Should().BeFalse();
}

[Fact]
public void Create_WithExpiration_SetsExpiresAt()
{
    DateTimeOffset expiry = DateTimeOffset.UtcNow.AddDays(30);
    ApiKey key = ApiKey.Create(
        TenantId.New(), "key-id", "Key", "prefix",
        "hash", [], Guid.NewGuid(), expiry, TimeProvider.System);

    key.ExpiresAt.Should().Be(expiry);
}

[Fact]
public void Revoke_ActiveKey_SetsRevokedAt()
{
    ApiKey key = ApiKey.Create(
        TenantId.New(), "key-id", "Key", "prefix",
        "hash", [], Guid.NewGuid(), null, TimeProvider.System);

    Guid userId = Guid.NewGuid();
    key.Revoke(userId, TimeProvider.System);

    key.RevokedAt.Should().NotBeNull();
}

[Fact]
public void IsExpired_ExpiredKey_ReturnsTrue()
{
    FakeTimeProvider time = new(DateTimeOffset.UtcNow);
    ApiKey key = ApiKey.Create(
        TenantId.New(), "key-id", "Key", "prefix",
        "hash", [], Guid.NewGuid(),
        DateTimeOffset.UtcNow.AddDays(-1), time);

    key.IsExpired(time).Should().BeTrue();
}
```

- [ ] **Step 2: Implement `ApiKey` entity**

```csharp
namespace Foundry.Identity.Domain.Entities;

public sealed class ApiKey : AuditableEntity<ApiKeyId>, ITenantScoped
{
    public TenantId TenantId { get; init; }
    public string KeyId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Prefix { get; private set; } = string.Empty;
    public string KeyHash { get; private set; } = string.Empty;
    public Guid UserId { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public DateTimeOffset? LastUsedAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }

    private readonly List<string> _scopes = [];
    public IReadOnlyList<string> Scopes => _scopes.AsReadOnly();

    public bool IsRevoked => RevokedAt.HasValue;

    public bool IsExpired(TimeProvider timeProvider)
        => ExpiresAt.HasValue && ExpiresAt.Value < timeProvider.GetUtcNow();

    public bool IsActive(TimeProvider timeProvider)
        => !IsRevoked && !IsExpired(timeProvider);

    private ApiKey() { } // EF Core

    public static ApiKey Create(
        TenantId tenantId, string keyId, string name, string prefix,
        string keyHash, IEnumerable<string> scopes, Guid userId,
        DateTimeOffset? expiresAt, TimeProvider timeProvider)
    {
        ApiKey key = new()
        {
            Id = ApiKeyId.New(),
            TenantId = tenantId,
            KeyId = keyId,
            Name = name,
            Prefix = prefix,
            KeyHash = keyHash,
            UserId = userId,
            ExpiresAt = expiresAt
        };
        key._scopes.AddRange(scopes);
        key.SetCreated(timeProvider.GetUtcNow(), userId);
        return key;
    }

    public void MarkUsed(TimeProvider timeProvider)
    {
        LastUsedAt = timeProvider.GetUtcNow();
    }

    public void Revoke(Guid revokedByUserId, TimeProvider timeProvider)
    {
        if (IsRevoked)
            throw new BusinessRuleException("Identity.ApiKeyAlreadyRevoked", "API key is already revoked");
        RevokedAt = timeProvider.GetUtcNow();
        SetUpdated(timeProvider.GetUtcNow(), revokedByUserId);
    }
}
```

- [ ] **Step 3: Run tests, commit**

```bash
git add src/Modules/Identity/Foundry.Identity.Domain/Entities/ApiKey.cs \
        src/Modules/Identity/Foundry.Identity.Domain/Identity/ApiKeyId.cs \
        tests/Modules/Identity/Foundry.Identity.Domain.Tests/Entities/ApiKeyTests.cs
git commit -m "feat(identity): add ApiKey domain entity with scopes, expiry, and revocation"
```

---

### Feature 3.2: EF Core Configuration and Migration

#### Task 3.2.1: Add `ApiKey` to `IdentityDbContext`

**Files:**
- Modify: `src/Modules/Identity/Foundry.Identity.Infrastructure/Persistence/IdentityDbContext.cs`
- Create: `src/Modules/Identity/Foundry.Identity.Infrastructure/Persistence/Configurations/ApiKeyConfiguration.cs`

- [ ] **Step 1: Create EF configuration**

```csharp
namespace Foundry.Identity.Infrastructure.Persistence.Configurations;

internal sealed class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> builder)
    {
        builder.ToTable("api_keys");
        builder.HasKey(k => k.Id);
        builder.Property(k => k.Id).HasConversion(id => id.Value, v => ApiKeyId.Create(v));
        builder.Property(k => k.TenantId).HasConversion(id => id.Value, v => TenantId.Create(v));
        builder.Property(k => k.KeyId).HasMaxLength(32).IsRequired();
        builder.Property(k => k.Name).HasMaxLength(256).IsRequired();
        builder.Property(k => k.Prefix).HasMaxLength(16).IsRequired();
        builder.Property(k => k.KeyHash).HasMaxLength(64).IsRequired();
        builder.HasIndex(k => k.KeyHash).IsUnique();
        builder.HasIndex(k => new { k.UserId, k.TenantId });
        builder.HasIndex(k => k.KeyId).IsUnique();
        builder.Property(k => k.Scopes).HasColumnType("jsonb");
    }
}
```

- [ ] **Step 2: Add DbSet to `IdentityDbContext`**

```csharp
public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
```

- [ ] **Step 3: Generate EF migration**

```bash
dotnet ef migrations add AddApiKeysTable \
    --project src/Modules/Identity/Foundry.Identity.Infrastructure \
    --startup-project src/Foundry.Api \
    --context IdentityDbContext
```

- [ ] **Step 4: Commit**

```bash
git add src/Modules/Identity/Foundry.Identity.Infrastructure/Persistence/
git commit -m "feat(identity): add ApiKeys table EF Core migration for durable API key storage"
```

---

### Feature 3.3: Dual-Write API Key Service

#### Task 3.3.1: Create `IApiKeyRepository` interface

**Files:**
- Create: `src/Modules/Identity/Foundry.Identity.Application/Interfaces/IApiKeyRepository.cs`

```csharp
namespace Foundry.Identity.Application.Interfaces;

public interface IApiKeyRepository
{
    Task<ApiKey?> GetByKeyHashAsync(string keyHash, CancellationToken ct);
    Task<ApiKey?> GetByKeyIdAsync(string keyId, CancellationToken ct);
    Task<IReadOnlyList<ApiKey>> GetByUserIdAsync(Guid userId, Guid tenantId, CancellationToken ct);
    Task<int> CountByUserIdAsync(Guid userId, Guid tenantId, CancellationToken ct);
    void Add(ApiKey entity);
    Task SaveChangesAsync(CancellationToken ct = default);
}
```

- [ ] **Step 1: Create interface and commit**

```bash
git add src/Modules/Identity/Foundry.Identity.Application/Interfaces/IApiKeyRepository.cs
git commit -m "feat(identity): add IApiKeyRepository interface for durable API key storage"
```

---

#### Task 3.3.2: Implement `ApiKeyRepository`

**Files:**
- Create: `src/Modules/Identity/Foundry.Identity.Infrastructure/Persistence/Repositories/ApiKeyRepository.cs`

- [ ] **Step 1: Implement**

```csharp
namespace Foundry.Identity.Infrastructure.Persistence.Repositories;

internal sealed class ApiKeyRepository(IdentityDbContext context) : IApiKeyRepository
{
    public Task<ApiKey?> GetByKeyHashAsync(string keyHash, CancellationToken ct)
        => context.ApiKeys.FirstOrDefaultAsync(k => k.KeyHash == keyHash && k.RevokedAt == null, ct);

    public Task<ApiKey?> GetByKeyIdAsync(string keyId, CancellationToken ct)
        => context.ApiKeys.FirstOrDefaultAsync(k => k.KeyId == keyId && k.RevokedAt == null, ct);

    public async Task<IReadOnlyList<ApiKey>> GetByUserIdAsync(Guid userId, Guid tenantId, CancellationToken ct)
        => await context.ApiKeys
            .Where(k => k.UserId == userId && k.TenantId == TenantId.Create(tenantId) && k.RevokedAt == null)
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync(ct);

    public Task<int> CountByUserIdAsync(Guid userId, Guid tenantId, CancellationToken ct)
        => context.ApiKeys.CountAsync(k => k.UserId == userId
            && k.TenantId == TenantId.Create(tenantId) && k.RevokedAt == null, ct);

    public void Add(ApiKey entity) => context.ApiKeys.Add(entity);

    public Task SaveChangesAsync(CancellationToken ct) => context.SaveChangesAsync(ct);
}
```

- [ ] **Step 2: Register in DI, commit**

```bash
git add src/Modules/Identity/Foundry.Identity.Infrastructure/Persistence/Repositories/ApiKeyRepository.cs
git commit -m "feat(identity): implement ApiKeyRepository for PostgreSQL API key persistence"
```

---

#### Task 3.3.3: Update `RedisApiKeyService` to dual-write

**Files:**
- Modify: `src/Modules/Identity/Foundry.Identity.Infrastructure/Services/RedisApiKeyService.cs`

This is the most complex task. The service needs to:
1. **Create**: Write to PostgreSQL first (durable), then Valkey (cache)
2. **Validate**: Check Valkey first (fast) → PostgreSQL fallback on miss → repopulate Valkey
3. **List**: Read from PostgreSQL (authoritative)
4. **Revoke**: Delete from PostgreSQL first, then Valkey
5. **UpdateLastUsed**: Update both, use `When.Exists` for Valkey (fixes race condition too)

- [ ] **Step 1: Write tests for dual-write behavior**

Tests should verify:
- Create writes to both PostgreSQL and Valkey
- Validate falls back to PostgreSQL on Valkey miss
- Revoke marks as revoked in PostgreSQL and deletes from Valkey
- List reads from PostgreSQL

- [ ] **Step 2: Add `IApiKeyRepository` constructor parameter**

```csharp
public sealed partial class RedisApiKeyService(
    IConnectionMultiplexer redis,
    IApiKeyRepository apiKeyRepository,
    ILogger<RedisApiKeyService> logger) : IApiKeyService
```

- [ ] **Step 3: Update `CreateApiKeyAsync` to dual-write**

After generating the key and hash, before writing to Valkey:
```csharp
// PostgreSQL first (durable)
ApiKey entity = ApiKey.Create(
    TenantId.Create(tenantId), keyId, name, prefix,
    keyHash, scopes?.ToList() ?? [], userId, expiresAt, TimeProvider.System);
apiKeyRepository.Add(entity);
await apiKeyRepository.SaveChangesAsync(ct);

// Then Valkey (cache) — existing code
```

- [ ] **Step 4: Update `ValidateApiKeyAsync` with PostgreSQL fallback**

After Valkey miss:
```csharp
if (json.IsNullOrEmpty)
{
    // Fallback to PostgreSQL
    ApiKey? dbKey = await apiKeyRepository.GetByKeyHashAsync(keyHash, ct);
    if (dbKey == null || !dbKey.IsActive(TimeProvider.System))
        return new ApiKeyValidationResult(false, null, null, null, null, "API key not found");

    // Repopulate Valkey cache
    // ... serialize and cache ...

    return new ApiKeyValidationResult(true, dbKey.KeyId, dbKey.UserId, dbKey.TenantId.Value, dbKey.Scopes.ToList(), null);
}
```

- [ ] **Step 5: Update `RevokeApiKeyAsync` to use PostgreSQL**

```csharp
// Mark revoked in PostgreSQL (durable)
ApiKey? dbKey = await apiKeyRepository.GetByKeyIdAsync(keyId, ct);
if (dbKey == null || dbKey.UserId != userId) return false;
dbKey.Revoke(userId, TimeProvider.System);
await apiKeyRepository.SaveChangesAsync(ct);

// Then delete from Valkey (cache) — existing code
```

- [ ] **Step 6: Update `ListApiKeysAsync` to use PostgreSQL**

Update `IApiKeyService.ListApiKeysAsync` signature to accept `tenantId`:
```csharp
Task<IReadOnlyList<ApiKeyMetadata>> ListApiKeysAsync(Guid userId, Guid tenantId, CancellationToken ct = default);
```

Then update the implementation:
```csharp
IReadOnlyList<ApiKey> keys = await apiKeyRepository.GetByUserIdAsync(userId, tenantId, ct);
return keys.Select(k => new ApiKeyMetadata(
    k.KeyId, k.Name, k.Prefix, k.UserId, k.TenantId.Value,
    k.Scopes.ToList(), k.CreatedAt, k.ExpiresAt, k.LastUsedAt)).ToList();
```

And update `ApiKeysController.ListApiKeys` to pass `tenantId`:
```csharp
IReadOnlyList<ApiKeyMetadata> keys = await apiKeyService.ListApiKeysAsync(userId.Value, tenantContext.TenantId.Value, ct);
```

- [ ] **Step 7: Run all tests**

```bash
dotnet test tests/Modules/Identity/Foundry.Identity.Tests --filter "RedisApiKeyService" -v n
```

- [ ] **Step 8: Commit**

```bash
git add src/Modules/Identity/Foundry.Identity.Infrastructure/Services/RedisApiKeyService.cs \
        tests/Modules/Identity/Foundry.Identity.Tests/Infrastructure/RedisApiKeyServiceTests.cs
git commit -m "feat(identity): dual-write API keys to PostgreSQL + Valkey cache

PostgreSQL is the durable source of truth. Valkey is a read-through cache.
Keys now survive Valkey restarts. Closes foundry-u49d."
```

---

## Epic 4: API Key Security Hardening

**Goal:** Fix privilege escalation bug, add key limits, and fix revocation race condition.

**Beads:** foundry-hsp5 (P0), foundry-s5hr (P2), foundry-5osr (P2)

**Depends on:** Epic 5 (needs complete `ApiScopes.ValidScopes` for scope subset validation)

### Feature 4.1: Scope-to-Permission Mapper (Shared)

#### Task 4.1.1: Extract `MapScopeToPermission` to a shared static helper

**Files:**
- Create: `src/Modules/Identity/Foundry.Identity.Application/Constants/ScopePermissionMapper.cs`
- Modify: `src/Modules/Identity/Foundry.Identity.Infrastructure/Authorization/PermissionExpansionMiddleware.cs`

- [ ] **Step 1: Write failing test**

```csharp
[Theory]
[InlineData("showcases.read", PermissionType.ShowcasesRead)]
[InlineData("billing.manage", PermissionType.BillingManage)]
[InlineData("unknown.scope", null)]
public void MapScopeToPermission_ReturnsCorrectMapping(string scope, string? expected)
{
    string? result = ScopePermissionMapper.MapScopeToPermission(scope);
    result.Should().Be(expected);
}
```

- [ ] **Step 2: Create `ScopePermissionMapper`**

Extract the switch expression from `PermissionExpansionMiddleware.MapScopeToPermission()` into:

```csharp
namespace Foundry.Identity.Application.Constants;

public static class ScopePermissionMapper
{
    public static string? MapScopeToPermission(string scope)
    {
        // Same switch expression currently in PermissionExpansionMiddleware
        return scope switch { ... };
    }
}
```

- [ ] **Step 3: Update `PermissionExpansionMiddleware` to call `ScopePermissionMapper`**

```csharp
string? permission = ScopePermissionMapper.MapScopeToPermission(scope);
```

- [ ] **Step 4: Run all tests**

```bash
dotnet test tests/Modules/Identity/Foundry.Identity.Tests -v n
```

- [ ] **Step 5: Commit**

```bash
git add src/Modules/Identity/Foundry.Identity.Application/Constants/ScopePermissionMapper.cs \
        src/Modules/Identity/Foundry.Identity.Infrastructure/Authorization/PermissionExpansionMiddleware.cs \
        tests/Modules/Identity/Foundry.Identity.Tests/Application/ScopePermissionMapperTests.cs
git commit -m "refactor(identity): extract ScopePermissionMapper from PermissionExpansionMiddleware

Makes scope-to-permission mapping reusable for API key scope validation."
```

---

### Feature 4.2: Scope Subset Validation (P0 Fix)

#### Task 4.2.1: Add permission subset check to `ApiKeysController.CreateApiKey`

**Files:**
- Modify: `src/Modules/Identity/Foundry.Identity.Api/Controllers/ApiKeysController.cs:78-93`

- [ ] **Step 1: Write failing test**

```csharp
[Fact]
public async Task CreateApiKey_ScopeExceedsUserPermissions_Returns403()
{
    // Arrange — user has only ShowcasesRead permission
    ClaimsPrincipal user = CreateUserWithPermissions(PermissionType.ShowcasesRead, PermissionType.ApiKeyManage);
    _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

    CreateApiKeyRequest request = new("Test Key", ["billing.manage"], null);

    // Act
    IActionResult result = await _controller.CreateApiKey(request, CancellationToken.None);

    // Assert
    ObjectResult obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(403);
}

[Fact]
public async Task CreateApiKey_ScopeWithinUserPermissions_Succeeds()
{
    // Arrange — user has ShowcasesRead
    ClaimsPrincipal user = CreateUserWithPermissions(
        PermissionType.ShowcasesRead, PermissionType.ApiKeyManage);
    _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

    _apiKeyService.CreateApiKeyAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<Guid>(),
        Arg.Any<IEnumerable<string>>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>())
        .Returns(new ApiKeyCreateResult(true, "key-id", "sk_live_...", "sk_live_1234", null));

    CreateApiKeyRequest request = new("Test Key", ["showcases.read"], null);

    // Act
    IActionResult result = await _controller.CreateApiKey(request, CancellationToken.None);

    // Assert
    result.Should().BeOfType<CreatedAtActionResult>();
}
```

- [ ] **Step 2: Add scope subset validation after existing scope validation (line ~93)**

```csharp
// Check that each requested scope maps to a permission the user currently holds
if (request.Scopes is { Count: > 0 })
{
    IReadOnlyCollection<string> userPermissions = HttpContext.User
        .FindAll("permission").Select(c => c.Value).ToHashSet();

    foreach (string scope in request.Scopes)
    {
        string? requiredPermission = ScopePermissionMapper.MapScopeToPermission(scope);
        if (requiredPermission is not null && !userPermissions.Contains(requiredPermission))
        {
            return Problem(
                statusCode: 403,
                title: "Forbidden",
                detail: $"Scope '{scope}' exceeds your current permissions");
        }
    }
}
```

- [ ] **Step 3: Run tests**

```bash
dotnet test tests/Modules/Identity/Foundry.Identity.Tests --filter "ApiKeysController" -v n
```

- [ ] **Step 4: Commit**

```bash
git add src/Modules/Identity/Foundry.Identity.Api/Controllers/ApiKeysController.cs \
        tests/Modules/Identity/Foundry.Identity.Tests/Api/ApiKeysControllerTests.cs
git commit -m "fix(identity): validate API key scopes against user's current permissions

Prevents privilege escalation where a user could create an API key with
scopes their role doesn't grant. Closes foundry-hsp5."
```

---

### Feature 4.3: Max API Key Limit

#### Task 4.3.1: Add configurable key limit to `ApiKeysController`

**Files:**
- Create: `src/Modules/Identity/Foundry.Identity.Application/Options/ApiKeyOptions.cs`
- Modify: `src/Modules/Identity/Foundry.Identity.Api/Controllers/ApiKeysController.cs`

- [ ] **Step 1: Write failing test**

```csharp
[Fact]
public async Task CreateApiKey_AtMaxLimit_Returns403()
{
    // Arrange — user already has 10 keys
    _apiKeyRepository.CountByUserIdAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
        .Returns(10);

    CreateApiKeyRequest request = new("Key 11", ["showcases.read"], null);

    // Act
    IActionResult result = await _controller.CreateApiKey(request, CancellationToken.None);

    // Assert
    ObjectResult obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(403);
}
```

- [ ] **Step 2: Create `ApiKeyOptions`**

```csharp
namespace Foundry.Identity.Application.Options;

public sealed class ApiKeyOptions
{
    public const string SectionName = "ApiKeys";
    public int MaxKeysPerUser { get; set; } = 10;
}
```

- [ ] **Step 3: Add limit check to controller (before creation)**

```csharp
int keyCount = await apiKeyRepository.CountByUserIdAsync(userId.Value, tenantId, ct);
if (keyCount >= apiKeyOptions.Value.MaxKeysPerUser)
{
    return Problem(
        statusCode: 403,
        title: "Forbidden",
        detail: $"Maximum API key limit ({apiKeyOptions.Value.MaxKeysPerUser}) reached. Revoke unused keys before creating new ones.");
}
```

- [ ] **Step 4: Register options in DI**

```csharp
services.Configure<ApiKeyOptions>(configuration.GetSection(ApiKeyOptions.SectionName));
```

- [ ] **Step 5: Run tests, commit**

```bash
git add src/Modules/Identity/Foundry.Identity.Application/Options/ApiKeyOptions.cs \
        src/Modules/Identity/Foundry.Identity.Api/Controllers/ApiKeysController.cs \
        tests/Modules/Identity/Foundry.Identity.Tests/Api/ApiKeysControllerTests.cs
git commit -m "feat(identity): enforce max API key limit per user (default 10)

Configurable via ApiKeys:MaxKeysPerUser in appsettings.json. Closes foundry-s5hr."
```

---

### Feature 4.4: Revocation Race Condition Fix

#### Task 4.4.1: Change `When.Always` to `When.Exists` in `UpdateLastUsedAsync`

**Files:**
- Modify: `src/Modules/Identity/Foundry.Identity.Infrastructure/Services/RedisApiKeyService.cs:273-274`

- [ ] **Step 1: Write failing test**

```csharp
[Fact]
public async Task UpdateLastUsed_AfterRevocation_DoesNotReCreateKey()
{
    // Create a key
    ApiKeyCreateResult created = await _service.CreateApiKeyAsync("Test", userId, tenantId, ["showcases.read"]);

    // Revoke it
    await _service.RevokeApiKeyAsync(created.KeyId!, userId);

    // Simulate concurrent UpdateLastUsed (which would have been fired before revocation)
    // The key should NOT be re-created in Valkey
    ApiKeyValidationResult result = await _service.ValidateApiKeyAsync(created.ApiKey!);
    result.IsValid.Should().BeFalse();
}
```

- [ ] **Step 2: Fix — change `When.Always` to `When.Exists`**

In `RedisApiKeyService.cs` line 273:
```csharp
await db.StringSetAsync($"{KeyPrefix}{keyHash}", json, expiry, keepTtl: false, When.Exists, CommandFlags.None);
await db.StringSetAsync($"{KeyPrefix}id:{data.KeyId}", json, expiry, keepTtl: false, When.Exists, CommandFlags.None);
```

- [ ] **Step 3: Run tests, commit**

```bash
git add src/Modules/Identity/Foundry.Identity.Infrastructure/Services/RedisApiKeyService.cs \
        tests/Modules/Identity/Foundry.Identity.Tests/Infrastructure/RedisApiKeyServiceTests.cs
git commit -m "fix(identity): prevent revoked API key re-creation by UpdateLastUsedAsync

Changes When.Always to When.Exists in Valkey StringSet calls so that
fire-and-forget LastUsedAt updates cannot re-create a just-revoked key.
Closes foundry-5osr."
```

---

## Epic 5: Permission Model Completeness

**Goal:** Sync `ApiScopes.ValidScopes` with `PermissionExpansionMiddleware.MapScopeToPermission()` so all 40 mapped scopes are available for API key creation.

**Bead:** (new — create as part of implementation)

### Feature 5.1: Complete `ApiScopes.ValidScopes`

#### Task 5.1.1: Add 26 missing scopes to `ApiScopes.ValidScopes`

**Files:**
- Modify: `src/Modules/Identity/Foundry.Identity.Application/Constants/ApiScopes.cs`
- Modify: `tests/` — update any assertions on scope count

- [ ] **Step 1: Write failing test**

```csharp
[Fact]
public void ValidScopes_ContainsAllMappedScopes()
{
    // Every scope that PermissionExpansionMiddleware can map should be in ValidScopes
    string[] allMappedScopes =
    [
        "billing.read", "billing.manage",
        "invoices.read", "invoices.write",
        "payments.read", "payments.write",
        "subscriptions.read", "subscriptions.write",
        "users.read", "users.write", "users.manage",
        "roles.read", "roles.write", "roles.manage",
        "organizations.read", "organizations.write", "organizations.manage",
        "apikeys.read", "apikeys.write", "apikeys.manage",
        "sso.read", "sso.manage", "scim.manage",
        "storage.read", "storage.write",
        "messaging.access",
        "announcements.read", "announcements.manage", "changelog.manage",
        "notifications.read", "notifications.write",
        "configuration.read", "configuration.manage",
        "showcases.read", "showcases.manage",
        "inquiries.read", "inquiries.write",
        "serviceaccounts.read", "serviceaccounts.write", "serviceaccounts.manage",
        "webhooks.manage"
    ];

    foreach (string scope in allMappedScopes)
    {
        ApiScopes.ValidScopes.Should().Contain(scope, $"scope '{scope}' is mapped in PermissionExpansionMiddleware but missing from ValidScopes");
    }
}

[Fact]
public void ValidScopes_Count_Is40()
{
    ApiScopes.ValidScopes.Should().HaveCount(40);
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/Modules/Identity/Foundry.Identity.Tests --filter "ValidScopes" -v n
```

- [ ] **Step 3: Update `ApiScopes.ValidScopes`**

```csharp
public static readonly IReadOnlySet<string> ValidScopes = new HashSet<string>
{
    // Billing
    "billing.read", "billing.manage",
    "invoices.read", "invoices.write",
    "payments.read", "payments.write",
    "subscriptions.read", "subscriptions.write",
    // Identity - Users
    "users.read", "users.write", "users.manage",
    // Identity - Roles
    "roles.read", "roles.write", "roles.manage",
    // Identity - Organizations
    "organizations.read", "organizations.write", "organizations.manage",
    // Identity - API Keys
    "apikeys.read", "apikeys.write", "apikeys.manage",
    // Identity - SSO/SCIM
    "sso.read", "sso.manage", "scim.manage",
    // Storage
    "storage.read", "storage.write",
    // Communications
    "messaging.access",
    "announcements.read", "announcements.manage", "changelog.manage",
    "notifications.read", "notifications.write",
    // Configuration
    "configuration.read", "configuration.manage",
    // Showcases
    "showcases.read", "showcases.manage",
    // Inquiries
    "inquiries.read", "inquiries.write",
    // Service Accounts
    "serviceaccounts.read", "serviceaccounts.write", "serviceaccounts.manage",
    // Platform
    "webhooks.manage"
};
```

- [ ] **Step 4: Run tests**

```bash
dotnet test tests/Modules/Identity/Foundry.Identity.Tests --filter "ValidScopes" -v n
```

- [ ] **Step 5: Update `ApiScopeSeeder` to seed all 40 scopes**

Update `src/Modules/Identity/Foundry.Identity.Infrastructure/Data/ApiScopeSeeder.cs` to include all 40 scopes. Update any count assertions in tests from 14 to 40.

- [ ] **Step 6: Generate EF migration for new seed data (if seeder uses EF HasData)**

```bash
dotnet ef migrations add SeedAllApiScopes \
    --project src/Modules/Identity/Foundry.Identity.Infrastructure \
    --startup-project src/Foundry.Api \
    --context IdentityDbContext
```

- [ ] **Step 7: Commit**

```bash
git add src/Modules/Identity/Foundry.Identity.Application/Constants/ApiScopes.cs \
        src/Modules/Identity/Foundry.Identity.Infrastructure/Persistence/ \
        tests/
git commit -m "feat(identity): sync ApiScopes.ValidScopes with all 40 mapped scopes

Adds 26 missing scopes that exist in PermissionExpansionMiddleware but were
absent from ValidScopes, preventing API key creation with those scopes.
Updates ApiScopeSeeder to seed all scopes."
```

---

## Epic 6: Anonymous Access Removal

**Goal:** Remove `[AllowAnonymous]` from `ShowcasesController` read endpoints to enforce the "no anonymous access" security guarantee.

**Bead:** foundry-oopm (P1)

### Feature 6.1: Require Authentication for Showcase Reads

#### Task 6.1.1: Replace `[AllowAnonymous]` with `[HasPermission]` on ShowcasesController

**Files:**
- Modify: `src/Modules/Showcases/Foundry.Showcases.Api/Controllers/ShowcasesController.cs:32,46`

- [ ] **Step 1: Write failing test**

```csharp
[Fact]
public async Task GetAll_Unauthenticated_Returns401()
{
    HttpClient client = _factory.CreateClient();
    HttpResponseMessage response = await client.GetAsync("/api/v1/showcases");
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
}

[Fact]
public async Task GetById_Unauthenticated_Returns401()
{
    HttpClient client = _factory.CreateClient();
    HttpResponseMessage response = await client.GetAsync($"/api/v1/showcases/{Guid.NewGuid()}");
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
}

[Fact]
public async Task GetAll_WithShowcasesReadPermission_Returns200()
{
    HttpClient client = _factory.CreateAuthenticatedClient(PermissionType.ShowcasesRead);
    HttpResponseMessage response = await client.GetAsync("/api/v1/showcases");
    response.StatusCode.Should().Be(HttpStatusCode.OK);
}
```

- [ ] **Step 2: Remove `[AllowAnonymous]` and add `[HasPermission]`**

Line 32 — change:
```csharp
[AllowAnonymous]
```
to:
```csharp
[HasPermission(PermissionType.ShowcasesRead)]
```

Line 46 — same change for `GetById`.

- [ ] **Step 3: Run tests**

```bash
dotnet test tests/Modules/Showcases/ -v n
```

- [ ] **Step 4: Commit**

```bash
git add src/Modules/Showcases/Foundry.Showcases.Api/Controllers/ShowcasesController.cs \
        tests/Modules/Showcases/
git commit -m "fix(showcases): require ShowcasesRead permission for read endpoints

Removes [AllowAnonymous] from GetAll and GetById. Public showcase access
now goes through authenticated service accounts (sa-* or app-*).
Closes foundry-oopm."
```

---

## Beads Mapping

This plan maps directly to beads. Create these as epics and features:

### Epics to Create
| Epic | Title | Description |
|------|-------|-------------|
| E2 | Developer Self-Service DCR Proxy | Enable third-party developers to register app-* OAuth2 clients |
| E3 | API Key PostgreSQL Persistence | Add durable storage for API keys |
| E4 | API Key Security Hardening | Fix privilege escalation, key limits, race condition |
| E5 | Permission Model Completeness | Sync ApiScopes with all mapped scopes |
| E6 | Anonymous Access Removal | Enforce no-anonymous-access security guarantee |

### Existing Beads to Link
| Bead | Epic | Task |
|------|------|------|
| foundry-hsef | E2 | Tasks 2.1.1, 2.1.2, 2.2.3 |
| foundry-i5y6 | E2 | Task 2.2.4 |
| foundry-pb5t | E2 | Task 2.1.3 (already correct, docs only) |
| foundry-u49d | E3 | Tasks 3.1.1–3.3.3 |
| foundry-hsp5 | E4 | Tasks 4.1.1, 4.2.1 |
| foundry-s5hr | E4 | Task 4.3.1 |
| foundry-5osr | E4 | Task 4.4.1 |
| foundry-oopm | E6 | Task 6.1.1 |

### Dependencies
```
E5 (scope completeness) ──blocks──► E4.Feature 4.2 (scope subset validation)
E4.Feature 4.1 (ScopePermissionMapper) ──blocks──► E4.Feature 4.2
All other epics are independent.
```

---

## Task Dependencies Summary

```
Task 2.1.1 (PermissionExpansion app-*) ─┐
Task 2.1.2 (ServiceAccountTracking app-*)├── Feature 2.1 (middleware updates)
Task 2.1.3 (TenantResolution docs)      ─┘

Task 2.2.1 (scope whitelist) ─────────────► Task 2.2.3 (AppsController)
Task 2.2.2 (IDeveloperAppService) ────────► Task 2.2.3 (AppsController)
Task 2.2.3 (AppsController) ──────────────► Task 2.2.4 (rate limiting)

Task 3.1.1 (ApiKeyId) ────────────────────► Task 3.1.2 (ApiKey entity)
Task 3.1.2 (ApiKey entity) ───────────────► Task 3.2.1 (EF config)
Task 3.2.1 (EF config) ──────────────────► Task 3.3.1 (IApiKeyRepository)
Task 3.3.1 (IApiKeyRepository) ───────────► Task 3.3.2 (Repository impl)
Task 3.3.2 (Repository impl) ─────────────► Task 3.3.3 (dual-write service)

Task 4.1.1 (ScopePermissionMapper) ───────► Task 4.2.1 (scope subset validation)
Task 5.1.1 (complete ValidScopes) ────────► Task 4.2.1 (scope subset validation)

Task 4.3.1 (max key limit) — independent
Task 4.4.1 (race condition fix) — independent
Task 6.1.1 (anonymous removal) — independent
```

**Parallelizable execution groups:**
1. **Group A** (parallel, no deps): Tasks 2.1.1–2.1.3, 3.1.1, 4.1.1, 5.1.1, 6.1.1, 4.4.1
2. **Group B** (after Group A deps): Tasks 2.2.1–2.2.2, 3.1.2, 4.2.1, 4.3.1
3. **Group C** (after Group B deps): Tasks 2.2.3, 3.2.1
4. **Group D** (after Group C deps): Tasks 2.2.4, 3.3.1–3.3.3
