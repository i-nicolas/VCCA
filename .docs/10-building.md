# 10 — Building & Running

[← Back to Index](../README.md)

---

## Prerequisites

Before you start, make sure you have installed:

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server (local or Docker)
- Your preferred IDE (Visual Studio, VS Code, or Rider)

---

## Environment Variables

These **must** be set before running the app. Never put them in `appsettings.json`.

| Variable | Description | Example |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | SQL Server connection string | `Server=localhost;Database=YourDatabase;Trusted_Connection=True;` |

### Setting Environment Variables

**Windows (Command Prompt):**
```cmd
set ConnectionStrings__DefaultConnection="Server=localhost;Database=YourDatabase;Trusted_Connection=True;"
```

**Windows (PowerShell):**
```powershell
$env:ConnectionStrings__DefaultConnection = "Server=localhost;Database=YourDatabase;Trusted_Connection=True;"
```

**macOS / Linux:**
```bash
export ConnectionStrings__DefaultConnection="Server=localhost;Database=YourDatabase;Trusted_Connection=True;"
```

**Using a `.env` file (not committed to git):**
Create a `.env` file in the solution root and load it with your preferred tool, or set the variables in your IDE's launch profile settings (`launchSettings.json` — also not committed).

---

## Build

From the solution root:

```bash
dotnet build
```

---

## Run

```bash
dotnet run --project Web.BlazorServer
```

The app will be available at `https://localhost:5001` (or the port shown in terminal output).

---

## Database Migrations

Always run migration commands from the **solution root**.

### Apply existing migrations (first-time setup)

```bash
dotnet ef database update \
  --project Database.MsSql \
  --startup-project Web.BlazorServer
```

### Add a new migration

```bash
dotnet ef migrations add YourMigrationName \
  --project Database.MsSql \
  --startup-project Web.BlazorServer
```

> 💡 **Name your migrations descriptively** — `AddOrderStatusColumn` is better than `Migration1`.

### Review before applying

After adding a migration, always open the generated file in `Database.MsSql/Migrations/` and verify it looks correct before running `database update`.

---

## What's in `appsettings.json`?

`appsettings.json` is committed to git. It contains only **non-sensitive** configuration:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

Never put secrets here. Connection strings and other sensitive values go in environment variables or a local `.env` file (not committed).

---

## Troubleshooting

| Problem | Check |
|---|---|
| App won't start | Is `ConnectionStrings__DefaultConnection` set? |
| Database connection error | Is SQL Server running? Is the connection string correct? |
| Migration errors | Are you running from the solution root? Does the database exist? |
| Login fails silently | Is the DB seeded with at least one user and role? Check `AppDbSeeding.cs`. |
| Cookie auth not working | Is `CookieAuthenticationDefaults.AuthenticationScheme` registered in `Program.cs`? |

---

## Next Step

➡️ Read [09 — Code Conventions](./09-conventions.md) if you haven't already — it covers the rules for writing code in this project.

Or go back to the [index](../README.md) and pick a topic relevant to what you're building.
