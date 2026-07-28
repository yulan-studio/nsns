# NSNS Waiver Application - AGENTS.md

## Purpose

This document provides implementation instructions for AI coding agents working on this project.

Read `PROJECT.md` before making code changes. `PROJECT.md` defines the business requirements. This document defines the technical implementation standards.

---

## Technology Stack

- ASP.NET Core Razor Pages
- .NET 10
- MySQL
- Dapper
- MySqlConnector
- Bootstrap
- Docker
- Railway deployment

Do not replace these technologies unless explicitly instructed.

Do not migrate the project to Blazor, MVC, React, Angular, Vue, Entity Framework Core, or another technology stack.

---

## Architecture

- Keep the application as a single ASP.NET Core Razor Pages project.
- Use dependency injection throughout the application.
- Keep business logic in services.
- Keep database access in repositories.
- Use Dapper for all database access.
- Use parameterized SQL only.
- Use asynchronous database operations.
- Use nullable reference types.
- Use UTC for all stored timestamps.

---

## Waiver Agreement

- Store the waiver agreement in `Content/waiver-agreement.html`.
- Load the agreement from the HTML file.
- Do not store the agreement in MySQL.
- Do not create waiver versions.
- Do not create agreement snapshots.
- Do not create agreement hashes.
- Do not create agreement metadata tables.

---

## Event Handling

- Event is supplied using the `event` query-string parameter.
- Validate the event against server-side configuration.
- Store both `event_code` and `event_name` with every submission.
- Do not create an Events database table.

---

## Database

Use MySQL.

Primary tables:

- `waiver_submissions`
- `waiver_family_members`
- `email_outbox`

Do not introduce additional database tables unless explicitly requested.

---

## Application Rules

- Allow duplicate submissions.
- Save family members in a separate table.
- Maximum of 20 family members per submission.
- Generate the signed date on the server.
- Generate a UUID submission reference.
- Queue a confirmation email for the customer.
- Queue a notification email for the business owner.
- Use a database transaction when saving a submission and related records.

---

## Coding Standards

- Use dependency injection.
- Use async/await.
- Use constructor injection.
- Keep Razor Pages focused on UI concerns.
- Keep business logic inside services.
- Keep SQL inside repositories.
- Keep methods small and focused.
- Prefer composition over inheritance.
- Write clear, self-documenting code.
- Use meaningful names.
- Avoid unnecessary complexity.

---

## Security

- Use parameterized SQL only.
- Validate all user input on the server.
- Enable anti-forgery protection for form submissions.
- Never log connection strings.
- Never log passwords or secrets.
- Avoid logging personally identifiable information unless necessary for troubleshooting.

---

## Development Workflow

Before implementing a task:

1. Review the relevant section of `PROJECT.md`.
2. Explain the implementation plan.
3. List the files that will be created or modified.

After implementing a task:

1. Run `dotnet restore` if required.
2. Run `dotnet build`.
3. Run tests when applicable.
4. Fix build errors before completing the task.
5. Summarize all changes.
6. List any assumptions or remaining work.

Do not claim code has been built or tested unless it has actually been run.

---

## Scope

Only implement the requested task.

Do not implement unrelated features or redesign the application architecture unless explicitly instructed.