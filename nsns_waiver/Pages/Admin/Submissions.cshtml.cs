using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using nsns_waiver.Models;
using nsns_waiver.Repositories;

namespace nsns_waiver.Pages.Admin;

[Authorize]
public sealed class SubmissionsModel : PageModel
{
    private readonly IAdminSubmissionRepository _repository;

    public SubmissionsModel(IAdminSubmissionRepository repository)
    {
        _repository = repository;
    }

    public IReadOnlyList<AdminSubmissionListItem> Submissions { get; private set; } = [];
    public string Sort { get; private set; } = "signedAt";
    public string Direction { get; private set; } = "desc";

    public async Task OnGetAsync(
        string? sort,
        string? direction,
        CancellationToken cancellationToken)
    {
        var selectedSort = ParseSort(sort);
        var descending = string.Equals(direction, "desc", StringComparison.OrdinalIgnoreCase);

        Sort = ToQueryValue(selectedSort);
        Direction = descending ? "desc" : "asc";
        Submissions = await _repository.GetRecentAsync(
            selectedSort,
            descending,
            200,
            cancellationToken);
    }

    public string NextDirection(string column) =>
        string.Equals(Sort, column, StringComparison.OrdinalIgnoreCase)
        && Direction == "asc"
            ? "desc"
            : "asc";

    public string SortIndicator(string column) =>
        string.Equals(Sort, column, StringComparison.OrdinalIgnoreCase)
            ? Direction == "asc" ? " ↑" : " ↓"
            : string.Empty;

    public async Task<IActionResult> OnPostLogoutAsync()
    {
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToPage("/Admin/Login");
    }

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
