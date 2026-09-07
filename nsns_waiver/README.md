# NSNS Waiver Application

ASP.NET Core Razor Pages application for submitting event liability waivers. See
`PROJECT.md` for business requirements and `AGENTS.md` for implementation standards.

## Technology

- .NET 10 and ASP.NET Core Razor Pages
- MySQL 8.0 or later
- Dapper and MySqlConnector
- Bootstrap

## Build and run

```powershell
dotnet restore
dotnet build
dotnet run
```

## Database configuration

Create a local empty database with placeholder local credentials:

```sql
CREATE DATABASE nsns_waiver
  CHARACTER SET utf8mb4
  COLLATE utf8mb4_unicode_ci;
```

Configure the `Default` connection string with .NET User Secrets:

```powershell
dotnet user-secrets set "ConnectionStrings:Default" "Server=localhost;Port=3306;Database=nsns_waiver;User ID=waiver_local;Password=<local-password>"
```

Alternatively, use the standard ASP.NET Core environment-variable mapping:

```powershell
$env:ConnectionStrings__Default = "Server=localhost;Port=3306;Database=nsns_waiver;User ID=waiver_local;Password=<local-password>"
```

Never commit database credentials, production connection strings, or other secrets.

Configure allowed events and the business-owner notification address using the
`Waiver` section. Event keys are the values accepted from the `event` query string:

```json
{
  "Waiver": {
    "BusinessOwnerEmail": "owner@example.com",
    "Events": {
      "summer-camp-2026": "Summer Camp 2026"
    }
  }
}
```

For deployed environments, the equivalent keys include
`Waiver__BusinessOwnerEmail` and
`Waiver__Events__summer-camp-2026`. Keep real addresses in environment-specific
configuration rather than committing them to source control.

## Boss submission page

The protected submission page is available at `/Admin/Submissions`. Configure
its credentials locally with User Secrets:

```powershell
dotnet user-secrets set "Admin:Username" "boss"
dotnet user-secrets set "Admin:Password" "<a-strong-local-password>"
```

For Railway, use the `Admin__Username` and `Admin__Password` environment
variables. Do not put real admin credentials in `appsettings.json`.

## Database migration

The application does not create or update its schema during web requests. From the
project root, apply migrations explicitly and in numeric order:

```powershell
mysql --host=localhost --user=waiver_local --password nsns_waiver < Data/Migrations/001_create_waiver_tables.sql
```

The initial migration is idempotent and creates:

- `waiver_submissions`
- `waiver_family_members`
- `email_outbox`

Duplicate waiver submissions—including duplicate event and email combinations—are
intentionally allowed. Only the server-generated submission reference is unique.

The agreement remains in `Content/waiver-agreement.html`. Agreement content and
metadata are not stored in MySQL.

The repository initially contains a marked placeholder agreement. Replace it
with the organization’s approved legal HTML before accepting submissions. The
form detects the placeholder marker and disables signing and submission until
approved content is present.

## Tests

Run unit tests:

```powershell
dotnet test
```

MySQL integration tests require an empty, disposable MySQL test database:

```powershell
# First edit Tests/test-database.local.ps1 with your local test credentials.
. .\Tests\test-database.local.ps1
dotnet test
```

The local PowerShell file is excluded by `.gitignore`. Loading it sets
`WAIVERAPP_TEST_MYSQL_CONNECTION` for the current terminal session only. Verify
that it is loaded without printing the secret:

```powershell
if ($env:WAIVERAPP_TEST_MYSQL_CONNECTION) { "Test connection is configured" }
```

Never use a production database for tests. If
`WAIVERAPP_TEST_MYSQL_CONNECTION` is absent, MySQL integration tests are reported
as skipped and the unit tests still run.

## Project structure

```text
NSNS-Waiver/
|-- Content/
|   `-- waiver-agreement.html
|-- Data/
|   `-- Migrations/
|-- Models/
|-- Repositories/
|-- Services/
|-- Tests/
|-- Pages/
|-- wwwroot/
`-- Program.cs
```

Keep business logic in services, SQL in repositories, and Razor Pages focused on
UI concerns. Use asynchronous operations, dependency injection, parameterized SQL,
and UTC timestamps.
