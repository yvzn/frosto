# Local orchestration (Aspire TypeScript AppHost)

Runs the local dev stack: `db` (Azurite, plain node process, no docker), `api`, `support`, `batch` (Azure Functions), `admin`, `app`, `site`.

```bash
cd apphost
npm install
aspire run
```

The dashboard URL is printed on start. `api` then `support` start one after the other (shared projects, concurrent builds lock `obj/`). `batch` is started on demand from the dashboard (it needs mail settings).

| Resource | URL |
|---|---|
| api | http://localhost:7071 (`/api/healthz`) |
| batch | http://localhost:7072 |
| support | http://localhost:7073 |
| admin | http://localhost:5176 |
| app | http://localhost:5175 |
| site fr / en | http://localhost:5180 / http://localhost:5181 |
| Azurite blob / queue / table | 10000 / 10001 / 10002 |

## Configuration: Aspire is the source of truth

> **Local settings files are ignored when running with Aspire.** Do not rely on `local.settings.json`
> (functions), `env/<site>/.env` / `app/.env` (Vite) or `appsettings.*.json` secrets: the AppHost sets
> every variable the services read. Values Aspire sets are real process environment variables, which
> take precedence over those files. Do not put settings in them for Aspire runs; add them to `apphost.mts`.

The files stay in place and unchanged, so running a project on its own (outside Aspire) and production
(Azure app settings, pipeline variables) are not affected.

What `apphost.mts` wires:

- **Cross-service URLs** (fixed local ports): `SITE_URL`, `SITE_FR_URL`, `SITE_EN_URL`, `SiteFrUrl`, `SiteEnUrl`,
  `UNSUBSCRIBE_URL`, `WEBSITE_HOSTNAME`, `INTERNAL_PROTOCOL`, and all `VITE_*_URL` of `app` and `site`.
- **Storage**: `ALERTS_CONNECTION_STRING`, `AzureWebJobsStorage`, `ConnectionStrings__Alerts` -> `UseDevelopmentStorage=true` (Azurite).
- **Functions runtime**: `FUNCTIONS_WORKER_RUNTIME=dotnet-isolated`.
- **Aspire parameters** for everything environment-specific or secret (below).

### Parameters

Set once (stored in the AppHost user-secrets, outside the repo), or type them in the dashboard when a resource is waiting for a value:

```bash
aspire secret set Parameters:open-meteo-api-url "https://api.open-meteo.com/v1/forecast?latitude={0}&longitude={1}&timezone={2}"
aspire secret set Parameters:smtp-password "..."
```

| Parameter | Secret | Used by | Env variable |
|---|---|---|---|
| `open-meteo-api-url` | | api, batch | `OPEN_METEO_API_URL` (format string `{0}` lat, `{1}` lon, `{2}` timezone) |
| `period-in-days` | | batch | `PERIOD_IN_DAYS` |
| `internal-api-key` | yes | batch | `INTERNAL_API_KEY` |
| `send-mail-api-url` | | batch | `SEND_MAIL_API_URL` |
| `smtp-url`, `smtp-login` | | batch | `SMTP_URL`, `SMTP_LOGIN` |
| `smtp-password` | yes | batch | `SMTP_PASSWORD` |
| `scaleway-api-url`, `scaleway-project-id` | | batch | `SCALEWAY_API_URL`, `SCALEWAY_PROJECT_ID` |
| `scaleway-api-key` | yes | batch | `SCALEWAY_API_KEY` |
| `azure-maps-connection-string` | yes | admin | `ConnectionStrings__AzureMaps` |
| `admin-smtp-connection-string` | yes | admin | `ConnectionStrings__Smtp` |

A resource whose parameter has no value waits (state "Waiting") until it is provided. Use a dummy value for services
you do not exercise locally (a mail sender you do not use, for example), so that local runs never send real emails.
