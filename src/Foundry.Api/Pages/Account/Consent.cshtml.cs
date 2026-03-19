using System.Security.Claims;
using Foundry.Identity.Domain.Entities;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Foundry.Api.Pages.Account;

[Authorize]
public class ConsentModel : PageModel
{
    private readonly UserManager<FoundryUser> _userManager;
    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly IOpenIddictAuthorizationManager _authorizationManager;
    private readonly IOpenIddictScopeManager _scopeManager;

    public ConsentModel(
        UserManager<FoundryUser> userManager,
        IOpenIddictApplicationManager applicationManager,
        IOpenIddictAuthorizationManager authorizationManager,
        IOpenIddictScopeManager scopeManager)
    {
        _userManager = userManager;
        _applicationManager = applicationManager;
        _authorizationManager = authorizationManager;
        _scopeManager = scopeManager;
    }

    public string ApplicationName { get; set; } = string.Empty;

    public List<ScopeViewModel> RequestedScopes { get; set; } = [];

    [TempData]
    public string? ErrorMessage { get; set; }

    private static readonly Dictionary<string, (string DisplayName, string Description)> ScopeDescriptions = new()
    {
        [Scopes.OpenId] = ("Verify your identity", "Confirm who you are"),
        [Scopes.Profile] = ("Access your profile", "Read your name and profile information"),
        [Scopes.Email] = ("Access your email", "Read your email address"),
        [Scopes.Roles] = ("Access your roles", "Read your assigned roles"),
        [Scopes.OfflineAccess] = ("Stay signed in", "Maintain access when you are not actively using the application"),
    };

    public async Task<IActionResult> OnGetAsync()
    {
        OpenIddictRequest? request = HttpContext.GetOpenIddictServerRequest();
        if (request is null)
        {
            return BadRequest("Unable to retrieve the OpenID Connect request.");
        }

        object? application = await _applicationManager.FindByClientIdAsync(request.ClientId!);
        if (application is null)
        {
            return BadRequest("The specified client application could not be found.");
        }

        ApplicationName = await _applicationManager.GetDisplayNameAsync(application) ?? request.ClientId!;

        ImmutableArray<string> scopes = request.GetScopes();
        RequestedScopes = [];

        foreach (string scope in scopes)
        {
            if (ScopeDescriptions.TryGetValue(scope, out (string DisplayName, string Description) info))
            {
                RequestedScopes.Add(new ScopeViewModel { Name = scope, DisplayName = info.DisplayName, Description = info.Description });
            }
            else
            {
                RequestedScopes.Add(new ScopeViewModel { Name = scope, DisplayName = scope, Description = scope });
            }
        }

        return Page();
    }

    public async Task<IActionResult> OnPostGrantAsync()
    {
        OpenIddictRequest? request = HttpContext.GetOpenIddictServerRequest();
        if (request is null)
        {
            return BadRequest("Unable to retrieve the OpenID Connect request.");
        }

        FoundryUser? user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return BadRequest("Unable to retrieve the user.");
        }

        object? application = await _applicationManager.FindByClientIdAsync(request.ClientId!);
        if (application is null)
        {
            return BadRequest("The specified client application could not be found.");
        }

        string? applicationId = await _applicationManager.GetIdAsync(application);
        string userId = user.Id.ToString();
        ImmutableArray<string> scopes = request.GetScopes();

        // Look for an existing permanent authorization or create one.
        object? authorization = null;

        await foreach (object? existing in _authorizationManager.FindAsync(
            subject: userId,
            client: applicationId!,
            status: Statuses.Valid,
            type: AuthorizationTypes.Permanent,
            scopes: scopes))
        {
            authorization = existing;
            break;
        }

        authorization ??= await _authorizationManager.CreateAsync(
            identity: new ClaimsIdentity(),
            subject: userId,
            client: applicationId!,
            type: AuthorizationTypes.Permanent,
            scopes: scopes);

        string? authorizationId = await _authorizationManager.GetIdAsync(authorization);

        ClaimsIdentity identity = new(
            authenticationType: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            nameType: Claims.Name,
            roleType: Claims.Role);

        identity.SetClaim(Claims.Subject, userId);
        identity.SetClaim(Claims.Name, user.DisplayName);
        identity.SetClaim(Claims.Email, user.Email);

        identity.SetScopes(scopes);
        identity.SetDestinations(GetDestinations);

        identity.SetClaim("oi_au_id", authorizationId);

        ClaimsPrincipal principal = new(identity);

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    public IActionResult OnPostDeny()
    {
        return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private static IEnumerable<string> GetDestinations(Claim claim)
    {
        switch (claim.Type)
        {
            case Claims.Name or Claims.Email:
                yield return Destinations.AccessToken;
                if (claim.Subject?.HasScope(Scopes.Profile) is true ||
                    (claim.Type == Claims.Email && claim.Subject?.HasScope(Scopes.Email) is true))
                {
                    yield return Destinations.IdentityToken;
                }
                break;

            case Claims.Subject:
                yield return Destinations.AccessToken;
                yield return Destinations.IdentityToken;
                break;

            default:
                yield return Destinations.AccessToken;
                break;
        }
    }

    public class ScopeViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
