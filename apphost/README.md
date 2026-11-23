# Local orchestration (Aspire TypeScript AppHost)

Runs the local dev stack: `db` (Azurite, plain node process, no docker), `api`, `support`, `batch` (Azure Functions), `admin`, `app`, `site`.

```bash
cd apphost
npm install
aspire run
```

The dashboard URL is printed on start. `api` then `support` start one after the other (shared projects, concurrent builds lock `obj/`). `batch` starts automatically with mocks on; without mocks it would send real mails, so it is started on demand from the dashboard.

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

Each parameter has a description (what it is for, format, which mock makes it unnecessary) shown in the dashboard when
you enter its value; the source is the `descriptions` map in `apphost.mts`.

A resource whose parameter has no value waits (state "Waiting") until it is provided. Use a dummy value for services
you do not exercise locally (a mail sender you do not use, for example), so that local runs never send real emails.

## Optional mocks of external services

By default the real services are used (through the parameters above). Set `MOCKS` to replace some of them with a small
local node server (`mocks/server.mjs`, started as the `mocks` resource, port 7090, no docker):

```bash
MOCKS=all aspire run                 # bash
```

```powershell
$env:MOCKS = 'all'; aspire run       # PowerShell
```

`MOCKS` is `all` or a comma-separated list:

| Value | Replaces | Mock behaviour |
|---|---|---|
| `weather` | Open-Meteo (`api`, `batch`) | one fixed forecast, same for any coordinates: frost on the next 3 days (about -2 / -4 / -1 °C minimum), mild otherwise |
| `scaleway` | Scaleway TEM (`batch`) | stores the mail, answers 200 |
| `mail` | custom send-mail API (`batch`) | stores the mail, answers 200 |
| `maps` | Azure Maps geocoding and time zone (`admin`, server side) | same place (Paris, `Europe/Paris`) for any address or coordinates |
| `smtp` | SMTP (`batch` "smtp" channel, `admin`) | fake SMTP server on `localhost:465` (implicit TLS, any login accepted), mails shown in the inbox as `smtp` |

Sent mails are listed at http://localhost:7090/ (link in the dashboard); open one to see its headers and body. `smtp` mails show the real headers received (DKIM-Signature, List-Unsubscribe...) plus a raw message link; `scaleway` and `send-mail` mails show headers rebuilt from the API request (the real provider builds the final ones; `send-mail` carries none beyond to/subject). A mocked service needs no parameter: its URL and
credentials are set by the AppHost. With any mock on, `INTERNAL_API_KEY` and `PERIOD_IN_DAYS` also get local values.
`batch` starts automatically (its mails go to the mock inbox). The same mails are available as JSON at
http://localhost:7090/api/mails (`?to=<address>` to filter, `DELETE` to clear): the e2e happy path (`e2e/`, `npm run test:local`) reads them there.

Without `smtp`, `batch` and `admin` get placeholder SMTP values when another mock is on, so they start but sending mail
by SMTP fails (admin shows "SMTP configuration is missing").

### SMTP mock: development certificate

`batch` and `admin` connect with MailKit (port 465, implicit TLS) and validate the server certificate normally, so the
fake SMTP server must present a certificate they trust for `localhost`: the ASP.NET Core development certificate.
`MOCKS=smtp` (or `all`) therefore needs it to exist and be trusted. The AppHost checks this at startup and stops with
these steps if it is not the case:

```bash
dotnet dev-certs https --check --trust   # exists, valid and trusted? (exit code 0 = yes)
dotnet dev-certs https --trust           # create it if missing and trust it (Windows asks to confirm)
# if it exists but is not trusted or not exportable:
dotnet dev-certs https --clean
dotnet dev-certs https --trust
```

The AppHost then exports the certificate with a random password to `apphost/mocks/.cert/localhost.pfx` (gitignored) and
passes it to the mock server. The certificate is never committed. It expires after a year: rerun the commands above.
Port 465 must be free on `localhost`.

Limits of the `maps` mock: it covers the Azure Maps SDK calls made by `admin`'s server (geocoding, time zone). The map
widgets in the browser load the real Azure Maps web SDK and tiles, so they do not render with the mock key.
To let `admin` call another host, the AppHost sets an optional `AzureMapsEndpoint` setting that `admin`
reads (`admin/src/Services/AzureMapsEndpointOverride.cs`); when it is absent (production) the SDK calls Azure Maps as before.

The mocks only cover calls to external services: you still need locations in Azurite (sign up through the site) for `batch`
to have something to notify.

### DKIM keys

`batch` and `admin` load `dkim_private.pem` / `dkim_public.pem` to sign mails (DKIM) and unsubscribe tokens. The files in
the repo are placeholders (`CHANGE ME`: the pipelines deploy the real keys), which makes any mail with a signature fail
(`PemException: problem creating RSA private key`). With the `smtp` or `scaleway` mock on, the AppHost generates a
throwaway RSA key pair in `apphost/mocks/.cert/` (gitignored) and sets `DKIM_PRIVATE_KEY_PATH` (batch, admin) and
`DKIM_PUBLIC_KEY_PATH` (admin) to it. These settings are optional overrides read by the services; when unset (production, or
runs without these mocks) the services read the `.pem` files next to the app as before. Real SMTP/Scaleway runs
need your real key in those files.
