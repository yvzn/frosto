---
applyTo: "app/**"
---

# Vue 3 + TypeScript Companion App Instructions

## Runtime & Tooling

- **Node.js:** `>=20.19.0` or `>=22.12.0` (enforced by `engines` in `package.json`)
- **Package manager:** npm (use `npm ci` in CI, `npm install` locally)
- **Framework:** Vue 3 with TypeScript
- **Build tool:** Vite 8 (`vite.config.ts`)
- **Base path:** `/app/` — all assets are served under `/app/`
- **Type checking:** `vue-tsc` (not plain `tsc`)
- **Linting:** oxlint (`.oxlintrc.json`) + ESLint (`eslint.config.ts`)
- **Formatting:** oxfmt (`.oxfmtrc.json`)

## Project Structure

```
app/
├── src/                  # Application source
│   └── (components, views, router, i18n, etc.)
├── public/               # Static assets copied as-is
├── index.html            # Entry HTML
├── vite.config.ts        # Vite config; base='/app/', Vue plugin, SCSS config
├── tsconfig.json         # TypeScript project references
├── tsconfig.app.json     # App TS config
├── tsconfig.node.json    # Node/Vite TS config
├── eslint.config.ts      # ESLint flat config
├── .oxlintrc.json        # oxlint rules
├── .oxfmtrc.json         # oxfmt formatter config
├── .env                  # Development env vars (committed)
├── build-env-production.ps1  # CI script to generate .env.production
└── package.json
```

## Commands (always run from `app/` directory)

```bash
npm install             # Install dependencies (required before any other command)
npm run dev             # Start dev server with hot reload
npm run type-check      # vue-tsc type check (must pass before committing)
npm run lint            # Run oxlint then eslint with auto-fix
npm run format          # Format src/ with oxfmt
npm run build           # type-check + vite build -> dist/
npm run clean           # Remove dist/
```

**Always run `npm run type-check` and `npm run lint` before considering a change complete.**

## Key Conventions

- **Path alias:** `@` maps to `./src/` (configured in `vite.config.ts`)
- **i18n:** `vue-i18n` is used for translations
- **Routing:** `vue-router` v5
- **CSS:** Bootstrap 5 + SCSS; SCSS deprecation warnings are silenced in `vite.config.ts`
- **Do not use `any` type** — TypeScript strict mode via `vue-tsc`
- Environment variables for production are injected via `build-env-production.ps1` during CI; for local dev, use the committed `.env` file
- The `dist/` folder is gitignored; never commit build output
