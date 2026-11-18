// Aspire TypeScript AppHost: local orchestration of frosto (replaces ../orch).
// Orchestrates: db (Azurite, plain node process, no docker), api, support, batch, admin, app, site.
//
// Aspire is the source of truth for local configuration: it wires the cross-service URLs and
// provides secrets through Aspire parameters (user-secrets). Production is untouched: it keeps
// reading Azure app settings / pipeline variables. See README.md.

import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { createBuilder } from './.aspire/modules/aspire.mjs';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const at = (...p: string[]) => path.join(root, ...p);

// --- Fixed local ports -------------------------------------------------------------------
const ports = { api: 7071, batch: 7072, support: 7073, admin: 5176, app: 5175, siteFr: 5180, siteEn: 5181 };
const origin = (port: number) => `http://localhost:${port}`;

const url = {
  api: origin(ports.api),
  support: origin(ports.support),
  siteFr: `${origin(ports.siteFr)}/`, // trailing slash expected: code appends page names
  siteEn: `${origin(ports.siteEn)}/`,
};

const devStorage = 'UseDevelopmentStorage=true'; // Azurite

const builder = await createBuilder();

// --- Parameters (values: `aspire secret set Parameters:<name> <value>`, or the dashboard) ---
const p = {
  openMeteoApiUrl: await builder.addParameter('open-meteo-api-url'),
  periodInDays: await builder.addParameter('period-in-days'),
  internalApiKey: await builder.addParameter('internal-api-key', { secret: true }),
  sendMailApiUrl: await builder.addParameter('send-mail-api-url'),
  smtpUrl: await builder.addParameter('smtp-url'),
  smtpLogin: await builder.addParameter('smtp-login'),
  smtpPassword: await builder.addParameter('smtp-password', { secret: true }),
  scalewayApiUrl: await builder.addParameter('scaleway-api-url'),
  scalewayProjectId: await builder.addParameter('scaleway-project-id'),
  scalewayApiKey: await builder.addParameter('scaleway-api-key', { secret: true }),
  azureMaps: await builder.addParameter('azure-maps-connection-string', { secret: true }),
  adminSmtp: await builder.addParameter('admin-smtp-connection-string', { secret: true }),
};

// --- Azurite (blob 10000, queue 10001, table 10002) via ../db `npm run dev` ---------------
const db = await builder.addJavaScriptApp('db', at('db'), { runScriptName: 'dev' });

// --- Azure Functions: `func host start` on fixed ports. api then support start one after the other:
// they share ../entities and ../weather-forecast, concurrent builds lock their obj/ files.
// batch is optional locally (needs mail settings) so it starts on demand, last.
const common = (r: ReturnType<typeof builder.addExecutable>, port: number) =>
  r
    .withHttpEndpoint({ port, targetPort: port, isProxied: false })
    .withHttpHealthCheck({ path: '/api/healthz' })
    .withEnvironment('FUNCTIONS_WORKER_RUNTIME', 'dotnet-isolated')
    .withEnvironment('AzureWebJobsStorage', devStorage)
    .withEnvironment('ALERTS_CONNECTION_STRING', devStorage);

const api = await common(builder.addExecutable('api', 'func', at('api'), ['host', 'start', '--port', String(ports.api)]), ports.api)
  .withEnvironment('SITE_URL', url.siteFr)
  .withEnvironment('SITE_EN_URL', url.siteEn)
  .withEnvironment('OPEN_METEO_API_URL', p.openMeteoApiUrl)
  .waitFor(db);

const support = await common(builder.addExecutable('support', 'func', at('support'), ['host', 'start', '--port', String(ports.support)]), ports.support)
  .withEnvironment('SITE_FR_URL', url.siteFr)
  .withEnvironment('SITE_EN_URL', url.siteEn)
  .waitFor(api);

