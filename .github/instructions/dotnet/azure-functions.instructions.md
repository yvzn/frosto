---
applyTo: "api/**,batch/**,weather-forecast/**"
---

# C# Azure Functions Instructions (api, batch, weather-forecast)

## Runtimes & Tooling

- **Target framework:** `net9.0` for all three projects
- **Azure Functions:** isolated worker model, v4 (`AzureFunctionsVersion=v4`)
- **.NET SDK required:** 9.0.x (9.0.101 used in CI)
- **C# language version:** 13.0
- **Nullable reference types:** enabled — always handle nullability correctly
- `batch/` has **`TreatWarningsAsErrors=true`** — every compiler warning is a build failure

## Project Structure

```
api/
├── api.csproj          # Project file; references ../weather-forecast/
├── api.sln
├── host.json           # Azure Functions host config
├── local.settings.json # Local only — NEVER commit; gitignored
└── src/                # Function trigger classes

batch/
├── batch.csproj        # References ../weather-forecast/
├── batch.sln
├── host.json
├── local.settings.json # NEVER commit
├── dkim_private.pem    # Placeholder only; real key injected by CI
├── Program.cs          # DI/host setup
├── LocationLoop2.cs    # Timer trigger: loops over subscriber locations
├── NotifyAtLocation2.cs
├── SendNotification2.cs
├── Health.cs           # Health-check HTTP trigger
├── Models/
└── Services/

weather-forecast/
├── weather-forecast.csproj   # Shared library; no Azure Functions dependency
├── Forecast.cs
├── ForecastBuilder.cs        # Calls Open-Meteo API
├── ILocation.cs
├── OpenMeteoApiResult.cs
└── RequestUri.cs
```

## Build Commands

Always build from the solution to ensure the shared `weather-forecast` library is included:

```bash
# api
dotnet restore api/api.sln
dotnet build api/api.sln --configuration Release

# batch
dotnet restore batch/batch.sln
dotnet build batch/batch.sln --configuration Release
# batch: fix ALL warnings — TreatWarningsAsErrors=true

# weather-forecast (built transitively via api.sln / batch.sln)
# If editing standalone:
dotnet build weather-forecast/weather-forecast.csproj
```

## Dependencies & Conventions

- `api` and `batch` both reference `weather-forecast` via `<ProjectReference>` — changes to `weather-forecast` affect both
- `MailKit` is used for email sending in `batch` and `admin`
- `Polly.Core` is used for resilience/retry in `batch`
- Dependency injection is configured in `batch/Program.cs`
- `local.settings.json` holds connection strings for local dev (Azure Tables via Azurite) — never commit
- `dkim_private.pem` in `batch/` is a placeholder; the real DKIM key is downloaded by the CI secure-file task
- Always use `async`/`await` properly; avoid blocking `.Result` or `.Wait()`
- Follow existing patterns: functions are in root or `Services/` directory, models in `Models/`
