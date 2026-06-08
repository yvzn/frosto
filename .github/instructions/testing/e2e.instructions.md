---
applyTo: "e2e/**"
---

# End-to-End Tests Instructions

## Tooling

- **Framework:** Playwright
- **Language:** TypeScript
- **Config:** `e2e/playwright.config.ts`
- **Tests location:** `e2e/tests/`

## Commands (run from `e2e/` directory)

```bash
npm install                    # Install dependencies first
npx playwright install         # Install browser binaries — required on first setup or in CI
npx playwright test            # Run all tests
npx playwright test --headed   # Run with visible browser (debug)
npx playwright show-report     # View HTML test report
```

## CI Pipeline

The `e2e/azure-pipelines.yml` runs Playwright tests against the deployed environment (not localhost). Tests in this directory validate the live site endpoints.

## Conventions

- Tests are in `e2e/tests/` directory
- Target URLs are configured in `playwright.config.ts` via environment variables
- Do not commit `playwright-report/` or `test-results/` directories (gitignored)
