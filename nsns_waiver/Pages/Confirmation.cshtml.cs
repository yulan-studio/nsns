using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace nsns_waiver.Pages;

/// <summary>
/// Displays safe confirmation details after a successful form redirect.
/// </summary>
public sealed class ConfirmationModel : PageModel
{
    public string? SubmissionReference { get; private set; }
    public string? EventName { get; private set; }
    public string? SignedAtUtcText { get; private set; }

    /// <summary>
    /// Reads one-time confirmation values and rejects direct or stale page visits.
    /// </summary>
    public IActionResult OnGet()
    {
        SubmissionReference = Convert.ToString(
            TempData[nameof(SubmissionReference)],
            CultureInfo.InvariantCulture);
        EventName = Convert.ToString(
            TempData[nameof(EventName)],
            CultureInfo.InvariantCulture);
        SignedAtUtcText = Convert.ToString(
            TempData[nameof(SignedAtUtcText)],
            CultureInfo.InvariantCulture);

        if (string.IsNullOrWhiteSpace(SubmissionReference)
            || string.IsNullOrWhiteSpace(EventName)
            || string.IsNullOrWhiteSpace(SignedAtUtcText))
        {
            return RedirectToPage("/Index");
        }

        return Page();
    }
}
