using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using nsns_waiver.Pages;
using nsns_waiver.Services;

namespace nsns_waiver.Tests;

public sealed class IndexPageTests
{
    [Fact]
    public async Task OnGet_LoadsConfiguredEventAndAgreement()
    {
        var service = new FakeSubmissionService();
        var page = CreatePage(service, isAgreementApproved: true);

        await page.OnGetAsync(" Summer-Camp-2026 ", CancellationToken.None);

        Assert.Equal("summer-camp-2026", page.Event?.Code);
        Assert.Equal("Summer Camp 2026", page.Event?.Name);
        Assert.Equal("summer-camp-2026", page.Input.EventCode);
        Assert.True(page.AgreementIsApproved);
        Assert.True(page.Input.MediaReleaseAgreed);
    }

    [Fact]
    public async Task OnPost_MapsInputAndRedirectsToConfirmation()
    {
        var service = new FakeSubmissionService();
        var page = CreatePage(service, isAgreementApproved: true);
        page.Input = CreateValidInput();

        var result = await page.OnPostAsync(CancellationToken.None);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Confirmation", redirect.PageName);
        Assert.Equal("abc-123", page.TempData[nameof(ConfirmationModel.SubmissionReference)]);
        Assert.NotNull(service.Request);
        Assert.Equal("summer-camp-2026", service.Request.EventCode);
        Assert.True(service.Request.MediaReleaseAgreed);
        Assert.Single(service.Request.FamilyMembers);
        Assert.Equal("127.0.0.1", service.Request.IpAddress);
    }

    [Fact]
    public async Task OnPost_DoesNotSubmitWhenAgreementIsPlaceholder()
    {
        var service = new FakeSubmissionService();
        var page = CreatePage(service, isAgreementApproved: false);
        page.Input = CreateValidInput();

        var result = await page.OnPostAsync(CancellationToken.None);

        Assert.IsType<PageResult>(result);
        Assert.False(page.ModelState.IsValid);
        Assert.Null(service.Request);
    }

    [Fact]
    public async Task OnPost_RejectsSubmissionWhenAgreementIsNotChecked()
    {
        var service = new FakeSubmissionService();
        var page = CreatePage(service, isAgreementApproved: true);
        page.Input = CreateValidInput();
        page.Input.Agreed = false;

        var result = await page.OnPostAsync(CancellationToken.None);

        Assert.IsType<PageResult>(result);
        Assert.Contains("Input.Agreed", page.ModelState.Keys);
        Assert.Null(service.Request);
    }

    [Fact]
    public async Task OnPost_AddsServiceValidationErrorsToModelState()
    {
        var service = new FakeSubmissionService
        {
            Exception = new WaiverValidationException(
                new Dictionary<string, string[]>
                {
                    ["Email"] = ["Email must be valid."]
                })
        };
        var page = CreatePage(service, isAgreementApproved: true);
        page.Input = CreateValidInput();

        var result = await page.OnPostAsync(CancellationToken.None);

        Assert.IsType<PageResult>(result);
        Assert.Contains("Input.Email", page.ModelState.Keys);
    }

    private static IndexModel CreatePage(
        FakeSubmissionService service,
        bool isAgreementApproved)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress =
            System.Net.IPAddress.Parse("127.0.0.1");
        httpContext.Request.Headers.UserAgent = "Unit Test Browser";

        var page = new IndexModel(
            service,
            new FakeAgreementProvider(isAgreementApproved))
        {
            PageContext = new PageContext
            {
                HttpContext = httpContext
            },
            TempData = new TempDataDictionary(
                httpContext,
                new MemoryTempDataProvider())
        };

        return page;
    }

    private static IndexModel.WaiverInput CreateValidInput() =>
        new()
        {
            EventCode = "summer-camp-2026",
            FirstName = "Test",
            LastName = "Customer",
            Email = "test@example.com",
            Phone = "4165550123",
            SignatureName = "Test Customer",
            Agreed = true,
            MediaReleaseAgreed = true,
            FamilyMembers =
            [
                new IndexModel.FamilyMemberInput
                {
                    FirstName = "Family",
                    LastName = "Member"
                }
            ]
        };

    private sealed class FakeSubmissionService : IWaiverSubmissionService
    {
        public SubmitWaiverRequest? Request { get; private set; }
        public WaiverValidationException? Exception { get; init; }

        public WaiverEventInfo? FindEvent(string? eventCode) =>
            string.Equals(
                eventCode?.Trim(),
                "summer-camp-2026",
                StringComparison.OrdinalIgnoreCase)
                ? new WaiverEventInfo("summer-camp-2026", "Summer Camp 2026")
                : null;

        public Task<SubmitWaiverResult> SubmitAsync(
            SubmitWaiverRequest request,
            CancellationToken cancellationToken = default)
        {
            Request = request;

            if (Exception is not null)
            {
                throw Exception;
            }

            return Task.FromResult(
                new SubmitWaiverResult(
                    "abc-123",
                    "Summer Camp 2026",
                    new DateTime(2026, 7, 24, 18, 30, 0, DateTimeKind.Utc)));
        }
    }

    private sealed class FakeAgreementProvider(bool isApproved)
        : IWaiverAgreementProvider
    {
        public Task<WaiverAgreementContent> GetAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                new WaiverAgreementContent("<p>Agreement</p>", isApproved));
    }

    private sealed class MemoryTempDataProvider : ITempDataProvider
    {
        private Dictionary<string, object> _values = [];

        public IDictionary<string, object> LoadTempData(HttpContext context) =>
            new Dictionary<string, object>(_values);

        public void SaveTempData(
            HttpContext context,
            IDictionary<string, object> values)
        {
            _values = new Dictionary<string, object>(values);
        }
    }
}
