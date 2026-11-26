import path from 'node:path';
import { fileURLToPath } from 'node:url';

export const PROJECT_ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..', '..');

/**
 * Build settings per site. Site content settings (domain, lang...) live in src/_data/sites/.
 * devPort: Vite dev server port (serves .11ty/<site>/).
 * previewPort: Vite preview server port (serves dist/<site>/).
 */
export const SITES = {
	fr: { devPort: 5180, previewPort: 4180 },
	en: { devPort: 5181, previewPort: 4181 },
};

/**
 * Eleventy writes here; Vite then uses it as its root.
 * npm run build (SITE_BUILD set by scripts/build.mjs) uses .11ty/build/<site>/, apart from the
 * .11ty/<site>/ of npm run dev: a build wipes its folder first, which would leave a running dev
 * server serving stale files.
 */
export function eleventyOutDir(site) {
	return process.env.SITE_BUILD
		? path.join(PROJECT_ROOT, '.11ty', 'build', site)
		: path.join(PROJECT_ROOT, '.11ty', site);
}

export function distDir(site) {
	return path.join(PROJECT_ROOT, 'dist', site);
}

/** The site selected with the SITE environment variable. */
export function currentSite() {
	const site = process.env.SITE;
	if (!SITES[site]) {
		throw new Error(`Set SITE to one of: ${Object.keys(SITES).join(', ')} (got "${site ?? ''}")`);
	}
	return site;
}

export function parseSiteArgs(argv) {
	const idx = argv.indexOf('--site');
	const requested = idx >= 0 ? argv[idx + 1] : null;
	if (requested && !SITES[requested]) {
		throw new Error(`Unknown site "${requested}". Expected one of: ${Object.keys(SITES).join(', ')}`);
	}
	return requested ? [requested] : Object.keys(SITES);
}
