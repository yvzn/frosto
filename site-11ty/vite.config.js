// Vite builds the Eleventy output of ONE site (SITE=fr|en): .11ty/<site>/ -> dist/<site>/.
// Same settings as the legacy site/vite.config.js, except that the page list is
// discovered from Eleventy's output instead of being maintained by hand.
//
// Form endpoints: Vite is the ONLY place that substitutes them, as in the legacy sites:
// %VITE_SIGNUP_URL% in HTML and import.meta.env.VITE_SIGNUP_URL in JS.
// Values come from env/<site>/.env (empty in the repo), overridden by environment variables.

import { existsSync, readdirSync } from 'node:fs';
import path from 'node:path';
import { defineConfig } from 'vite';
import { PROJECT_ROOT, SITES, currentSite, distDir, eleventyOutDir } from './scripts/lib/sites.mjs';

const site = currentSite();
const root = eleventyOutDir(site);

function pages() {
	if (!existsSync(root)) return {};
	return Object.fromEntries(
		readdirSync(root)
			.filter((file) => file.endsWith('.html'))
			.map((file) => [path.parse(file).name, path.join(root, file)]),
	);
}

export default defineConfig({
	root,
	base: '',
	envDir: path.join(PROJECT_ROOT, 'env', site),
	build: {
		outDir: distDir(site),
		emptyOutDir: true,
		rollupOptions: {
			input: pages(),
		},
	},
	server: {
		port: SITES[site].devPort,
		strictPort: true,
	},
});
