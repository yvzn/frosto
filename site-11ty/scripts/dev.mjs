// Dev loop: for each site, Eleventy (watch) renders into .11ty/<site>/ and a Vite dev server
// serves that folder with live reload.
//   French:  http://localhost:5180/
//   English: http://localhost:5181/
//
//   node scripts/dev.mjs             both sites
//   node scripts/dev.mjs --site fr   one site
//
// Editing a template re-renders the HTML (Vite reloads the page); editing src/assets/
// copies the file again (Vite hot-reloads CSS/JS).
// Responses carry the production headers, with the endpoint origins added to the CSP connect-src
// (see vite.config.js).

import { rmSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { runPerSite } from './lib/per-site.mjs';
import { PROJECT_ROOT, currentSite, eleventyOutDir } from './lib/sites.mjs';

if (!process.env.SITE) {
	runPerSite(fileURLToPath(import.meta.url), process.argv.slice(2));
} else {
	const site = currentSite();
	process.chdir(PROJECT_ROOT);
	rmSync(eleventyOutDir(site), { recursive: true, force: true });

	const { default: Eleventy } = await import('@11ty/eleventy');
	const eleventy = new Eleventy(undefined, undefined, { configPath: 'eleventy.config.js' });
	await eleventy.write();
	await eleventy.watch();

	const { createServer } = await import('vite');
	const server = await createServer({ configFile: path.join(PROJECT_ROOT, 'vite.config.js') });
	await server.listen();
	server.printUrls();
}
