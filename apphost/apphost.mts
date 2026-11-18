// Aspire TypeScript AppHost: local orchestration of frosto (replaces ../orch).
// Only orchestrates what orch/config.json used to: db, api, support, batch, admin, app, site.
// Azurite runs as a plain node process (no docker).

import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { createBuilder } from './.aspire/modules/aspire.mjs';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const at = (...p: string[]) => path.join(root, ...p);

// Local Azurite. Other secrets/settings (SMTP, Scaleway, URLs...) still come from each
// project's own local.settings.json (gitignored), which `func host start` loads.
const devStorage = 'UseDevelopmentStorage=true';

const builder = await createBuilder();

// Azurite (blob 10000, queue 10001, table 10002) via ../db `npm run dev`
const db = await builder
  .addJavaScriptApp('db', at('db'), { runScriptName: 'dev' });

// Azure Functions: `func host start` on fixed ports.
// Started one after the other (each waits for the previous to be healthy): the three
// projects share ../entities and ../weather-forecast, concurrent builds lock their obj/ files.
const functions = [
  { name: 'api', dir: 'api', port: 7071 },
  { name: 'batch', dir: 'batch', port: 7072 },
  { name: 'support', dir: 'support', port: 7073 },
] as const;

let previous: Parameters<typeof db.waitFor>[0] = db;
for (const f of functions) {
  previous = await builder
    .addExecutable(f.name, 'func', at(f.dir), ['host', 'start', '--port', String(f.port)])
    .withHttpEndpoint({ port: f.port, targetPort: f.port, isProxied: false })
    .withHttpHealthCheck({ path: '/api/healthz' })
    .withEnvironment('FUNCTIONS_WORKER_RUNTIME', 'dotnet-isolated')
    .withEnvironment('AzureWebJobsStorage', devStorage)
    .withEnvironment('ALERTS_CONNECTION_STRING', devStorage)
    .waitFor(previous);
}

await builder
  .addProject('admin', at('admin', 'src', 'admin.csproj'), { launchProfileOrOptions: 'http' })
  .withEnvironment('ConnectionStrings__Alerts', devStorage)
  .waitFor(db);

await builder
  .addViteApp('app', at('app'), { runScriptName: 'dev' })
  .withArgs(['--port', '5175', '--strictPort'])
  .withHttpEndpoint({ port: 5175, targetPort: 5175, isProxied: false })
  .withHttpHealthCheck({ path: '/' });

await builder
  .addJavaScriptApp('site', at('site-11ty'), { runScriptName: 'dev' })
  .withHttpEndpoint({ port: 5180, targetPort: 5180, isProxied: false })
  .withHttpHealthCheck({ path: '/' });

await builder.build().run();
