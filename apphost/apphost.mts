// Aspire TypeScript AppHost: local orchestration of frosto (replaces ../orch).
// Orchestrates: db (Azurite, plain node process, no docker), api, support, batch, admin, app, site.
//
// Aspire is the source of truth for local configuration: it wires the cross-service URLs and
// provides secrets through Aspire parameters (user-secrets). Production is untouched: it keeps
// reading Azure app settings / pipeline variables. See README.md.

import { execFileSync } from 'node:child_process';
import { randomBytes } from 'node:crypto';
import { mkdirSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { createBuilder } from './.aspire/modules/aspire.mjs';

const here = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(here, '..');
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

// --- Mocks of external services (optional) -------------------------------------------------
// MOCKS=all | comma list of: weather, scaleway, mail, maps   (unset = real services, as before)
//   weather  Open-Meteo forecast   scaleway  Scaleway TEM   mail  custom send-mail API   maps  Azure Maps (admin)
// A mocked service needs no parameter: its URL/credentials are set here.
//   smtp  fake SMTP server on localhost:465 (implicit TLS) for batch's "smtp" channel and admin;
//         needs the ASP.NET dev certificate to exist and be trusted (checked below)
const allMocks = ['weather', 'scaleway', 'mail', 'maps', 'smtp'];
const requested = (process.env.MOCKS ?? '').split(',').map((s) => s.trim().toLowerCase()).filter(Boolean);
const mocked = new Set(requested.includes('all') ? allMocks : requested);
for (const m of mocked) {
  if (!allMocks.includes(m)) throw new Error(`Unknown MOCKS entry "${m}". Expected: all, ${allMocks.join(', ')}`);
}
/**
 * The fake SMTP server must present a certificate that MailKit (batch, admin) trusts for "localhost":
 * the ASP.NET Core development certificate. Checks it exists and is trusted, then exports it (with a
 * random password, to the gitignored mocks/.cert/) for the mock server.
 */
function exportDevCertificate(): { pfx: string; password: string } {
  const dotnet = (...args: string[]) => execFileSync('dotnet', ['dev-certs', 'https', ...args], { stdio: 'pipe' });
  try {
    dotnet('--check', '--trust'); // exists, valid and trusted (exit code 0)
  } catch {
    throw new Error(
      'MOCKS=smtp needs a trusted ASP.NET development certificate (CN=localhost). Create and trust it, then rerun:\n' +
        '  dotnet dev-certs https --trust\n' +
        'Check it with: dotnet dev-certs https --check --trust',
    );
  }
  const dir = path.join(here, 'mocks', '.cert');
  mkdirSync(dir, { recursive: true });
  const pfx = path.join(dir, 'localhost.pfx');
  const password = randomBytes(16).toString('hex');
  try {
    dotnet('--export-path', pfx, '--password', password, '--format', 'Pfx');
  } catch {
    throw new Error('Could not export the development certificate (it must be exportable). Recreate it: dotnet dev-certs https --clean then dotnet dev-certs https --trust');
  }
  return { pfx, password };
}
const smtpCert = mocked.has('smtp') ? exportDevCertificate() : undefined;

const mockPort = 7090;
const mockUrl = `${origin(mockPort)}`;

// --- Parameters (values: `aspire secret set Parameters:<name> <value>`, or the dashboard) ---
// Created on first use, so a mocked service does not ask for its parameter.
type Param = Awaited<ReturnType<typeof builder.addParameter>>;
const created = new Map<string, Param>();
const param = async (name: string, secret = false): Promise<Param> => {
  let existing = created.get(name);
  if (!existing) {
    existing = await builder.addParameter(name, { secret });
    created.set(name, existing);
  }
  return existing;
};
/** The mock value when `feature` is mocked, otherwise the Aspire parameter. */
const setting = async (feature: string, name: string, mockValue: string, secret = false): Promise<string | Param> =>
  mocked.has(feature) ? mockValue : await param(name, secret);

const openMeteoApiUrl = await setting('weather', 'open-meteo-api-url', `${mockUrl}/open-meteo?lat={0}&lon={1}&tz={2}`);

// --- Azurite (blob 10000, queue 10001, table 10002) via ../db `npm run dev` ---------------
const db = await builder.addJavaScriptApp('db', at('db'), { runScriptName: 'dev' });

// --- Mock services (only when MOCKS is set) ------------------------------------------------
const mocks = mocked.size
  ? await builder
      .addJavaScriptApp('mocks', path.join(here, 'mocks'), { runScriptName: 'start' })
      .withEnvironment('MOCK_SMTP_PFX', smtpCert?.pfx ?? '')
      .withEnvironment('MOCK_SMTP_PFX_PASSWORD', smtpCert?.password ?? '')
      .withEnvironment('PORT', String(mockPort))
      .withHttpEndpoint({ port: mockPort, targetPort: mockPort, isProxied: false })
      .withHttpHealthCheck({ path: '/healthz' })
      .withUrl(`${mockUrl}/`, { displayText: 'Mock mail inbox' })
  : undefined;

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
  .withEnvironment('OPEN_METEO_API_URL', openMeteoApiUrl)
  .waitFor(db);
if (mocks) await api.waitFor(mocks);

const support = await common(builder.addExecutable('support', 'func', at('support'), ['host', 'start', '--port', String(ports.support)]), ports.support)
  .withEnvironment('SITE_FR_URL', url.siteFr)
  .withEnvironment('SITE_EN_URL', url.siteEn)
  .waitFor(api);

// With the smtp mock: batch and admin talk to the fake SMTP server on localhost:465 (any login).
// With another mock but not smtp: placeholder values so they start (the smtp channel would fail).
const smtp = mocked.has('smtp');
const batch = await common(builder.addExecutable('batch', 'func', at('batch'), ['host', 'start', '--port', String(ports.batch)]), ports.batch)
  .withEnvironment('SITE_FR_URL', url.siteFr)
  .withEnvironment('SITE_EN_URL', url.siteEn)
  .withEnvironment('UNSUBSCRIBE_URL', `${url.support}/api/unsubscribe`)
  .withEnvironment('INTERNAL_PROTOCOL', 'http')
  .withEnvironment('WEBSITE_HOSTNAME', `localhost:${ports.batch}`)
  .withEnvironment('INTERNAL_API_KEY', mocked.size ? 'mock' : await param('internal-api-key', true))
  .withEnvironment('PERIOD_IN_DAYS', mocked.size ? '1' : await param('period-in-days'))
  .withEnvironment('OPEN_METEO_API_URL', openMeteoApiUrl)
  .withEnvironment('SEND_MAIL_API_URL', await setting('mail', 'send-mail-api-url', `${mockUrl}/send-mail`))
  .withEnvironment('SMTP_URL', mocked.size ? 'localhost' : await param('smtp-url'))
  .withEnvironment('SMTP_LOGIN', mocked.size ? 'mock' : await param('smtp-login'))
  .withEnvironment('SMTP_PASSWORD', mocked.size ? 'mock' : await param('smtp-password', true))
  .withEnvironment('SCALEWAY_API_URL', await setting('scaleway', 'scaleway-api-url', `${mockUrl}/scaleway`))
  .withEnvironment('SCALEWAY_PROJECT_ID', await setting('scaleway', 'scaleway-project-id', 'mock'))
  .withEnvironment('SCALEWAY_API_KEY', await setting('scaleway', 'scaleway-api-key', 'mock', true))
  .waitFor(support)
  .withExplicitStart();
if (mocks) await batch.waitFor(mocks);

// --- admin (ASP.NET, config binds env vars: ConnectionStrings__X, SiteEnUrl...) -----------
// With the maps mock, AzureMapsEndpoint redirects admin's Azure Maps SDK calls to the mock
// (optional admin setting; unset = real Azure Maps). The browser map widgets still load the real
// Azure Maps web SDK and need a real key: with the mock they do not render.
const admin = builder
  .addProject('admin', at('admin', 'src', 'admin.csproj'), { launchProfileOrOptions: 'http' })
  .withEnvironment('ConnectionStrings__Alerts', devStorage)
  .withEnvironment('ConnectionStrings__AzureMaps', await setting('maps', 'azure-maps-connection-string', 'mock', true))
  .withEnvironment('ConnectionStrings__Smtp', smtp ? 'mock:mock@localhost' : mocked.size ? 'mock' : await param('admin-smtp-connection-string', true))
  .withEnvironment('SiteFrUrl', url.siteFr)
  .withEnvironment('SiteEnUrl', url.siteEn)
  .waitFor(db);
if (mocked.has('maps')) await admin.withEnvironment('AzureMapsEndpoint', mockUrl).waitFor(mocks!);
else await admin;

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
