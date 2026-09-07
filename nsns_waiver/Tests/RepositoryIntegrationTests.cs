using Dapper;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using nsns_waiver.Data;
using nsns_waiver.Models;
using nsns_waiver.Repositories;

namespace nsns_waiver.Tests;

public sealed class RepositoryIntegrationTests
{
    [MySqlIntegrationFact]
    public async Task CreateSubmission_CommitsZeroFamilyMembersAndTwoOutboxMessages()
    {
        var repository = await CreateRepositoryAsync();
        var submission = CreateSubmission();
        var messages = CreateOutboxMessages();

        var id = await repository.CreateSubmissionAsync(submission, [], messages);

        await using var connection = await OpenTestConnectionAsync();
        var familyCount = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM waiver_family_members WHERE submission_id = @Id;",
            new { Id = id });
        var outboxCount = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM email_outbox WHERE submission_id = @Id;",
            new { Id = id });

        Assert.Equal(0, familyCount);
        Assert.Equal(2, outboxCount);
        Assert.All(messages, message => Assert.Equal(id, message.SubmissionId));

        await DeleteSubmissionAsync(submission.SubmissionReference);
    }

    [MySqlIntegrationFact]
    public async Task CreateSubmission_CommitsMultipleFamilyMembers()
    {
        var repository = await CreateRepositoryAsync();
        var submission = CreateSubmission();
        var familyMembers = new[]
        {
            CreateFamilyMember("FamilyOne"),
            CreateFamilyMember("FamilyTwo")
        };

        var id = await repository.CreateSubmissionAsync(submission, familyMembers, []);

        await using var connection = await OpenTestConnectionAsync();
        var count = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM waiver_family_members WHERE submission_id = @Id;",
            new { Id = id });

        Assert.Equal(2, count);
        Assert.All(familyMembers, member => Assert.Equal(id, member.SubmissionId));

        await DeleteSubmissionAsync(submission.SubmissionReference);
    }

    [MySqlIntegrationFact]
    public async Task CreateSubmission_RollsBackWhenFamilyMemberInsertFails()
    {
        var repository = await CreateRepositoryAsync();
        var submission = CreateSubmission();
        var invalidMember = CreateFamilyMember(null!);

        await Assert.ThrowsAsync<MySqlException>(
            () => repository.CreateSubmissionAsync(submission, [invalidMember], []));

        Assert.False(await SubmissionExistsAsync(submission.SubmissionReference));
    }

    [MySqlIntegrationFact]
    public async Task CreateSubmission_RollsBackWhenOutboxInsertFails()
    {
        var repository = await CreateRepositoryAsync();
        var submission = CreateSubmission();
        var invalidMessage = new EmailOutboxMessage
        {
            MessageType = "CustomerConfirmation",
            RecipientEmail = "customer@example.invalid",
            Subject = "Confirmation",
            BodyHtml = null!
        };

        await Assert.ThrowsAsync<MySqlException>(
            () => repository.CreateSubmissionAsync(submission, [], [invalidMessage]));

        Assert.False(await SubmissionExistsAsync(submission.SubmissionReference));
    }

    [MySqlIntegrationFact]
    public async Task GetByReference_MapsUtcDateFields()
    {
        var repository = await CreateRepositoryAsync();
        var submission = CreateSubmission();
        await repository.CreateSubmissionAsync(submission, [], []);

        var loaded = await repository.GetBySubmissionReferenceAsync(
            submission.SubmissionReference);

        Assert.NotNull(loaded);
        Assert.Equal(submission.SubmissionReference, loaded.SubmissionReference);
        Assert.True(loaded.MediaReleaseAgreed);
        Assert.Equal(DateTimeKind.Utc, loaded.SignedAtUtc.Kind);
        Assert.Equal(DateTimeKind.Utc, loaded.CreatedAtUtc.Kind);

        await DeleteSubmissionAsync(submission.SubmissionReference);
    }

    private static async Task<IWaiverSubmissionRepository> CreateRepositoryAsync()
    {
        await using var connection = await OpenTestConnectionAsync();
        var migration = await File.ReadAllTextAsync(
            Path.Combine(AppContext.BaseDirectory, "001_create_waiver_tables.sql"));
        await connection.ExecuteAsync(migration);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = GetTestConnectionString()
            })
            .Build();
        return new WaiverSubmissionRepository(new MySqlConnectionFactory(configuration));
    }

    private static async Task<MySqlConnection> OpenTestConnectionAsync()
    {
        var builder = new MySqlConnectionStringBuilder(GetTestConnectionString())
        {
            DateTimeKind = MySqlDateTimeKind.Utc,
            GuidFormat = MySqlGuidFormat.None
        };
        var connection = new MySqlConnection(builder.ConnectionString);
        await connection.OpenAsync();
        return connection;
    }

    private static string GetTestConnectionString() =>
        Environment.GetEnvironmentVariable("WAIVERAPP_TEST_MYSQL_CONNECTION")
        ?? throw new InvalidOperationException("MySQL test connection is not configured.");

    private static WaiverSubmission CreateSubmission() =>
        new()
        {
            SubmissionReference = Guid.NewGuid().ToString(),
            EventCode = "integration-test",
            EventName = "Integration Test",
            FirstName = "Test",
            LastName = "Customer",
            Email = "test@example.invalid",
            NormalizedEmail = "test@example.invalid",
            Phone = "5550100",
            NormalizedPhone = "5550100",
            SignatureName = "Test Customer",
            Agreed = true,
            MediaReleaseAgreed = true,
            SignedAtUtc = DateTime.UtcNow
        };

    private static WaiverFamilyMember CreateFamilyMember(string firstName) =>
        new()
        {
            FirstName = firstName,
            LastName = "Member",
            Relationship = "Family"
        };

    private static EmailOutboxMessage[] CreateOutboxMessages() =>
    [
        new()
        {
            MessageType = "CustomerConfirmation",
            RecipientEmail = "customer@example.invalid",
            Subject = "Confirmation",
            BodyHtml = "<p>Confirmed</p>"
        },
        new()
        {
            MessageType = "BossNotification",
            RecipientEmail = "owner@example.invalid",
            Subject = "New waiver",
            BodyHtml = "<p>New waiver</p>"
        }
    ];

    private static async Task<bool> SubmissionExistsAsync(string submissionReference)
    {
        await using var connection = await OpenTestConnectionAsync();
        return await connection.ExecuteScalarAsync<bool>(
            """
            SELECT EXISTS(
                SELECT 1
                FROM waiver_submissions
                WHERE submission_reference = @SubmissionReference);
            """,
            new { SubmissionReference = submissionReference });
    }

    private static async Task DeleteSubmissionAsync(string submissionReference)
    {
        await using var connection = await OpenTestConnectionAsync();
        await connection.ExecuteAsync(
            "DELETE FROM waiver_submissions WHERE submission_reference = @SubmissionReference;",
            new { SubmissionReference = submissionReference });
    }
}
