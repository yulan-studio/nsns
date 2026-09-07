using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using nsns_waiver.Models;
using nsns_waiver.Repositories;

namespace nsns_waiver.Pages.Admin;

/// <summary>
/// Displays the protected, sortable, paginated waiver-submission list.
/// </summary>
[Authorize]
public sealed class SubmissionsModel : PageModel
{
    private const int PageSize = 20;
    private const int MaximumSubmissions = 200;

    private readonly IAdminSubmissionRepository _repository;

    /// <summary>
    /// Creates the page with read-only administrator submission access.
    /// </summary>
    public SubmissionsModel(IAdminSubmissionRepository repository)
    {
        _repository = repository;
    }

    public IReadOnlyList<AdminSubmissionListItem> Submissions { get; private set; } = [];
    public string Sort { get; private set; } = "signedAt";
    public string Direction { get; private set; } = "desc";
    public int CurrentPage { get; private set; } = 1;
    public int TotalPages { get; private set; }
    public int TotalSubmissions { get; private set; }

    /// <summary>
    /// Parses sort/page input and loads one page from the 200 newest submissions.
    /// </summary>
    public async Task OnGetAsync(
        string? sort,
        string? direction,
        CancellationToken cancellationToken,
        int page = 1)
    {
        var selectedSort = ParseSort(sort);
        var descending = string.Equals(direction, "desc", StringComparison.OrdinalIgnoreCase);

        Sort = ToQueryValue(selectedSort);
        Direction = descending ? "desc" : "asc";
        TotalSubmissions = await _repository.CountRecentAsync(
            MaximumSubmissions,
            cancellationToken);
        TotalPages = (int)Math.Ceiling(TotalSubmissions / (double)PageSize);
        CurrentPage = Math.Clamp(page, 1, Math.Max(TotalPages, 1));
        Submissions = await _repository.GetRecentAsync(
            selectedSort,
            descending,
            (CurrentPage - 1) * PageSize,
            PageSize,
            cancellationToken);
    }

    /// <summary>
    /// Returns the direction a column link should use on its next click.
    /// </summary>
    public string NextDirection(string column) =>
        string.Equals(Sort, column, StringComparison.OrdinalIgnoreCase)
        && Direction == "asc"
            ? "desc"
            : "asc";

    /// <summary>
    /// Returns an arrow for the active sort column, otherwise an empty string.
    /// </summary>
    public string SortIndicator(string column) =>
        string.Equals(Sort, column, StringComparison.OrdinalIgnoreCase)
            ? Direction == "asc" ? " ↑" : " ↓"
            : string.Empty;

    /// <summary>
    /// Deletes the authentication cookie and returns to the login page.
    /// </summary>
    public async Task<IActionResult> OnPostLogoutAsync()
    {
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToPage("/Admin/Login");
    }

    /// <summary>
    /// Converts an untrusted query-string value to an allowed sort enum.
    /// </summary>
    private static AdminSubmissionSort ParseSort(string? sort) =>
        sort?.ToLowerInvariant() switch
        {
            "eventname" => AdminSubmissionSort.EventName,
            "firstname" => AdminSubmissionSort.FirstName,
            "lastname" => AdminSubmissionSort.LastName,
            "email" => AdminSubmissionSort.Email,
            "phone" => AdminSubmissionSort.Phone,
            "signature" => AdminSubmissionSort.SignatureName,
            "signedat" => AdminSubmissionSort.SignedAt,
            _ => AdminSubmissionSort.SignedAt
        };

    /// <summary>
    /// Converts the sort enum back to the canonical query-string value.
    /// </summary>
    private static string ToQueryValue(AdminSubmissionSort sort) =>
        sort switch
        {
            AdminSubmissionSort.EventName => "eventName",
            AdminSubmissionSort.FirstName => "firstName",
            AdminSubmissionSort.LastName => "lastName",
            AdminSubmissionSort.Email => "email",
            AdminSubmissionSort.Phone => "phone",
            AdminSubmissionSort.SignatureName => "signature",
            _ => "signedAt"
        };
}
