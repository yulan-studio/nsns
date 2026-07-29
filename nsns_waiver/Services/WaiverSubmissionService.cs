using System.Net.Mail;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Options;
using nsns_waiver.Models;
using nsns_waiver.Options;
using nsns_waiver.Repositories;

namespace nsns_waiver.Services;

public sealed class WaiverSubmissionService : IWaiverSubmissionService
{
    public const int MaximumFamilyMembers = 10;

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
        var configuredEvent = FindEvent(request.EventCode);
        var eventCode = NormalizeEventCode(request.EventCode);
        var eventName = configuredEvent?.Name;

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
            submission, familyMembers, ownerEmail);

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

    public WaiverEventInfo? FindEvent(string? eventCode)
    {
        var normalizedCode = NormalizeEventCode(eventCode);

        if (normalizedCode.Length == 0)
        {
            return null;
        }

        foreach (var configuredEvent in _options.Events)
        {
            if (NormalizeEventCode(configuredEvent.Key) == normalizedCode
                && !string.IsNullOrWhiteSpace(configuredEvent.Value))
            {
                return new WaiverEventInfo(
                    normalizedCode,
                    configuredEvent.Value.Trim());
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
        IReadOnlyCollection<WaiverFamilyMember> familyMembers,
        string ownerEmail)
    {
        var eventName = HtmlEncoder.Default.Encode(submission.EventName);
        var customerName = HtmlEncoder.Default.Encode(
            $"{submission.FirstName} {submission.LastName}");

        var messages = new List<EmailOutboxMessage>
        {
            new EmailOutboxMessage
            {
                MessageType = "CustomerConfirmation",
                RecipientEmail = submission.Email,
                Subject = $"Waiver received - {submission.EventName}",
                BodyHtml =
                    $"<p>Dear {customerName},</p>"
                    + $"<p>Thank you for submitting your waiver for "
                    + $"{eventName}. We are pleased to confirm that it has "
                    + "been received successfully.</p>"
                    + "<p>No further action is required at this time. "
                    + "Please retain this email for your records.</p>"
                    + "<p>Sincerely,<br>The NorthStar Team</p>"
            }
        };
        messages.Add(new EmailOutboxMessage
            {
                MessageType = "BossNotification",
                RecipientEmail = ownerEmail,
                Subject = $"New waiver submission - {submission.EventName}",
                BodyHtml = CreateBossNotificationBody(
                    submission,
                    familyMembers,
                    eventName)
            });

        return messages;
    }

    private static string CreateBossNotificationBody(
        WaiverSubmission submission,
        IReadOnlyCollection<WaiverFamilyMember> familyMembers,
        string encodedEventName)
    {
        var encoder = HtmlEncoder.Default;
        var body = new StringBuilder()
            .Append("<h2>New waiver submission</h2>")
            .Append(
                "<hr style=\"border: 0; border-top: 1px solid #b7b7b7; "
                + "margin: 16px 0;\">")
            .Append("<h3><strong>")
            .Append(encodedEventName)
            .Append("</strong></h3>")
            .Append("</p><h3>Person submitting the waiver</h3><ul>")
            .Append("<li><strong>Name:</strong> ")
            .Append(encoder.Encode($"{submission.FirstName} {submission.LastName}"))
            .Append("</li><li><strong>WeChat name:</strong> ")
            .Append(EncodeOptional(encoder, submission.WechatName))
            .Append("</li><li><strong>Email:</strong> ")
            .Append(encoder.Encode(submission.Email))
            .Append("</li><li><strong>Phone:</strong> ")
            .Append(encoder.Encode(submission.Phone))
            .Append("</li><li><strong>Electronic signature:</strong> ")
            .Append(encoder.Encode(submission.SignatureName))
            .Append("</li></ul><h3>Family members</h3>");

        if (familyMembers.Count == 0)
        {
            body.Append("<p>None</p>");
        }
        else
        {
            body.Append("<ol>");
            foreach (var member in familyMembers)
            {
                body.Append("<li>")
                    .Append(encoder.Encode($"{member.FirstName} {member.LastName}"));

                if (!string.IsNullOrWhiteSpace(member.Relationship))
                {
                    body.Append(" — ")
                        .Append(encoder.Encode(member.Relationship));
                }

                body.Append("</li>");
            }

            body.Append("</ol>");
        }

        body.Append(
            "<hr style=\"border: 0; border-top: 1px solid #b7b7b7; "
            + "margin: 16px 0;\">"
            + "<p style=\"color: #000000;\">To view this and other waiver "
            + "submissions, please visit the "
            + "<a href=\"https://waiver.nsns.ca/Admin/Submissions\" "
            + "style=\"color: #000000;\">"
            + "waiver submissions page</a>.</p>");

        return body.ToString();
    }

    private static string EncodeOptional(HtmlEncoder encoder, string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? "Not provided"
            : encoder.Encode(value);

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
