using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using nsns_waiver.Services;

namespace nsns_waiver.Pages.Admin;

[AllowAnonymous]
public sealed class LoginModel : PageModel
{
    private readonly IAdminCredentialValidator _credentialValidator;

    public LoginModel(IAdminCredentialValidator credentialValidator)
    {
        _credentialValidator = credentialValidator;
    }

    [BindProperty]
    public LoginInput Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public IActionResult OnGet()
    {
        return User.Identity?.IsAuthenticated == true
            ? RedirectToPage("/Admin/Submissions")
            : Page();
    }

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

        return Url.IsLocalUrl(ReturnUrl)
            ? LocalRedirect(ReturnUrl)
            : RedirectToPage("/Admin/Submissions");
    }

    public sealed class LoginInput
    {
        [Required, StringLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required, StringLength(200), DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}
