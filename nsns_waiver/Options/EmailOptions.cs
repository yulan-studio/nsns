namespace nsns_waiver.Options;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public bool Enabled { get; set; }
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "NSNS Waiver";
    public int PollIntervalSeconds { get; set; } = 10;
    public int BatchSize { get; set; } = 20;
    public int MaximumAttempts { get; set; } = 5;
    public SmtpOptions Smtp { get; set; } = new();
}

public sealed class SmtpOptions
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool UseStartTls { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
