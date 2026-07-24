using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace nsns_waiver.Pages;

public sealed class ConfirmationModel : PageModel
{
    [TempData]
    public string? SubmissionReference { get; set; }

    [TempData]
    public string? EventName { get; set; }

    [TempData]
    public string? SignedAtUtcText { get; set; }

    public IActionResult OnGet()
    {
        if (string.IsNullOrWhiteSpace(SubmissionReference)
            || string.IsNullOrWhiteSpace(EventName)
            || string.IsNullOrWhiteSpace(SignedAtUtcText))
        {
            return RedirectToPage("/Index");
        }

        return Page();
    }
}
