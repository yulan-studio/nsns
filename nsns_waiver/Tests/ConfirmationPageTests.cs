using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using nsns_waiver.Pages;

namespace nsns_waiver.Tests;

public sealed class ConfirmationPageTests
{
    [Fact]
    public void OnGet_ConvertsGuidTempDataToReferenceString()
    {
        var reference = Guid.NewGuid();
        var page = CreatePage(
            new Dictionary<string, object>
            {
                [nameof(ConfirmationModel.SubmissionReference)] = reference,
                [nameof(ConfirmationModel.EventName)] = "Summer Camp 2026",
                [nameof(ConfirmationModel.SignedAtUtcText)] =
                    "July 25, 2026 at 18:00:00 UTC"
            });

        var result = page.OnGet();

        Assert.IsType<PageResult>(result);
        Assert.Equal(reference.ToString(), page.SubmissionReference);
        Assert.Equal("Summer Camp 2026", page.EventName);
    }

    [Fact]
    public void OnGet_RedirectsWhenConfirmationDataIsMissing()
    {
        var page = CreatePage(new Dictionary<string, object>());

        var result = page.OnGet();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Index", redirect.PageName);
    }

    private static ConfirmationModel CreatePage(
        IDictionary<string, object> values)
    {
        var httpContext = new DefaultHttpContext();
        var page = new ConfirmationModel
        {
            PageContext = new PageContext
            {
                HttpContext = httpContext
            },
            TempData = new TempDataDictionary(
                httpContext,
                new MemoryTempDataProvider(values))
        };
        return page;
    }

    private sealed class MemoryTempDataProvider(
        IDictionary<string, object> initialValues) : ITempDataProvider
    {
        private readonly Dictionary<string, object> _values = new(initialValues);

        public IDictionary<string, object> LoadTempData(HttpContext context) =>
            new Dictionary<string, object>(_values);

        public void SaveTempData(
            HttpContext context,
            IDictionary<string, object> values)
        {
        }
    }
}
