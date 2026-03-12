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
| `ConnectionStrings__DefaultConnection` | SQL Server connection string | `Server=localhost;Database=LSMS;Trusted_Connection=True;` |
| `Jwt__SigningKey` | Secret key for signing JWTs | `a-long-random-secret-string` |

### Setting Environment Variables

**Windows (Command Prompt):**
```cmd
set ConnectionStrings__DefaultConnection="Server=...;Database=LSMS;..."
set Jwt__SigningKey="your-secret-key"
```

**Windows (PowerShell):**
```powershell
$env:ConnectionStrings__DefaultConnection = "Server=...;Database=LSMS;..."
$env:Jwt__SigningKey = "your-secret-key"
```

**macOS / Linux:**
```bash
export ConnectionStrings__DefaultConnection="Server=...;Database=LSMS;..."
export Jwt__SigningKey="your-secret-key"
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
dotnet run --project src/presentation/Web.BlazorServer
```

The app will be available at `https://localhost:5001` (or the port shown in terminal output).

---

## Database Migrations

Always run migration commands from the **solution root**.

### Apply existing migrations (first-time setup)

```bash
dotnet ef database update \
  --project src/infrastructure/database/Database.MsSql \
  --startup-project src/presentation/Web.BlazorServer
```

### Add a new migration

```bash
dotnet ef migrations add YourMigrationName \
  --project src/infrastructure/database/Database.MsSql \
  --startup-project src/presentation/Web.BlazorServer
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
      "Default": "Information"
    }
  },
  "Jwt": {
    "Issuer": "your-project",
    "Audience": "lsms-users",
    "ExpiryMinutes": 480
  }
}
```

Never put secrets here.

---

## Troubleshooting

| Problem | Check |
|---|---|
| App won't start | Are environment variables set? |
| Database connection error | Is SQL Server running? Is the connection string correct? |
| Migration errors | Are you running from solution root? Does the database exist? |
| JWT auth failing | Is `Jwt__SigningKey` set and matching on all instances? |

---

## Next Step

➡️ Read [09 — Code Conventions](./09-conventions.md) if you haven't already — it covers the rules for writing code in this project.

Or go back to the [index](../README.md) and pick a topic relevant to what you're building.
