using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using nsns_waiver.Services;

namespace nsns_waiver.Pages.Admin;

/// <summary>
/// Handles administrator sign-in and creation of the authentication cookie.
/// </summary>
[AllowAnonymous]
public sealed class LoginModel : PageModel
{
    private readonly IAdminCredentialValidator _credentialValidator;

    /// <summary>
    /// Creates the page with the configured credential validator.
    /// </summary>
    public LoginModel(IAdminCredentialValidator credentialValidator)
    {
        _credentialValidator = credentialValidator;
    }

    [BindProperty]
    public LoginInput Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    /// <summary>
    /// Redirects an already authenticated administrator to submissions.
    /// </summary>
    public IActionResult OnGet()
    {
        return User.Identity?.IsAuthenticated == true
            ? RedirectToPage("/Admin/Submissions")
            : Page();
    }

    /// <summary>
    /// Validates credentials, signs in, and redirects only to a local return URL.
    /// </summary>
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (!_credentialValidator.IsValid(Input.Username, Input.Password))
        {
            ModelState.AddModelError(string.Empty, "The username or password is incorrect.");
            return Page();
        }

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, Input.Username)],
            CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        // Local-only validation prevents an attacker from supplying an external redirect.
        return Url.IsLocalUrl(ReturnUrl)
            ? LocalRedirect(ReturnUrl)
            : RedirectToPage("/Admin/Submissions");
    }

    /// <summary>
    /// Represents credentials posted by the login form.
    /// </summary>
    public sealed class LoginInput
    {
        [Required, StringLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required, StringLength(200), DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}
