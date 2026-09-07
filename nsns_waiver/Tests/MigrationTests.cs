using System.Text.RegularExpressions;

namespace nsns_waiver.Tests;

public sealed class MigrationTests
{
    private readonly string _sql =
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "001_create_waiver_tables.sql"));

    [Fact]
    public void Migration_HasOnlyTheRequiredTablesAndIsIdempotent()
    {
        var tables = Regex.Matches(
                _sql,
                @"CREATE\s+TABLE\s+IF\s+NOT\s+EXISTS\s+(\w+)",
                RegexOptions.IgnoreCase)
            .Select(match => match.Groups[1].Value)
            .ToArray();

        Assert.Equal(
            ["waiver_submissions", "waiver_family_members", "email_outbox"],
            tables);
        Assert.DoesNotContain("DROP TABLE", _sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("TRUNCATE TABLE", _sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SubmissionReferenceIsUnique_ButDuplicateCustomerSubmissionsAreAllowed()
    {
        Assert.Matches(
            @"UNIQUE\s+KEY\s+\w+\s*\(\s*submission_reference\s*\)",
            _sql);
        Assert.DoesNotMatch(
            @"UNIQUE\s+(?:KEY|INDEX)[^(]*\([^)]*(?:email|normalized_email|event_code|phone)",
            _sql);
    }

    [Fact]
    public void SubmissionStoresOptionalMediaReleaseChoice()
    {
        Assert.Matches(
            @"media_release_agreed\s+BOOLEAN\s+NOT\s+NULL\s+DEFAULT\s+TRUE",
            _sql);
    }

    [Fact]
    public void RelatedTablesHaveCascadeForeignKeysAndRequiredIndexes()
    {
        Assert.Contains(
            "FOREIGN KEY (submission_id) REFERENCES waiver_submissions (id)",
            _sql);
        Assert.Equal(
            2,
            Regex.Matches(_sql, @"ON\s+DELETE\s+CASCADE", RegexOptions.IgnoreCase).Count);
        Assert.DoesNotContain("ON UPDATE CASCADE", _sql, StringComparison.OrdinalIgnoreCase);

        var requiredIndexes = new[]
        {
            "ix_waiver_submissions_event_code",
            "ix_waiver_submissions_normalized_email",
            "ix_waiver_submissions_normalized_phone",
            "ix_waiver_submissions_event_email",
            "ix_waiver_submissions_signed_at_utc",
            "ix_waiver_family_members_submission_id",
            "ix_email_outbox_submission_id",
            "ix_email_outbox_status_next_attempt",
            "ix_email_outbox_created_at_utc"
        };

        foreach (var index in requiredIndexes)
        {
            Assert.Contains(index, _sql);
        }
    }
}
