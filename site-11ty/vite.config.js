// Vite builds the Eleventy output of ONE site (SITE=fr|en): .11ty/build/<site>/ -> dist/<site>/,
// or serves .11ty/<site>/ in dev (see scripts/lib/sites.mjs).
// The page list is discovered from Eleventy's output: every .html file is an entry point.
//
// Form endpoints: Vite is the ONLY place that substitutes them:
// %VITE_SIGNUP_URL% in HTML and import.meta.env.VITE_SIGNUP_URL in JS.
// Values come from env/<site>/.env (empty in the repo), overridden by environment variables.
//
// Response headers of the local servers (scripts/lib/headers.mjs):
//   preview (npm run preview): the production headers of src/public/.htaccess, as deployed
//   dev     (npm run dev):     the same, plus the endpoint origins added to the CSP connect-src
//                              and blob: workers for Vite's dev client

import { existsSync, readdirSync } from 'node:fs';
import path from 'node:path';
import { defineConfig, loadEnv } from 'vite';
import { endpointOrigins, productionHeaders, withConnectSrc, withViteClient } from './scripts/lib/headers.mjs';
import { PROJECT_ROOT, SITES, currentSite, distDir, eleventyOutDir } from './scripts/lib/sites.mjs';

const site = currentSite();
const root = eleventyOutDir(site);
const envDir = path.join(PROJECT_ROOT, 'env', site);

function pages() {
	if (!existsSync(root)) return {};
	return Object.fromEntries(
		readdirSync(root)
			.filter((file) => file.endsWith('.html'))
			.map((file) => [path.parse(file).name, path.join(root, file)]),
	);
}

/**
 * Production headers, plus the origins of this site's endpoints (env files and variables) in connect-src
 * and blob: workers for Vite's dev client.
 */
function devHeaders(mode) {
	const { headers, added } = withConnectSrc(productionHeaders(), endpointOrigins(loadEnv(mode, envDir, 'VITE_')));
	if (added.length) {
		console.log(`[dev CSP] connect-src + ${added.join(' ')} (dev only: add them to src/public/.htaccess to deploy)`);
	}
	return withViteClient(headers);
}

export default defineConfig(({ command, mode, isPreview }) => ({
	root,
	base: '',
	envDir,
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
		headers: command === 'serve' && !isPreview ? devHeaders(mode) : undefined,
	},
	preview: {
		port: SITES[site].previewPort,
		strictPort: true,
		headers: isPreview ? productionHeaders() : undefined,
	},
}));
