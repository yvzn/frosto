# site-11ty — Frosto websites

The two Frosto landing sites, built from one Eleventy project:

- **fr**: [alertegelee.fr](https://www.alertegelee.fr), output in `dist/fr/`
- **en**: [frostalert.net](https://www.frostalert.net), output in `dist/en/`

They replaced the former `site/` and `site-en/` Vite projects with zero visible difference. The tools used for
that migration (parity check against the former builds, live snapshot and check of the deployed sites) are in
the git history.

Node version: see `.nvmrc`.

## Commands

```bash
npm ci
npm run dev                 # both sites with live reload: FR http://localhost:5180/, EN http://localhost:5181/
npm run dev -- --site fr    # one site
npm run build               # dist/fr/ and dist/en/, ready to deploy as-is
npm run preview             # serve dist/ with the production headers: FR http://localhost:4180/, EN http://localhost:4181/
npm test                    # self-tests (email guard, local server headers)
```

## How a build works

For each site, in its own process (`SITE=fr` or `SITE=en`):

1. **Eleventy** renders `src/` into `.11ty/<site>/`, a folder laid out as a Vite project:
   pages as `*.html`, `main.js`, `main.css`, page scripts, `asset/`, and `public/` (`.htaccess`, `robots.txt`,
   `sitemap.xml`, see [Server files](#server-files)).
2. **Vite** builds `.11ty/<site>/` into `dist/<site>/`: bundles JS and Bootstrap CSS, hashes file names,
   copies `public/` to the output root. Every `.html` file Eleventy produced is a Vite input: there is no page list to maintain.
3. **PurgeCSS** removes unused CSS.
4. **Email check**: the build fails if the contact address appears unobfuscated in the output
   (see [Email obfuscation](#email-obfuscation)).

In dev, Eleventy watches `src/` and a Vite dev server serves `.11ty/<site>/`. Any edit re-renders the site
and Vite reloads the page. The dev server sends the production security headers (see [Local servers and CSP](#local-servers-and-csp)).

| Path | What |
|---|---|
| `src/content/<site>/*.njk` | one file per page; `sign-up.njk` is published as `/sign-up.html` (`src/content/content.11tydata.js`) |
| `src/_includes/layouts/base.njk` | page skeleton: `<head>` built from front matter, header, page script |
| `src/_includes/partials/` | header (with the language switch), simple header, form consent checkbox |
| `src/_includes/partials/<site>/footer.njk` | home page footer, different on each site |
| `src/_data/site.js`, `src/_data/sites/<site>.js` | site settings and shared labels, available in templates as `{{ site.lang }}`, `{{ site.domain }}`… |
| `src/assets/` | shared `main.js`, `main.css`, `unsubscribe.js`, `asset/` images (identical on both sites) |
| `src/scripts/<site>/` | page scripts with translated strings, loaded as `./<page>.js` |
| `src/public/` | `.htaccess` (shared), `robots.txt.njk`, `sitemap.xml.njk`: published at the site root |
| `src/_data/sitemap.js` | pages listed in `sitemap.xml`, with `lastmod`, `changefreq`, `priority` |
| `env/<site>/.env` | form endpoints, empty in the repo |
| `eleventy.config.js`, `vite.config.js` | read `SITE` |
| `scripts/build.mjs`, `scripts/dev.mjs`, `scripts/preview.mjs` | run the steps above for one or both sites |

## Pages

A page extends the layout and fills two blocks: `head` (extra tags such as Open Graph) and `main`
(everything between the header and the page script). The rest comes from front matter:

```njk
---
title: Inscription - Alerte gelées     # full <title>, the site name is not appended
description: Inscrivez-vous…           # optional <meta name="description">
homeLabel: Revenir à l'accueil         # aria-label of the home link in the header
brand: Frost alert                     # optional, header site name, default site.name
script: sign-up                        # page script, default main
noindex: true                          # default false
canonical: false                       # default true: canonical + hreflang links
header: simple                         # default full (with the language switch)
---
{% extends "layouts/base.njk" %}
{% block main %}…{% endblock %}
```

Canonical, `hreflang`, `og:url` and the language switch are built from `site.domain`, `site.alternateDomain`
and the page path, so a page cannot point to the wrong domain.

`open-meteo.njk` is a bare redirect and does not use the layout.

Keep both sites in sync: a structural or functional change to a French page goes to its English page too.

## Form endpoints

Vite substitutes them, and only Vite: `%VITE_SIGNUP_URL%` in HTML and `import.meta.env.VITE_SIGNUP_URL` in JS.
Templates must leave `%VITE_…%` placeholders untouched.

Values come from environment variables (`VITE_SIGNUP_URL`, `VITE_HEALTHCHECK_URL`, `VITE_SUPPORT_URL`,
`VITE_CHECKSUBSCRIPTION_URL`, `VITE_UNSUBSCRIBE_URL`), falling back to `env/<site>/.env`.
Use `env/<site>/.env.local` (git-ignored) for local values, read by both `npm run dev` and `npm run build`.
For a build only, `env/<site>/.env.production.local` takes precedence.

## Local servers and CSP

Both local servers send the `Header set` values of `src/public/.htaccess` (`scripts/lib/headers.mjs`),
so CSP violations show up in the browser console before deployment. `Cache-Control` is left out.

| | `npm run dev` | `npm run preview` |
|---|---|---|
| Serves | `.11ty/<site>/`, live reload | `dist/<site>/`, from the last `npm run build` |
| Ports | FR 5180, EN 5181 | FR 4180, EN 4181 |
| CSP | production, plus the endpoint origins in `connect-src` and `worker-src 'self' blob:` | production, unchanged |

- **Day to day**: put a local API in `env/<site>/.env.local`, for example
  `VITE_SUPPORT_URL=http://localhost:7071/api/support`. The dev server adds `http://localhost:7071` to
  `connect-src` and says so at startup: `[dev CSP] connect-src + http://localhost:7071`.
  `worker-src` lets Vite's client poll for the server after a restart.
- **Before a release**: build with the real endpoints (environment variables or
  `env/<site>/.env.production.local`), then `npm run preview`. Any endpoint host missing from `.htaccess`
  is blocked there, as it would be in production. A preview of a build made with local endpoints shows
  those blocked, which is expected.
- After changing headers, reload with the cache bypassed (Ctrl+Shift+R): a cached page keeps the old CSP.
- Neither server runs Apache: `<FilesMatch>` scoping, `Cache-Control` and `AddType` are only exercised once deployed.

## Server files

- **`.htaccess`**: identical on both sites, copied as-is. It holds the security headers, including the
  Content-Security-Policy whose `connect-src` lists the API hosts: update it when an endpoint host changes.
  The local servers send these headers too, except `Cache-Control` (see [Local servers and CSP](#local-servers-and-csp)).
- **`robots.txt`**: points to `{{ site.domain }}/sitemap.xml`.
- **`sitemap.xml`**: one `<url>` per entry of `src/_data/sitemap.js`, on the site's own domain, with
  `hreflang` alternates for both sites and `x-default` on the home page only. When a listed page changes,
  update its `lastmod` there.

## Email obfuscation

`contact.js` and `sign-up-complete.js` build the contact address in the browser from parts, so it never
appears in the HTML or JS files:

```js
['yvan', '%40', 'alertegelee.', 'fr'].map(String).join('')
```

Keep `.map(String)`: without it, rolldown 1.2.16+ folds the parts into a plain string at build time.
`scripts/lib/email-guard.mjs` fails the build if an address of `alertegelee.fr`, `alertegelees.fr` or
`frostalert.net` appears in the output, plain or encoded (`@`, `%40`, `&#64;`).

`alertegelees.fr` (with an "s") in `sign-up-complete.js` is not a typo: it is the sending domain set up for
DKIM and SPF.

## Known quirks

Inherited from the former sites and kept during the zero-diff migration. Free to fix now:

- EN `donate.html`: header reads "Frost alert" instead of "Frost Alerts" (`brand` in its front matter).
- EN `unsubscribe-complete.html`: image alt text copied from `check-subscription-complete.html`.
- `contact.html` (both sites) has no `<main>` element.
- Home-link `aria-label`, head tags order and some wording vary from page to page.
- FR and EN `sign-up.html` use different markup for the consent checkbox.
- `sign-up.js` and `check-subscription.js` share most of their code, duplicated on each site.

## Live check

The parity check proves the built files are unchanged. The live check covers what the server adds, on the
deployed sites: status codes, response headers, files actually present on the host. It is made for cutover:

```bash
npm run live -- snapshot                                   # 1. before cutover: save production
# 2. cutover: deploy site-11ty to production
npm run live -- check --snapshot live-snapshots/<timestamp>   # 3. after: compare production with the snapshot
```

- **Fetched**: `/`, every page of `src/content/<site>/`, `robots.txt`, `sitemap.xml`, and every same-origin
  script, stylesheet, image and link they reference (about 30 URLs per site). Status only: `/.htaccess`
  (expected 403) and a missing page (expected 404).
- **Compared**: HTTP status; `content-type`, `cache-control`, `content-security-policy`, `x-frame-options`,
  `x-content-type-options`, `strict-transport-security`, `referrer-policy`, `location`; file contents with
  the parity check rules (whitespace, attribute order and asset hashes ignored).
- **Options**: `--site fr|en` for one site; `--base <url>` to fetch another host with one site, for example
  a staging URL against a production snapshot (pages are identical: canonical and `hreflang` always point
  to the production domains).
- **Output**: `live-snapshots/<timestamp>/<site>/` (git-ignored): `responses.json` and `files/`. The check
  fetches into `.tmp/live/<site>/`, kept for inspection. Exit code 1 on any difference.

Any difference after cutover is either expected (none should be) or a reason to roll back: re-enable the
legacy pipelines and run them.

## CI

`azure-pipelines-checks.yml` runs on every PR touching `site-11ty/`: `npm test`, then `npm run build` of both
sites. It deploys nothing.

## Deployment

`azure-pipelines.yml` builds and deploys both sites: per site, one build job (`npm ci`, then
`npm run build -- --site <site>` with the endpoints as environment variables) and one deployment job
(`CopyFilesOverSSH` of `dist/<site>/`, `.htaccess` included). Deployment runs from `main` only.

| Parameter `Deploy to` | When | SSH variables used | Azure environment |
|---|---|---|---|
| `production` (default) | every push to `main` touching `site-11ty/` (except this README) | `sshEndpoint`, `sshTargetFolder` | `Azure` |
| `staging` | manual run only | `stagingSshEndpoint` | `staging` |

Variables come from the variable groups `production` (fr) and `production-en` (en): `signUpUrl`,
`healthCheckUrl`, `supportUrl`, `checkSubscriptionUrl`, `unsubscribeUrl`, `sshEndpoint`, `sshTargetFolder`,
`stagingSshEndpoint`, `stagingSshTargetFolder`. The build step maps the endpoints explicitly to `VITE_*`
environment variables, which Vite reads.

Staging uses the production endpoints, already allowed by the CSP. Form posts work from any origin; the
health and support checks (`fetch`/XHR from the page) also need the API's CORS settings to allow the
staging origins.

Hashed assets from previous deployments stay on the host (`overwrite: true` replaces files, it does not
delete old ones).
