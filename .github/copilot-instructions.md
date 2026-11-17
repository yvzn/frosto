# Frosto — Copilot Agent Instructions

> Trust these instructions first. Only search the repository if the information here is incomplete or appears incorrect.

## Project Overview

**Frosto** (alertegelee.fr / frostalert.net) is a free, open-source frost-alert notification service. Users subscribe with their location; the system fetches weather forecasts and sends email notifications when sub-zero temperatures are forecast.

**Repository size:** medium (~15 top-level directories, ~50 source files across all projects).
**Languages:** C# 47%, HTML 38%, Vue/TypeScript/JS 13%, PowerShell 1%.
**Deployment platform:** Microsoft Azure (Azure Functions, Azure Static Web Apps / SSH deploy, Azure Tables storage).
**CI/CD:** Azure Pipelines (one `azure-pipelines.yml` per sub-project; no GitHub Actions workflows exist in this repo).

---

## Repository Layout

```
frosto/
├── api/                  # Azure Function App (C# .NET 9, isolated worker) — subscription HTTP API
│   ├── api.csproj        # Target: net9.0, AzureFunctionsVersion v4
│   ├── api.sln
│   ├── src/              # Function source files
│   └── azure-pipelines.yml
├── batch/                # Azure Function App (C# .NET 9, isolated worker) — weather fetch & notifications
│   ├── batch.csproj      # Target: net9.0, TreatWarningsAsErrors=true
│   ├── batch.sln
│   ├── Models/
│   ├── Services/
│   ├── LocationLoop2.cs, NotifyAtLocation2.cs, SendNotification2.cs, Health.cs
│   └── azure-pipelines.yml
├── weather-forecast/     # Shared C# library (net9.0) — Open-Meteo API client, referenced by api & batch
│   └── weather-forecast.csproj
├── entities/             # Shared C# entity models
│   ├── src/              # Production project and source
│   └── tests/            # Separate entity test project
├── admin/                # ASP.NET Core web app (C# .NET 10) — admin tooling
│   ├── admin.slnx
│   ├── src/              # admin.csproj, application source, configuration, and web assets
│   ├── tests/            # Separate admin test project
│   └── azure-pipelines.yml
├── app/                  # Vue 3 + TypeScript companion app (Vite 8, base path /app/)
│   ├── package.json      # Node >=20.19 || >=22.12
│   ├── vite.config.ts
│   ├── eslint.config.ts, .oxlintrc.json, .oxfmtrc.json
│   ├── src/
│   └── azure-pipelines.{fr,en}.yml (reference azure-pipelines.template.yml)
├── site-11ty/            # Static landing pages, French and English (Eleventy + Vite, Bootstrap 5)
│   ├── package.json
│   ├── eleventy.config.js, vite.config.js
│   ├── src/              # Templates (src/content/<site>/*.njk), layouts, data, assets, page scripts
│   └── azure-pipelines.yml, azure-pipelines-checks.yml
├── site-maintenance/     # Maintenance placeholder page
├── site-redirect/        # HTTP redirect page
├── e2e/                  # End-to-end tests (Playwright)
│   ├── playwright.config.ts
│   └── tests/
├── db/                   # Database / storage scripts
├── logic-app/            # Azure Logic App definition
├── tools/                # Miscellaneous tooling scripts
├── support/              # Support / contact tooling
└── .editorconfig         # Root editor config (tabs, UTF-8)
```

**Key dependency:** `weather-forecast/` is a shared library referenced by both `api/` and `batch/` via `<ProjectReference>`. Always build from the solution file (`.sln` / `.slnx`) or ensure `weather-forecast` is restored first.

---

## Build & Validation — by Sub-Project

### C# Projects (api, batch, admin)

**Runtime:** .NET SDK
- `api/` and `batch/`: .NET 9 (`net9.0`) — requires .NET SDK 9.0.x
- `admin/`: .NET 10 (`net10.0`) — `admin/global.json` pins SDK to `10.0.0` (rollForward: latestMinor)

**Commands (run from repo root or the sub-project directory):**

```bash
# Restore + build (always restore before building)
dotnet restore api/api.sln
dotnet build api/api.sln --configuration Release

dotnet restore batch/batch.sln
dotnet build batch/batch.sln --configuration Release
# NOTE: batch has TreatWarningsAsErrors=true — all compiler warnings fail the build

dotnet restore admin/admin.slnx
dotnet build admin/admin.slnx --configuration Release
```

**Local run (Azure Functions):** requires `local.settings.json` (gitignored) and Azurite for local storage emulation.
`local.settings.json` is never published (`CopyToPublishDirectory=Never`).

