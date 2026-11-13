# Frosto Eleventy migration

This project generates the French and English static websites from a single Eleventy template set.

## Local development

```bash
cd site-11ty
npm install
npm run dev
```

The dev command runs both languages in parallel:

- French: http://localhost:5180/
- English: http://localhost:5181/

## Build

```bash
cd site-11ty
npm run build
```

This renders both outputs to:

- `dist/fr/`
- `dist/en/`

## Parity check

```bash
cd site-11ty
npm run check
```

This compares the generated HTML against the saved baseline in `baseline/`.

## Adding a page

1. Add a new content file under `src/content/fr/` or `src/content/en/`.
2. Set `permalink` to match the final URL, e.g. `permalink: "my-page.html"`.
3. Share the shared layout and metadata via the standard Eleventy template files.

## Adding a string

Edit the language data in:

- `src/_data/i18n/fr.json`
- `src/_data/i18n/en.json`

Values are then exposed to layouts and page templates through the site data.

## Required environment variables

The default values are intentionally empty in the repo and are loaded from the environment when the site is built:

- `SIGNUP_URL`
- `HEALTHCHECK_URL`
- `SUPPORT_URL`
- `CHECKSUBSCRIPTION_URL`
- `UNSUBSCRIBE_URL`
- `SITE` (set to `fr` or `en` for local builds)

This project keeps the old sites untouched while generating deployable outputs for both markets.
