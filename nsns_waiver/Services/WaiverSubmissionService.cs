using System.Net.Mail;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Options;
using nsns_waiver.Models;
using nsns_waiver.Options;
using nsns_waiver.Repositories;

namespace nsns_waiver.Services;

public sealed class WaiverSubmissionService : IWaiverSubmissionService
{
    public const int MaximumFamilyMembers = 20;

    private readonly IWaiverSubmissionRepository _repository;
    private readonly WaiverOptions _options;
    private readonly TimeProvider _timeProvider;

    public WaiverSubmissionService(
        IWaiverSubmissionRepository repository,
        IOptions<WaiverOptions> options,
        TimeProvider timeProvider)
    {
        _repository = repository;
        _options = options.Value;
        _timeProvider = timeProvider;
    }

    public async Task<SubmitWaiverResult> SubmitAsync(
        SubmitWaiverRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var errors = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var eventCode = NormalizeEventCode(request.EventCode);
        var eventName = ResolveEventName(eventCode);

        if (eventName is null)
        {
            AddError(errors, nameof(request.EventCode), "The selected event is not valid.");
        }

        var firstName = ValidateRequired(
            errors, nameof(request.FirstName), request.FirstName, 100);
        var lastName = ValidateRequired(
            errors, nameof(request.LastName), request.LastName, 100);
        var wechatName = ValidateOptional(
            errors, nameof(request.WechatName), request.WechatName, 100);
        var email = ValidateEmail(errors, request.Email);
        var phone = ValidateRequired(errors, nameof(request.Phone), request.Phone, 40);
        var normalizedPhone = NormalizePhone(phone);
        var signatureName = ValidateRequired(
            errors, nameof(request.SignatureName), request.SignatureName, 200);
        var ipAddress = ValidateOptional(
            errors, nameof(request.IpAddress), request.IpAddress, 45);
        var userAgent = ValidateOptional(
            errors, nameof(request.UserAgent), request.UserAgent, 500);

        if (normalizedPhone.Length == 0)
        {
            AddError(errors, nameof(request.Phone), "Phone must contain at least one digit.");
        }

        if (!request.Agreed)
        {
            AddError(errors, nameof(request.Agreed), "Agreement is required.");
        }

        var familyMembers = ValidateFamilyMembers(errors, request.FamilyMembers);
        var ownerEmail = ValidateOwnerConfiguration();

        if (errors.Count > 0)
        {
            throw new WaiverValidationException(
                errors.ToDictionary(
                    pair => pair.Key,
                    pair => pair.Value.ToArray(),
                    StringComparer.Ordinal));
        }

        var signedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var submissionReference = Guid.NewGuid().ToString();
        var submission = new WaiverSubmission
        {
            SubmissionReference = submissionReference,
            EventCode = eventCode,
            EventName = eventName!,
            FirstName = firstName,
            LastName = lastName,
            WechatName = wechatName,
            Email = email,
            NormalizedEmail = email.ToLowerInvariant(),
            Phone = phone,
            NormalizedPhone = normalizedPhone,
            SignatureName = signatureName,
            Agreed = true,
            SignedAtUtc = signedAtUtc,
            IpAddress = ipAddress,
            UserAgent = userAgent
        };
        var outboxMessages = CreateOutboxMessages(
            submission, ownerEmail, signedAtUtc);

        await _repository.CreateSubmissionAsync(
            submission,
            familyMembers,
            outboxMessages,
            cancellationToken);

        return new SubmitWaiverResult(
            submissionReference,
            eventName!,
            signedAtUtc);
    }

    private string? ResolveEventName(string eventCode)
    {
        foreach (var configuredEvent in _options.Events)
        {
            if (NormalizeEventCode(configuredEvent.Key) == eventCode
                && !string.IsNullOrWhiteSpace(configuredEvent.Value))
            {
                return configuredEvent.Value.Trim();
            }
        }

        return null;
    }

    private string ValidateOwnerConfiguration()
    {
        var ownerEmail = _options.BusinessOwnerEmail.Trim();

        if (!MailAddress.TryCreate(ownerEmail, out _))
        {
            throw new InvalidOperationException(
                "Waiver:BusinessOwnerEmail must contain a valid email address.");
        }

        return ownerEmail;
    }

