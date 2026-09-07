using nsns_waiver.Repositories;

namespace nsns_waiver.Tests;

public sealed class RepositorySqlTests
{
    [Theory]
    [MemberData(nameof(InsertStatements))]
    public void InsertSql_UsesNamedParametersAndExplicitColumns(string name, string sql)
    {
        Assert.False(string.IsNullOrWhiteSpace(name));
        Assert.Contains("INSERT INTO", sql);
        Assert.Contains("@", sql);
        Assert.DoesNotContain("SELECT *", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("+", sql);
    }

    public static TheoryData<string, string> InsertStatements =>
        new()
        {
            {
                "submission",
                WaiverSubmissionRepository.InsertSubmissionSql
            },
            {
                "family member",
                WaiverSubmissionRepository.InsertFamilyMemberSql
            },
            {
                "transactional outbox",
                WaiverSubmissionRepository.InsertOutboxSql
            },
            {
                "standalone outbox",
                EmailOutboxRepository.InsertSql
            }
        };

    [Fact]
    public void SubmissionSql_PersistsMediaReleaseChoice()
    {
        Assert.Contains(
            "media_release_agreed",
            WaiverSubmissionRepository.InsertSubmissionSql);
        Assert.Contains(
            "@MediaReleaseAgreed",
            WaiverSubmissionRepository.InsertSubmissionSql);
    }
}