**CI pipeline steps (Azure Pipelines):**
- `api`: `dotnet build` then `dotnet publish --arch x64 --os win --no-self-contained`
- `batch`: `dotnet publish --configuration Release` (Linux target)
- `admin`: separate pipeline with Docker/SSH deploy steps

---

### Vue Companion App (`app/`)

**Runtime:** Node.js ≥20.19 or ≥22.12. Package manager: npm.

```bash
cd app
npm install          # Always run before building
npm run type-check   # vue-tsc type checking (must pass)
npm run lint         # oxlint + eslint (auto-fixes applied)
npm run build        # type-check + vite build (outputs to app/dist/)
```

- Linting: oxlint (`.oxlintrc.json`) + ESLint (`eslint.config.ts`) + oxfmt (`.oxfmtrc.json`)
- Built with Vite 8; base path is `/app/`
- Environment variables are injected via `.env.production` generated by `build-env-production.ps1` in CI — not required for local dev (`.env` file is committed)

---

### Static Sites (`site-11ty/`)

**Runtime:** Node.js (see `site-11ty/.nvmrc`). Package manager: npm. Builds alertegelee.fr (`dist/fr/`) and frostalert.net (`dist/en/`).

```bash
cd site-11ty
npm ci         # Always run before building
npm test       # Self-tests (node:test)
npm run build  # Eleventy + Vite + PurgeCSS for both sites (outputs to dist/fr/, dist/en/)
npm run dev    # Live reload: FR http://localhost:5180/, EN http://localhost:5181/
```

- The build fails if the contact email address appears unobfuscated in the output
- Endpoint URLs come from environment variables (`VITE_*`), mapped explicitly in `azure-pipelines.yml`
- See `site-11ty/README.md` for templates, front matter, CSP and the live check

---

### E2E Tests (`e2e/`)

```bash
cd e2e
npm install
npx playwright install   # Install browser binaries (required first time)
npx playwright test
```

---

## CI/CD Checks (Azure Pipelines)

Each sub-project has its own `azure-pipelines.yml`. The API, batch, admin, and support pipelines also trigger on changes to `entities/src`. There are **no GitHub Actions workflows**.

| Sub-project | Pipeline file | Key validation |
|-------------|--------------|----------------|
| `api/` | `api/azure-pipelines.yml` | `dotnet build --configuration Release` |
| `batch/` | `batch/azure-pipelines.yml` | `dotnet publish` (warnings = errors) |
| `admin/` | `admin/azure-pipelines.yml` | `dotnet build` |
| `app/` | `app/azure-pipelines.template.yml` | `npm run build` (type-check + vite) |
| `site-11ty/` | `site-11ty/azure-pipelines.yml` (deploy), `site-11ty/azure-pipelines-checks.yml` (PRs) | `npm test`, `npm run build` |
| `e2e/` | `e2e/azure-pipelines.yml` | Playwright tests |

**To replicate CI locally before opening a PR:**
1. For C# changes: `dotnet build <project>.sln --configuration Release`
2. For `app/` changes: `cd app && npm ci && npm run build`
3. For `site-11ty/` changes: `cd site-11ty && npm ci && npm test && npm run build`
4. For shared entity changes: `dotnet test entities/tests/entities-tests.csproj --configuration Release`

---

## Key Conventions

- Issues labeled `sample` (prefixed `[SAMPLE 1]`, `[SAMPLE 2]`) are onboarding references — never close, implement, or modify them.
- `batch/` has `TreatWarningsAsErrors=true` — fix all warnings before committing.
- `local.settings.json` files are gitignored and must never be committed.
- `dkim_private.pem` in `batch/` and `admin/src/` contains a placeholder (not real key); real key is injected by CI secure file task.
- The `weather-forecast/` library is shared — changes there affect both `api/` and `batch/`.
- C# nullable reference types are enabled (`<Nullable>enable</Nullable>`) across all projects.

---

## Keeping These Instructions Up to Date

When completing a task, check whether any of the following changed as a direct result of your work. If so, update the relevant instruction files in `.github/copilot-instructions.md` and/or `.github/instructions/**/*.instructions.md` in the same PR:

- **Runtime or SDK version** — e.g. `TargetFramework` bumped in a `.csproj`, Node.js `engines` field changed in `package.json`, SDK version in `global.json` updated, major dependency version upgrade (Vite, Vue, .NET, etc.)
- **Project or directory structure** — e.g. a new sub-project directory added, a source folder renamed or reorganised, a new significant config file introduced

Do **not** update instruction files for routine code changes (new features, bug fixes, refactors) that leave the runtime versions and project structure unchanged.
