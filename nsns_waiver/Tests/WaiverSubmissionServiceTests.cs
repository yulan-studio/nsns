using Microsoft.Extensions.Options;
using nsns_waiver.Models;
using nsns_waiver.Options;
using nsns_waiver.Repositories;
using nsns_waiver.Services;
using OptionsFactory = Microsoft.Extensions.Options.Options;

namespace nsns_waiver.Tests;

public sealed class WaiverSubmissionServiceTests
{
    private static readonly DateTimeOffset FixedUtcNow =
        new(2026, 7, 24, 18, 30, 0, TimeSpan.Zero);

    [Fact]
    public async Task SubmitAsync_NormalizesInputAndCreatesTransactionalRecords()
    {
        var repository = new CapturingRepository();
        var service = CreateService(repository);
        var request = CreateValidRequest(
            familyMembers:
            [
                new SubmitWaiverFamilyMember
                {
                    FirstName = " Child ",
                    LastName = " Member ",
                    Relationship = " Daughter "
                }
            ]);

        var result = await service.SubmitAsync(request);

        var submission = Assert.IsType<WaiverSubmission>(repository.Submission);
        Assert.Equal("summer-camp-2026", submission.EventCode);
        Assert.Equal("Summer Camp 2026", submission.EventName);
        Assert.Equal("Customer@Example.com", submission.Email);
        Assert.Equal("customer@example.com", submission.NormalizedEmail);
        Assert.Equal("(416) 555-0123", submission.Phone);
        Assert.Equal("4165550123", submission.NormalizedPhone);
        Assert.Equal(DateTimeKind.Utc, submission.SignedAtUtc.Kind);
        Assert.Equal(FixedUtcNow.UtcDateTime, submission.SignedAtUtc);
        Assert.True(Guid.TryParse(result.SubmissionReference, out _));
        Assert.Equal(submission.SubmissionReference, result.SubmissionReference);

        var member = Assert.Single(repository.FamilyMembers);
        Assert.Equal("Child", member.FirstName);
        Assert.Equal("Member", member.LastName);
        Assert.Equal("Daughter", member.Relationship);

        Assert.Collection(
            repository.OutboxMessages,
            customer =>
            {
                Assert.Equal("CustomerConfirmation", customer.MessageType);
                Assert.Equal("Customer@Example.com", customer.RecipientEmail);
                Assert.Equal(
                    "Waiver received - Summer Camp 2026",
                    customer.Subject);
                Assert.Contains(
                    "Dear Customer Person,",
                    customer.BodyHtml);
                Assert.Contains(
                    "Thank you for submitting your waiver for Summer Camp 2026.",
                    customer.BodyHtml);
                Assert.Contains(
                    "No further action is required at this time.",
                    customer.BodyHtml);
                Assert.Contains("The NorthStar Team", customer.BodyHtml);
                Assert.DoesNotContain(
                    result.SubmissionReference,
                    customer.BodyHtml);
                Assert.DoesNotContain(
                    "2026-07-24 18:30:00 UTC",
                    customer.BodyHtml);
            },
            owner =>
            {
                Assert.Equal("BossNotification", owner.MessageType);
                Assert.Equal("owner@example.com", owner.RecipientEmail);
                Assert.DoesNotContain(
                    result.SubmissionReference,
                    owner.BodyHtml);
                Assert.DoesNotContain(
                    "2026-07-24 18:30:00 UTC",
                    owner.BodyHtml);
                Assert.Contains("Customer Person", owner.BodyHtml);
                Assert.Contains(
                    "<h3><strong><span style=\"background-color: #fff3cd;\">"
                    + "Summer Camp 2026</span></strong></h3>",
                    owner.BodyHtml);
                Assert.Contains("WeChat User", owner.BodyHtml);
                Assert.Contains("Customer@Example.com", owner.BodyHtml);
                Assert.Contains("(416) 555-0123", owner.BodyHtml);
                Assert.Contains("Child Member", owner.BodyHtml);
                Assert.Contains("Daughter", owner.BodyHtml);
                Assert.Contains(
                    "To view this and other waiver submissions",
                    owner.BodyHtml);
                Assert.Contains(
                    "href=\"https://waiver.nsns.ca/Admin/Submissions\"",
                    owner.BodyHtml);
                Assert.Equal(
                    2,
                    owner.BodyHtml.Split(
                        "<hr style=\"border: 0; border-top: 1px solid "
                        + "#b7b7b7; margin: 16px 0;\">",
                        StringSplitOptions.None).Length - 1);
            });
        Assert.Equal(1, repository.CreateCallCount);
    }

    [Fact]
    public async Task SubmitAsync_HtmlEncodesCustomerContentInEmailBodies()
    {
        var repository = new CapturingRepository();
        var service = CreateService(repository);
        var request = CreateValidRequest(
            firstName: "<script>alert(1)</script>",
            familyMembers:
            [
                new SubmitWaiverFamilyMember
                {
                    FirstName = "<b>Child</b>",
                    LastName = "Member",
                    Relationship = "<em>Daughter</em>"
                }
            ]);

        await service.SubmitAsync(request);

        Assert.All(
            repository.OutboxMessages,
            message => Assert.DoesNotContain("<script>", message.BodyHtml));
        Assert.Contains(
            "&lt;script&gt;",
            repository.OutboxMessages.First().BodyHtml);
        var ownerEmail = repository.OutboxMessages.Last().BodyHtml;
        Assert.DoesNotContain("<b>Child</b>", ownerEmail);
        Assert.DoesNotContain("<em>Daughter</em>", ownerEmail);
        Assert.Contains("&lt;b&gt;Child&lt;/b&gt;", ownerEmail);
        Assert.Contains("&lt;em&gt;Daughter&lt;/em&gt;", ownerEmail);
    }

