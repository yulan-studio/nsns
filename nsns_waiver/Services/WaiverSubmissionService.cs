using System.Net.Mail;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Options;
using nsns_waiver.Models;
using nsns_waiver.Options;
using nsns_waiver.Repositories;

namespace nsns_waiver.Services;

/// <summary>
/// Validates waiver input, builds related records, and coordinates atomic persistence.
/// </summary>
public sealed class WaiverSubmissionService : IWaiverSubmissionService
{
    public const int MaximumFamilyMembers = 10;

    private readonly IWaiverSubmissionRepository _repository;
    private readonly WaiverOptions _options;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Creates the service with persistence, configuration, and a testable clock.
    /// </summary>
    public WaiverSubmissionService(
        IWaiverSubmissionRepository repository,
        IOptions<WaiverOptions> options,
        TimeProvider timeProvider)
    {
        _repository = repository;
        _options = options.Value;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Validates all form data, queues both emails, and saves everything transactionally.
    /// </summary>
    public async Task<SubmitWaiverResult> SubmitAsync(
        SubmitWaiverRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Collect every validation problem so the customer can fix the form in one pass.
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

        // Security-sensitive values are generated on the server after validation.
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
            MediaReleaseAgreed = request.MediaReleaseAgreed,
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

    /// <summary>
    /// Normalizes and resolves a query-string event code from configured events.
    /// </summary>
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

    /// <summary>
    /// Validates the configured owner address used for notification emails.
    /// </summary>
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

    /// <summary>
    /// Validates, trims, and maps up to the configured family-member limit.
    /// </summary>
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

    /// <summary>
    /// Builds the customer confirmation and business-owner notification messages.
    /// </summary>
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

    /// <summary>
    /// Builds an HTML-safe owner notification containing all submitted details.
    /// </summary>
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
            
            .Append(
                "<h3><strong><span style=\"background-color: #fff3cd;\">")
            .Append(encodedEventName)
            .Append(
                "</span></strong></h3>"
                + "<h3>Person submitting the waiver</h3><ul>")
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
            .Append("</li><li><strong>Media release:</strong> ")
            .Append(submission.MediaReleaseAgreed ? "Agreed" : "Declined")
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
            + "<p>To view this and other waiver submissions, please visit the "
            + "<a href=\"https://waiver.nsns.ca/Admin/Submissions\">"
            + "waiver submissions page</a>.</p>");

        return body.ToString();
    }

    /// <summary>
    /// Encodes optional email content or returns a readable fallback.
    /// </summary>
    private static string EncodeOptional(HtmlEncoder encoder, string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? "Not provided"
            : encoder.Encode(value);

    /// <summary>
    /// Validates and returns a trimmed email address.
    /// </summary>
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

    /// <summary>
    /// Trims a required value and records missing or length errors.
    /// </summary>
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

    /// <summary>
    /// Converts blank optional input to null and records length errors.
    /// </summary>
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

    /// <summary>
    /// Produces the canonical lower-case event code used for matching and storage.
    /// </summary>
    private static string NormalizeEventCode(string? value) =>
        value?.Trim().ToLowerInvariant() ?? string.Empty;

    /// <summary>
    /// Removes formatting from a phone number for normalized lookup.
    /// </summary>
    private static string NormalizePhone(string value) =>
        string.Concat(value.Where(char.IsDigit));

    /// <summary>
    /// Adds one message to a field's accumulated validation errors.
    /// </summary>
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
