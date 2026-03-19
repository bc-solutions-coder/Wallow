using Foundry.Identity.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Foundry.Api.Pages.Account;

[AllowAnonymous]
public class LogoutModel : PageModel
{
    private readonly SignInManager<FoundryUser> _signInManager;

    public LogoutModel(SignInManager<FoundryUser> signInManager)
    {
        _signInManager = signInManager;
    }

    [BindProperty(SupportsGet = true)]
    public string? PostLogoutRedirectUri { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await _signInManager.SignOutAsync();

        if (!string.IsNullOrEmpty(PostLogoutRedirectUri))
        {
            return Redirect(PostLogoutRedirectUri);
        }

        return RedirectToPage();
    }
}
