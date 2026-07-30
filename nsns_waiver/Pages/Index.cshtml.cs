using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using nsns_waiver.Services;

namespace nsns_waiver.Pages;

/// <summary>
/// Handles display and submission of the public event-waiver form.
/// </summary>
public sealed class IndexModel : PageModel
{
    private readonly IWaiverSubmissionService _submissionService;
    private readonly IWaiverAgreementProvider _agreementProvider;

    /// <summary>
    /// Creates the page with event/submission logic and agreement-file access.
    /// </summary>
    public IndexModel(
        IWaiverSubmissionService submissionService,
        IWaiverAgreementProvider agreementProvider)
    {
        _submissionService = submissionService;
        _agreementProvider = agreementProvider;
    }

    [BindProperty]
    public WaiverInput Input { get; set; } = new();

    public WaiverEventInfo? Event { get; private set; }
    public string AgreementHtml { get; private set; } = string.Empty;
    public bool AgreementIsApproved { get; private set; }

    /// <summary>
    /// Resolves the event query parameter and loads the current waiver agreement.
    /// </summary>
    public async Task OnGetAsync(
        [FromQuery(Name = "event")] string? eventCode,
        CancellationToken cancellationToken)
    {
        Event = _submissionService.FindEvent(eventCode);
        Input.EventCode = Event?.Code ?? eventCode?.Trim() ?? string.Empty;
        await LoadAgreementAsync(cancellationToken);
    }

    /// <summary>
    /// Validates page state, maps form input, submits the waiver, and redirects.
    /// </summary>
    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        Event = _submissionService.FindEvent(Input.EventCode);
        await LoadAgreementAsync(cancellationToken);

        if (Event is null)
        {
            ModelState.AddModelError(
                "Input.EventCode",
                "The selected event is missing or invalid.");
        }

        if (!AgreementIsApproved)
        {
            ModelState.AddModelError(
                string.Empty,
                "Submissions are unavailable until the approved waiver agreement is configured.");
        }

        if (!Input.Agreed)
        {
            ModelState.AddModelError(
                "Input.Agreed",
                "You must agree before submitting.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var userAgent = Request.Headers.UserAgent.ToString();
            if (userAgent.Length > 500)
            {
                userAgent = userAgent[..500];
            }

            var request = new SubmitWaiverRequest
            {
                EventCode = Input.EventCode,
                FirstName = Input.FirstName,
                LastName = Input.LastName,
                WechatName = Input.WechatName,
                Email = Input.Email,
                Phone = Input.Phone,
                SignatureName = Input.SignatureName,
                Agreed = Input.Agreed,
                MediaReleaseAgreed = Input.MediaReleaseAgreed,
                FamilyMembers = Input.FamilyMembers
                    .Select(member => new SubmitWaiverFamilyMember
                    {
                        FirstName = member.FirstName,
                        LastName = member.LastName,
                        Relationship = member.Relationship
                    })
                    .ToArray(),
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = string.IsNullOrWhiteSpace(userAgent) ? null : userAgent
            };

            var result = await _submissionService.SubmitAsync(
                request,
                cancellationToken);

            // TempData carries confirmation details across the redirect without a query string.
            TempData[nameof(ConfirmationModel.SubmissionReference)] =
                result.SubmissionReference;
            TempData[nameof(ConfirmationModel.EventName)] = result.EventName;
            TempData[nameof(ConfirmationModel.SignedAtUtcText)] =
                result.SignedAtUtc.ToString("MMMM d, yyyy 'at' HH:mm:ss 'UTC'");

            return RedirectToPage("/Confirmation");
        }
        catch (WaiverValidationException exception)
        {
            foreach (var error in exception.Errors)
            {
                var field = $"Input.{error.Key}";
                foreach (var message in error.Value)
                {
                    ModelState.AddModelError(field, message);
                }
            }

            return Page();
        }
    }

    /// <summary>
    /// Loads the agreement HTML and whether it is approved for submissions.
    /// </summary>
    private async Task LoadAgreementAsync(CancellationToken cancellationToken)
    {
        var agreement = await _agreementProvider.GetAsync(cancellationToken);
        AgreementHtml = agreement.Html;
        AgreementIsApproved = agreement.IsApproved;
    }

    /// <summary>
    /// Represents fields posted by the main waiver form.
    /// </summary>
    public sealed class WaiverInput
    {
        [Required]
        public string EventCode { get; set; } = string.Empty;

        [Required, StringLength(100)]
        [Display(Name = "First name")]
        public string FirstName { get; set; } = string.Empty;

        [Required, StringLength(100)]
        [Display(Name = "Last name")]
        public string LastName { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "WeChat name")]
        public string? WechatName { get; set; }

        [Required, StringLength(320), EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, StringLength(40)]
        [Display(Name = "Phone number")]
        public string Phone { get; set; } = string.Empty;

        [Required, StringLength(200)]
        [Display(Name = "Electronic signature")]
        public string SignatureName { get; set; } = string.Empty;

        public bool Agreed { get; set; }

        [Display(Name = "Media release")]
        public bool MediaReleaseAgreed { get; set; } = true;

        public List<FamilyMemberInput> FamilyMembers { get; set; } = [];
    }

    /// <summary>
    /// Represents one repeatable family-member section in the form.
    /// </summary>
    public sealed class FamilyMemberInput
    {
        [Required, StringLength(100)]
        [Display(Name = "First name")]
        public string FirstName { get; set; } = string.Empty;

        [Required, StringLength(100)]
        [Display(Name = "Last name")]
        public string LastName { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Relationship { get; set; }
    }
}
