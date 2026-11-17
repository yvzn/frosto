// Serves the built sites (dist/<site>/, from npm run build) with the production headers of
// src/public/.htaccess, CSP included, as a last check before a release.
//   French:  http://localhost:4180/
//   English: http://localhost:4181/
//
//   node scripts/preview.mjs             both sites
//   node scripts/preview.mjs --site fr   one site
//
// The pages call the endpoints the build was made with: build with the real ones
// (environment variables or env/<site>/.env.production.local) for a meaningful CSP check.

import { existsSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { runPerSite } from './lib/per-site.mjs';
import { PROJECT_ROOT, currentSite, distDir } from './lib/sites.mjs';

if (!process.env.SITE) {
	runPerSite(fileURLToPath(import.meta.url), process.argv.slice(2));
} else {
	const site = currentSite();
	process.chdir(PROJECT_ROOT);
	if (!existsSync(path.join(distDir(site), 'index.html'))) {
		console.error(`dist/${site}/ is missing: run npm run build first.`);
		process.exit(1);
	}

	const { preview } = await import('vite');
	const server = await preview({ configFile: path.join(PROJECT_ROOT, 'vite.config.js') });
	server.printUrls();
}
