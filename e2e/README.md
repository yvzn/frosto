# End-to-end tests

Playwright tests (TypeScript) in two flavours:

| Project | Runs against | What it tests |
|---|---|---|
| `prod` | the deployed sites (alertegelee.fr, frostalert.net) | the sign-up form, in French and English (smoke tests) |
| `local-fr`, `local-en` | the local stack (`apphost`) with every external service mocked | the full happy path, from sign-up to unsubscription, on the French or English site |
| `local-setup` | the local stack | runs first for the local projects: waits until every resource answers |

## Setup

Requires Node.js 24 (same as the pipeline). From this folder:

```bash
npm install
npx playwright install chromium     # browser binaries, once (npm run install:browsers for all browsers + OS deps)
```

## Prod smoke tests

```bash
npm test                 # same as npm run test:prod
```

They sign up `test@example.com` on the live sites. The weekly pipeline (`azure-pipelines.yml`, Monday 2am, `main`) runs `npm run test:prod` (explicitly, not `npm test`); it never runs the local projects.

## Local happy path

### 1. Start the local stack with mocks

In `../apphost` (see its README for the prerequisites: Aspire CLI, Azure Functions Core Tools, .NET SDK):

```bash
MOCKS=all aspire run                       # bash
```

```powershell
$env:MOCKS = 'all'; aspire run             # PowerShell
```

`MOCKS=all` replaces Open-Meteo, the mail providers, SMTP and Azure Maps with a local mock server (http://localhost:7090), and makes `batch` start automatically. The SMTP mock needs the trusted ASP.NET development certificate:

```bash
dotnet dev-certs https --check --trust     # exit code 0: ready
dotnet dev-certs https --trust             # otherwise
```

Leave the stack running. The first start builds every project and takes a few minutes: the tests wait for it (up to 4 minutes per resource).

### 2. Run the tests

In another terminal, from this folder:

```bash
npm run test:local       # French then English
npm run test:local:fr    # French site only
npm run test:local:en    # English site only
```

If `local-setup` fails with "is not up", the stack is not started, not started with `MOCKS=all`, or still building.

### Parameters

**Language** — the project: `local-fr` (alertegelee.fr pages, French mails) or `local-en` (frostalert.net pages, English mails). The `test:local*` scripts pick them; with Playwright directly: `npx playwright test --project=local-en`.

**Hemisphere** — the `E2E_HEMISPHERE` environment variable: `N`, `S` or `auto` (default).

`batch` only sends alerts in the winter months of the location's hemisphere (`batch/Services/AppSettings.cs`):

| Hemisphere | Test location | Winter months |
|---|---|---|
| `N` | Nantes, France (Europe/Paris) | November to May |
| `S` | Hobart, Australia (Australia/Hobart) | May to November |

`auto` picks the hemisphere where it is winter now (north from November to May, south from June to October). An explicit hemisphere outside its winter months fails the test immediately: no alert would be sent.

```powershell
$env:E2E_HEMISPHERE = 'S'; npm run test:local:en    # PowerShell
Remove-Item Env:E2E_HEMISPHERE                       # back to auto
```

```bash
E2E_HEMISPHERE=S npm run test:local:en              # bash (only for this command)
```

Both parameters combine: one project and one hemisphere per run.

### What the happy path does

One test (`tests/local/happy-path.spec.ts`), one step per stage, each using the state the previous one left:

1. **site** — sign up with a new e-mail and city
2. **admin** — validate the sign-up (coordinates of the chosen hemisphere), then mark the location as UAT: a debug build of `batch` only notifies UAT locations
3. **site** — check the subscription; **admin** — send the confirmation mail; the mail is received
4. **admin** — rebuild the batches (1-day cycle); the test reads the slot of the location in Azurite (`locationbatch` table)
5. **batch** — run that slot (`BatchCronDebug`); the frost alert is received (the weather mock is always frosty), with its `List-Unsubscribe` headers
6. **mail** — unsubscribe with the link of the alert (site confirmation form)
7. **admin** — process the unsubscribe request, unsubscribe the user, send the confirmation mail; the mail is received; the request is marked completed
8. **batch** — run the same slot again: no new alert

Not covered yet: one-click unsubscription from the `List-Unsubscribe` header (RFC 8058), the app (`../app`).

### How it reads mails and data

- **Mails**: the mock server stores every mail sent by `admin` (SMTP) and `batch` (Scaleway API). The tests read them as JSON: `GET http://localhost:7090/api/mails?to=<address>` (`DELETE` clears the inbox). The same mails are browsable at http://localhost:7090/.
- **Data**: Azurite (`UseDevelopmentStorage=true`), read with `@azure/data-tables` (`support/azurite.ts`).

### Side effects on your local data

- Each run creates a new user (`e2e.<random>@example.com`) and city (`Testville <random>`): runs do not depend on each other or on a clean database.
- Each run **rebuilds all batches** with a 1-day cycle.
- What the admin unsubscription keeps of the test user stays in Azurite.
- Other UAT locations of your local data in the same slot also get (mock) alerts.

### Debugging

```bash
npx playwright test --project=local-fr --headed     # watch the browser
npx playwright test --project=local-fr --debug      # step through with the inspector
npx playwright show-trace test-results/<test folder>/trace.zip   # trace of a failed run
```

Every step is named in the report, so a failure tells which stage broke. The Aspire dashboard (URL printed by `aspire run`) has the logs of every resource; the mock inbox shows what was sent.

## Layout

```
playwright.config.ts     projects, workers (1: the local projects share data), reporters
tests/prod/              smoke tests on the deployed sites
tests/local/             global.setup.ts (stack health) and the happy path
support/local.ts         local URLs (fixed ports of apphost/apphost.mts), parameters, texts per language
support/inbox.ts         mock inbox client
support/azurite.ts       Azurite table reads
```

Local URLs are the fixed ports of `../apphost/apphost.mts`: keep `support/local.ts` in sync when they change. The winter months in `support/local.ts` mirror `batch/Services/AppSettings.cs`.

Reports: JUnit in `test-results/e2e-junit-results.xml` (published by the pipeline), list output in the console. `test-results/` and `playwright-report/` are gitignored.
