# NSNS Waiver Application

## Overview

The NSNS Waiver Application is an ASP.NET Core Razor Pages web application that allows customers to complete and submit an online liability waiver for an event.

For business requirements and application behaviour, see **PROJECT.md**.

For implementation standards and AI coding instructions, see **AGENTS.md**.

---

# Technology Stack

- ASP.NET Core Razor Pages
- .NET 10
- MySQL
- Dapper
- MySqlConnector
- Bootstrap
- Docker
- Railway

---

# Prerequisites

Install the following software before building the application:

- .NET 10 SDK
- Git
- MySQL 8.0 or later
- Docker Desktop (optional for local development)
- Visual Studio 2022 Community or Visual Studio Code

---

# Getting Started

Clone the repository:

```bash
git clone <repository-url>
cd NSNS-Waiver
```

Restore NuGet packages:

```bash
dotnet restore
```

Build the project:

```bash
dotnet build
```

Run the application:

```bash
dotnet run
```

The application will display the local development URL in the console.

---

# Configuration

## Database Connection

Configure the database connection using the `Default` connection string.

For local development, use .NET User Secrets or environment variables.

Example:

```text
ConnectionStrings__Default=<your-mysql-connection-string>
```

Do not commit database credentials to source control.

---

# Database

The application uses MySQL.

The database schema is created using SQL migration scripts located in the project.

Primary tables:

- waiver_submissions
- waiver_family_members
- email_outbox

The application does not create or modify the schema automatically during normal execution.

---

# Running Database Migrations

Create an empty MySQL database.

Run the SQL migration scripts in order.

Example:

```
001_create_waiver_tables.sql
```

Future migrations should use incrementing numbers.

---

# Project Structure

```text
NSNS-Waiver/

├── AGENTS.md
├── PROJECT.md
├── README.md
├── Content/
│   └── waiver-agreement.html
├── Data/
│   ├── Migrations/
│   └── Repositories/
├── Models/
├── Services/
├── Pages/
├── wwwroot/
└── Program.cs
```

---

# Development Guidelines

- Read `PROJECT.md` before implementing new features.
- Follow all coding standards in `AGENTS.md`.
- Keep business logic in services.
- Keep database access in repositories.
- Use asynchronous programming.
- Use dependency injection.
- Use parameterized SQL.
- Do not use Entity Framework Core.

---

# Running Tests

Run all automated tests:

```bash
dotnet test
```

If integration tests require a MySQL database, configure the appropriate test connection string before running them.

---

# Docker

Docker support is provided for deployment consistency.

For everyday development, Docker is optional.

When required:

```bash
docker compose up --build
```

---

# Deployment

The application is intended to be deployed to Railway using Docker.

Deployment configuration should be kept separate from application code whenever possible.

---

# Security

- Validate all user input on the server.
- Use HTTPS in production.
- Never commit secrets or credentials.
- Never log connection strings.
- Avoid logging personally identifiable information unless required for troubleshooting.

---

# Contributing

Before submitting changes:

1. Restore packages.

```bash
dotnet restore
```

2. Build the project.

```bash
dotnet build
```

3. Run tests.

```bash
dotnet test
```

4. Review your changes.

5. Commit using a clear, descriptive commit message.

---

# Additional Documentation

| Document | Purpose |
|----------|---------|
| `PROJECT.md` | Business requirements and application behaviour |
| `AGENTS.md` | Coding standards, architecture, and AI development instructions |
| `README.md` | Project setup, build, configuration, and development guide |