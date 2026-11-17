# site-11ty — Eleventy migration of the Frosto websites

Work in progress: this folder will replace `site/` (alertegelee.fr) and `site-en/` (frostalert.net)
with a single Eleventy project. Until cutover, the legacy folders stay untouched and remain the source of truth.

Current state: **checkpoint 1 — baseline and parity check.** No new site is built yet.

Node version: see `.nvmrc`.

## Parity check

The parity check answers "did the migration change anything visitors or search engines can see?" with yes or no.

```bash
npm ci
npm run check            # compare dist/fr and dist/en with baseline/fr and baseline/en
npm run check -- --site fr
```

For each site it fails on any difference in:

- the list of output files (pages at the same flat `.html` paths, `.htaccess`, `robots.txt`, `sitemap.xml`, assets)
- per page: `lang`, `<title>`, all `<meta>` tags, canonical, `hreflang` links, headings, links, and every `<form>`
  (action, method, field names, hidden field values)
- the rest of the markup (zero-diff principle): text, classes, attributes, element structure
- `robots.txt`, `.htaccess`, page JS and CSS: exact content
- `sitemap.xml`: URLs, `lastmod`, `changefreq`, `priority`, alternates (including `x-default` on home only)
- any leftover `%VITE_…%` placeholder

It ignores whitespace, line endings, attribute order and Vite content hashes in `assets/` file names.

`npm test` runs self-tests that mutate a copy of the baseline and check that each kind of regression is caught,
and that harmless changes (re-indentation, new hashes) are not.

## Baseline

`baseline/fr/` and `baseline/en/` are the legacy builds of `site/` and `site-en/`. **Do not edit them by hand.**

They are built with dummy endpoints (`https://fr.parity.invalid/…`, `https://en.parity.invalid/…`,
see `scripts/lib/sites.mjs`). The new build must use the same values so the comparison is fair.
A distinct host per site catches a page posting to the other site's API.

```bash
npm run baseline          # rebuild site/ and site-en/, overwrite baseline/ (then commit)
npm run baseline:verify   # rebuild into .tmp/ and compare with baseline/ (run in CI)
```

The build runs `npm ci` and `npm run build` inside the legacy folders. It only writes their git-ignored
`node_modules/` and `dist/`, and fails if any tracked file changes.

If `site/` or `site-en/` is changed on `main` during the migration, `baseline:verify` fails in CI.
Regenerate the baseline and port the same change to the Eleventy templates.

## CI

`azure-pipelines-parity.yml` runs the self-tests and `baseline:verify` on every PR touching `site-11ty/`, `site/`
or `site-en/`. It must be registered once as a new pipeline in Azure DevOps. It deploys nothing.
