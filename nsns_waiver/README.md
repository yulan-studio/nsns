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

## Tests

Run unit tests:

```powershell
dotnet test
```

MySQL integration tests require an empty, disposable MySQL test database:

```powershell
$env:WAIVERAPP_TEST_MYSQL_CONNECTION = "Server=localhost;Port=3306;Database=nsns_waiver_test;User ID=waiver_test;Password=<test-password>"
dotnet test
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
