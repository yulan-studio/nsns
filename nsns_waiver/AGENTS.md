Project Name:
NSNS Waiver Application

Technology:

- ASP.NET Core Razor Pages
- .NET 10
- MySQL
- Dapper
- MySqlConnector
- Bootstrap
- Railway deployment

Coding Style:

- Use dependency injection.
- Use async methods.
- Use parameterized SQL.
- Keep business logic in services.
- Keep database access in repositories.
- Do not use Entity Framework.
- Keep Razor Pages simple.

Application Rules:

- One waiver HTML file.
- No waiver versions.
- No agreement snapshots.
- Event comes from ?event=xxxxx.
- Allow duplicate submissions.
- Save event name in database.
- Save family members separately.
- Maximum 20 family members.
- Use server-generated signing date.
- Send confirmation email.
- Notify boss by email.

Always run:

dotnet build

after making changes.

When finished, summarize what changed.