    [Fact]
    public async Task SubmitAsync_RejectsUnknownEventAndMissingAgreement()
    {
        var repository = new CapturingRepository();
        var service = CreateService(repository);
        var request = CreateValidRequest(eventCode: "unknown", agreed: false);

        var exception = await Assert.ThrowsAsync<WaiverValidationException>(
            () => service.SubmitAsync(request));

        Assert.Contains(nameof(request.EventCode), exception.Errors.Keys);
        Assert.Contains(nameof(request.Agreed), exception.Errors.Keys);
        Assert.Equal(0, repository.CreateCallCount);
    }

    [Fact]
    public async Task SubmitAsync_RejectsMoreThanTenFamilyMembers()
    {
        var repository = new CapturingRepository();
        var service = CreateService(repository);
        var members = Enumerable.Range(1, 11)
            .Select(index => new SubmitWaiverFamilyMember
            {
                FirstName = $"Member{index}",
                LastName = "Family"
            })
            .ToArray();

        var exception = await Assert.ThrowsAsync<WaiverValidationException>(
            () => service.SubmitAsync(CreateValidRequest(familyMembers: members)));

        Assert.Contains(
            nameof(SubmitWaiverRequest.FamilyMembers),
            exception.Errors.Keys);
        Assert.Equal(0, repository.CreateCallCount);
    }

    [Fact]
    public async Task SubmitAsync_AllowsDuplicateCustomerSubmissions()
    {
        var repository = new CapturingRepository();
        var service = CreateService(repository);
        var request = CreateValidRequest();

        var first = await service.SubmitAsync(request);
        var second = await service.SubmitAsync(request);

        Assert.Equal(2, repository.CreateCallCount);
        Assert.NotEqual(first.SubmissionReference, second.SubmissionReference);
    }

    [Fact]
    public async Task SubmitAsync_ThrowsClearErrorForMissingOwnerEmailConfiguration()
    {
        var repository = new CapturingRepository();
        var options = OptionsFactory.Create(new WaiverOptions
        {
            Events = { ["summer-camp-2026"] = "Summer Camp 2026" }
        });
        var service = new WaiverSubmissionService(
            repository,
            options,
            new FixedTimeProvider(FixedUtcNow));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SubmitAsync(CreateValidRequest()));

        Assert.Contains("Waiver:BusinessOwnerEmail", exception.Message);
        Assert.Equal(0, repository.CreateCallCount);
    }

    private static WaiverSubmissionService CreateService(
        IWaiverSubmissionRepository repository)
    {
        var options = OptionsFactory.Create(new WaiverOptions
        {
            BusinessOwnerEmail = "owner@example.com",
            Events = { ["SUMMER-CAMP-2026"] = " Summer Camp 2026 " }
        });
        return new WaiverSubmissionService(
            repository,
            options,
            new FixedTimeProvider(FixedUtcNow));
    }

    private static SubmitWaiverRequest CreateValidRequest(
        string eventCode = " Summer-Camp-2026 ",
        string firstName = " Customer ",
        bool agreed = true,
        IReadOnlyCollection<SubmitWaiverFamilyMember>? familyMembers = null) =>
        new()
        {
            EventCode = eventCode,
            FirstName = firstName,
            LastName = " Person ",
            WechatName = " WeChat User ",
            Email = " Customer@Example.com ",
            Phone = " (416) 555-0123 ",
            SignatureName = " Customer Person ",
            Agreed = agreed,
            FamilyMembers = familyMembers ?? [],
            IpAddress = " 127.0.0.1 ",
            UserAgent = " Test Browser "
        };

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class CapturingRepository : IWaiverSubmissionRepository
    {
        public WaiverSubmission? Submission { get; private set; }
        public IReadOnlyCollection<WaiverFamilyMember> FamilyMembers { get; private set; } = [];
        public IReadOnlyCollection<EmailOutboxMessage> OutboxMessages { get; private set; } = [];
        public int CreateCallCount { get; private set; }

        public Task<ulong> CreateSubmissionAsync(
            WaiverSubmission submission,
            IReadOnlyCollection<WaiverFamilyMember> familyMembers,
            IReadOnlyCollection<EmailOutboxMessage> outboxMessages,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Submission = submission;
            FamilyMembers = familyMembers;
            OutboxMessages = outboxMessages;
            CreateCallCount++;
            submission.Id = (ulong)CreateCallCount;
            return Task.FromResult(submission.Id);
        }

        public Task<WaiverSubmission?> GetBySubmissionReferenceAsync(
            string submissionReference,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<WaiverSubmission?>(null);

        public Task<ulong> InsertFamilyMemberAsync(
            ulong submissionId,
            WaiverFamilyMember familyMember,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<ulong> InsertSubmissionAsync(
            WaiverSubmission submission,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
