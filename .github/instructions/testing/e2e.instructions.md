---
applyTo: "e2e/**"
---

# End-to-End Tests Instructions

Full documentation (projects, commands, local stack, parameters, happy path steps): `e2e/README.md`. Keep it up to date when changing the tests.

## Tooling

- **Framework:** Playwright
- **Language:** TypeScript
- **Config:** `e2e/playwright.config.ts`
- **Tests location:** `e2e/tests/prod/` (deployed sites), `e2e/tests/local/` (local Aspire stack with mocks); helpers in `e2e/support/`

## Commands (run from `e2e/` directory)

```bash
npm install
npx playwright install chromium
npm run test:prod          # deployed sites (what the pipeline runs; `npm test` is an alias)
npm run test:local         # happy path, local stack must run: cd apphost; MOCKS=all aspire run
npm run test:local:fr      # French site only
npm run test:local:en      # English site only
```

Hemisphere of the local test location: `E2E_HEMISPHERE=N|S|auto` (default `auto`, the hemisphere where it is winter now).

## CI Pipeline

`e2e/azure-pipelines.yml` runs `npm run test:prod` (the `prod` project) against the deployed environment, weekly. The local projects run on a developer machine only.

## Conventions

- One project per target: `prod`, `local-fr`, `local-en` (`local-setup` waits for the local stack)
- The local projects share one database, one mail inbox and one batch configuration: one worker, never parallel
- Local URLs (fixed Aspire ports) are in `e2e/support/local.ts`; keep them in sync with `apphost/apphost.mts`
- Read mails from the mock inbox JSON API (`e2e/support/inbox.ts`), poll with `expect.poll`, never fixed waits (except to assert that nothing arrives)
- Each run uses a new e-mail and city: no dependency on a clean database
- Do not commit `playwright-report/` or `test-results/` directories (gitignored)
