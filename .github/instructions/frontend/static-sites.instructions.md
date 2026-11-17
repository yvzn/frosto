---
applyTo: "site-11ty/**,site-maintenance/**,site-redirect/**"
---

# Static Sites Instructions (site-11ty, site-maintenance, site-redirect)

## Overview

- **`site-11ty/`** — Both landing pages: French (alertegelee.fr) and English (frostalert.net). Eleventy (Nunjucks templates) renders the HTML, Vite bundles JS and Bootstrap 5 CSS, PurgeCSS removes unused CSS. Plain vanilla JS. Full documentation: `site-11ty/README.md`.
- **`site-maintenance/`** — Maintenance placeholder page.
- **`site-redirect/`** — HTTP redirect page.

## `site-11ty/` structure

```
site-11ty/
├── src/content/fr/*.njk, src/content/en/*.njk   # One template per page, published as /<page>.html
├── src/_includes/layouts/base.njk               # Page skeleton: <head> from front matter, header, page script
├── src/_includes/partials/                      # Headers, consent checkbox, per-site footers
├── src/_data/sites/fr.js, en.js                 # Per-site settings and shared labels ({{ site.* }})
├── src/_data/sitemap.js                         # Pages listed in sitemap.xml, with lastmod
├── src/assets/                                  # Shared main.js, main.css, unsubscribe.js, images
├── src/scripts/fr/, src/scripts/en/             # Page scripts with translated strings
├── src/public/                                  # .htaccess (shared), robots.txt.njk, sitemap.xml.njk
├── env/fr/.env, env/en/.env                     # Endpoint variables, empty in the repo
├── eleventy.config.js, vite.config.js           # Read SITE=fr|en
└── scripts/                                     # build, dev, preview, tests
```

## Commands (run from `site-11ty/`)

```bash
npm ci             # Always run first
npm run dev        # Both sites with live reload: FR http://localhost:5180/, EN http://localhost:5181/
npm run build      # dist/fr/ and dist/en/ (Eleventy, Vite, PurgeCSS, email obfuscation check)
npm run preview    # Serve dist/ with the production headers (CSP)
npm test           # Self-tests
```

## Conventions

- One page = one template per site; keep French and English in sync for any structural or functionality change.
- Canonical, hreflang, og:url and the language switch come from `site.*` settings: never hard-code a domain in a template.
- Form/API endpoints are substituted by Vite only (`%VITE_SIGNUP_URL%` in HTML, `import.meta.env.VITE_*` in JS), from environment variables or `env/<site>/.env*`. Use `env/<site>/.env.local` for local values. Templates must leave `%VITE_…%` placeholders untouched.
- A new API host must be added to the CSP `connect-src` in `src/public/.htaccess`.
- Keep the contact address split in parts with `.map(String).join('')`: the build fails if it appears in plain text.
- `dist/` and `.11ty/` are gitignored; never commit build output.
- No JavaScript framework. Do not add npm dependencies without a documented reason.
