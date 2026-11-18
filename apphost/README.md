# Local orchestration (Aspire TypeScript AppHost)

Runs the local dev stack: `db` (Azurite, plain node process, no docker), `api`, `batch`, `support` (Azure Functions), `admin`, `app`, `site`.

```bash
cd apphost
npm install
aspire run
```

Dashboard URL is printed on start. Functions start one after another (shared projects, concurrent builds lock `obj/`).
Storage settings point to Azurite; other settings (SMTP, Scaleway, URLs...) still come from each project's gitignored `local.settings.json`.
Health endpoint of functions: `/api/healthz`.