    private static List<WaiverFamilyMember> ValidateFamilyMembers(
        Dictionary<string, List<string>> errors,
        IReadOnlyCollection<SubmitWaiverFamilyMember>? requestedMembers)
    {
        var members = requestedMembers ?? [];

        if (members.Count > MaximumFamilyMembers)
        {
            AddError(
                errors,
                nameof(SubmitWaiverRequest.FamilyMembers),
                $"A submission can include at most {MaximumFamilyMembers} family members.");
        }

        var validated = new List<WaiverFamilyMember>(
            Math.Min(members.Count, MaximumFamilyMembers));
        var index = 0;

        foreach (var member in members.Take(MaximumFamilyMembers))
        {
            var prefix = $"{nameof(SubmitWaiverRequest.FamilyMembers)}[{index}]";
            var firstName = ValidateRequired(
                errors, $"{prefix}.FirstName", member.FirstName, 100);
            var lastName = ValidateRequired(
                errors, $"{prefix}.LastName", member.LastName, 100);
            var relationship = ValidateOptional(
                errors, $"{prefix}.Relationship", member.Relationship, 100);

            validated.Add(new WaiverFamilyMember
            {
                FirstName = firstName,
                LastName = lastName,
                Relationship = relationship
            });
            index++;
        }

        return validated;
    }

    private static List<EmailOutboxMessage> CreateOutboxMessages(
        WaiverSubmission submission,
        string ownerEmail,
        DateTime signedAtUtc)
    {
        var eventName = HtmlEncoder.Default.Encode(submission.EventName);
        var customerName = HtmlEncoder.Default.Encode(
            $"{submission.FirstName} {submission.LastName}");
        var reference = HtmlEncoder.Default.Encode(submission.SubmissionReference);
        var signedAt = HtmlEncoder.Default.Encode(
            signedAtUtc.ToString("yyyy-MM-dd HH:mm:ss 'UTC'"));

        return
        [
            new EmailOutboxMessage
            {
                MessageType = "CustomerConfirmation",
                RecipientEmail = submission.Email,
                Subject = $"Waiver confirmation - {submission.EventName}",
                BodyHtml =
                    $"<p>Hello {customerName},</p>"
                    + $"<p>Your waiver for {eventName} was received.</p>"
                    + $"<p>Reference: {reference}<br>Signed: {signedAt}</p>"
            },
            new EmailOutboxMessage
            {
                MessageType = "BossNotification",
                RecipientEmail = ownerEmail,
                Subject = $"New waiver submission - {submission.EventName}",
                BodyHtml =
                    $"<p>A waiver was submitted for {eventName}.</p>"
                    + $"<p>Reference: {reference}<br>Signed: {signedAt}</p>"
            }
        ];
    }

    private static string ValidateEmail(
        Dictionary<string, List<string>> errors,
        string? value)
    {
        var email = ValidateRequired(errors, nameof(SubmitWaiverRequest.Email), value, 320);

        if (email.Length > 0 && !MailAddress.TryCreate(email, out _))
        {
            AddError(
                errors,
                nameof(SubmitWaiverRequest.Email),
                "Email must be a valid email address.");
        }

        return email;
    }

    private static string ValidateRequired(
        Dictionary<string, List<string>> errors,
        string field,
        string? value,
        int maximumLength)
    {
        var trimmed = value?.Trim() ?? string.Empty;

        if (trimmed.Length == 0)
        {
            AddError(errors, field, $"{field} is required.");
        }
        else if (trimmed.Length > maximumLength)
        {
            AddError(
                errors,
                field,
                $"{field} cannot exceed {maximumLength} characters.");
        }

        return trimmed;
    }

    private static string? ValidateOptional(
        Dictionary<string, List<string>> errors,
        string field,
        string? value,
        int maximumLength)
    {
        var trimmed = value?.Trim();

        if (string.IsNullOrEmpty(trimmed))
        {
            return null;
        }

        if (trimmed.Length > maximumLength)
        {
            AddError(
                errors,
                field,
                $"{field} cannot exceed {maximumLength} characters.");
        }

        return trimmed;
    }

    private static string NormalizeEventCode(string? value) =>
        value?.Trim().ToLowerInvariant() ?? string.Empty;

    private static string NormalizePhone(string value) =>
        string.Concat(value.Where(char.IsDigit));

    private static void AddError(
        Dictionary<string, List<string>> errors,
        string field,
        string message)
    {
        if (!errors.TryGetValue(field, out var messages))
        {
            messages = [];
            errors[field] = messages;
        }

        messages.Add(message);
    }
}