await common(builder.addExecutable('batch', 'func', at('batch'), ['host', 'start', '--port', String(ports.batch)]), ports.batch)
  .withEnvironment('SITE_FR_URL', url.siteFr)
  .withEnvironment('SITE_EN_URL', url.siteEn)
  .withEnvironment('UNSUBSCRIBE_URL', `${url.support}/api/unsubscribe`)
  .withEnvironment('INTERNAL_PROTOCOL', 'http')
  .withEnvironment('WEBSITE_HOSTNAME', `localhost:${ports.batch}`)
  .withEnvironment('INTERNAL_API_KEY', p.internalApiKey)
  .withEnvironment('PERIOD_IN_DAYS', p.periodInDays)
  .withEnvironment('OPEN_METEO_API_URL', p.openMeteoApiUrl)
  .withEnvironment('SEND_MAIL_API_URL', p.sendMailApiUrl)
  .withEnvironment('SMTP_URL', p.smtpUrl)
  .withEnvironment('SMTP_LOGIN', p.smtpLogin)
  .withEnvironment('SMTP_PASSWORD', p.smtpPassword)
  .withEnvironment('SCALEWAY_API_URL', p.scalewayApiUrl)
  .withEnvironment('SCALEWAY_PROJECT_ID', p.scalewayProjectId)
  .withEnvironment('SCALEWAY_API_KEY', p.scalewayApiKey)
  .waitFor(support)
  .withExplicitStart();

// --- admin (ASP.NET, config binds env vars: ConnectionStrings__X, SiteEnUrl...) -----------
await builder
  .addProject('admin', at('admin', 'src', 'admin.csproj'), { launchProfileOrOptions: 'http' })
  .withEnvironment('ConnectionStrings__Alerts', devStorage)
  .withEnvironment('ConnectionStrings__AzureMaps', p.azureMaps)
  .withEnvironment('ConnectionStrings__Smtp', p.adminSmtp)
  .withEnvironment('SiteFrUrl', url.siteFr)
  .withEnvironment('SiteEnUrl', url.siteEn)
  .waitFor(db);

// --- app (Vue/Vite). Real env vars override app/.env ---------------------------------------
await builder
  .addViteApp('app', at('app'), { runScriptName: 'dev' })
  .withArgs(['--port', String(ports.app), '--strictPort'])
  .withHttpEndpoint({ port: ports.app, targetPort: ports.app, isProxied: false })
  .withHttpHealthCheck({ path: '/' })
  .withEnvironment('VITE_HEALTHCHECK_URL', `${url.api}/api/healthz`)
  .withEnvironment('VITE_SUPPORT_URL', `${url.support}/api/healthz`)
  .withEnvironment('VITE_CHECKSUBSCRIPTION_URL', `${url.support}/api/subscription/check`)
  .withEnvironment('VITE_WEATHERFORECAST_URL', `${url.api}/api/weather-forecast`)
  .withEnvironment('VITE_SITE_FR_URL', url.siteFr)
  .withEnvironment('VITE_SITE_EN_URL', url.siteEn);

// --- site (Eleventy + Vite; `npm run dev` serves fr on 5180 and en on 5181) ----------------
// Real env vars override site-11ty/env/<site>/.env.
await builder
  .addJavaScriptApp('site', at('site-11ty'), { runScriptName: 'dev' })
  .withHttpEndpoint({ port: ports.siteFr, targetPort: ports.siteFr, isProxied: false, name: 'fr' })
  .withHttpEndpoint({ port: ports.siteEn, targetPort: ports.siteEn, isProxied: false, name: 'en' })
  .withHttpHealthCheck({ path: '/', endpointName: 'fr' })
  .withEnvironment('VITE_SIGNUP_URL', `${url.api}/api/sign-up`)
  .withEnvironment('VITE_HEALTHCHECK_URL', `${url.api}/api/healthz`)
  .withEnvironment('VITE_SUPPORT_URL', `${url.support}/api/healthz`)
  .withEnvironment('VITE_CHECKSUBSCRIPTION_URL', `${url.support}/api/subscription/check`)
  .withEnvironment('VITE_UNSUBSCRIBE_URL', `${url.support}/api/unsubscribe`);

await builder.build().run();
