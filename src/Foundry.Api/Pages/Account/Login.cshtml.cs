using System.ComponentModel.DataAnnotations;
using Foundry.Identity.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Foundry.Api.Pages.Account;

[AllowAnonymous]
public class LoginModel : PageModel
{
    private readonly SignInManager<FoundryUser> _signInManager;

    public LoginModel(SignInManager<FoundryUser> signInManager)
    {
        _signInManager = signInManager;
    }

    [BindProperty]
    public LoginInput Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        Microsoft.AspNetCore.Identity.SignInResult result = await _signInManager.PasswordSignInAsync(
            Input.Email, Input.Password, isPersistent: false, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            return LocalRedirect(ReturnUrl ?? "/");
        }

        if (result.IsLockedOut)
        {
            ErrorMessage = "Account locked out. Please try again later.";
            return Page();
        }

        ErrorMessage = "Invalid email or password.";
        return Page();
    }

#pragma warning disable CA1034 // Razor Page binding model needs to be accessible
    public class LoginInput
#pragma warning restore CA1034
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}
