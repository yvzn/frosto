---
applyTo: "admin/**"
---

# ASP.NET Core Admin App Instructions

## Runtime & Tooling

- **Target framework:** `net10.0` (NOT net9.0 — different from the Azure Functions projects)
- **.NET SDK:** pinned to `10.0.0` via `admin/global.json` (rollForward: latestMinor)
- **Project type:** `Microsoft.NET.Sdk.Web` — ASP.NET Core Razor Pages + MVC
- **Nullable reference types:** enabled
- **Implicit usings:** enabled

## Project Structure

```
admin/
├── admin.csproj        # net10.0, no Azure Functions dependency
├── admin.slnx          # Solution file (new .slnx format)
├── global.json         # Pins .NET SDK 10.0.0
├── Program.cs          # App startup and DI configuration
├── appsettings.json    # Non-secret config; secrets in user-secrets / env vars
├── Controllers/        # MVC API controllers
├── Models/             # View models and domain models
├── Pages/              # Razor Pages
├── Services/           # Business logic / service layer
├── wwwroot/            # Static web assets
├── timezones.json      # Timezone data (large, ~64 KB) — do not modify manually
├── dkim_private.pem    # Placeholder private key; real key injected by CI
└── dkim_public.pem     # Public key (committed)
```

## Build Commands

```bash
cd admin
# Use the .slnx solution file
dotnet restore admin.slnx
dotnet build admin.slnx --configuration Release

# Run locally
dotnet run --project admin.csproj
```

## Key Dependencies

- `Azure.Data.Tables` — Azure Table Storage client
- `Azure.Maps.Search`, `Azure.Maps.TimeZones` — Azure Maps integration (beta packages)
- `MailKit` — email sending
- `System.IdentityModel.Tokens.Jwt` — JWT handling
- `Microsoft.Extensions.Azure` — Azure service registration

## Conventions

- Services are registered via DI in `Program.cs`
- `appsettings.json` contains non-secret config; secrets use environment variables or user-secrets locally
- `timezones.json` is large data file — never regenerate or overwrite manually
- Use Razor Pages for UI, MVC controllers for API endpoints
- `dkim_private.pem` is a placeholder — the real key is injected by CI
