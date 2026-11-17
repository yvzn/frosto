import path from 'node:path';
import { fileURLToPath } from 'node:url';

export const PROJECT_ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..', '..');
export const REPO_ROOT = path.resolve(PROJECT_ROOT, '..');

/**
 * Build settings per site. Site content settings (domain, lang...) live in src/_data/sites/.
 * legacyDir: legacy Vite project, relative to the repository root.
 * devPort: Vite dev server port (legacy sites use Vite's default 5173).
 * previewPort: Vite preview server port (serves dist/<site>/).
 */
export const SITES = {
	fr: { legacyDir: 'site', devPort: 5180, previewPort: 4180 },
	en: { legacyDir: 'site-en', devPort: 5181, previewPort: 4181 },
};

/** Eleventy writes here; Vite then uses it as its root. */
export function eleventyOutDir(site) {
	return path.join(PROJECT_ROOT, '.11ty', site);
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

/**
 * Dummy form/API endpoints used for BOTH the baseline build and the new build,
 * so the parity check compares like with like.
 * Each site gets its own host so a page pointing at the other site's endpoint is caught.
 * The `.invalid` TLD is reserved (RFC 2606) and never resolves.
 */
export function dummyEndpoints(site) {
	const host = `https://${site}.parity.invalid`;
	return {
		VITE_SIGNUP_URL: `${host}/signup`,
		VITE_HEALTHCHECK_URL: `${host}/healthcheck`,
		VITE_SUPPORT_URL: `${host}/support`,
		VITE_CHECKSUBSCRIPTION_URL: `${host}/check-subscription`,
		VITE_UNSUBSCRIBE_URL: `${host}/unsubscribe`,
	};
}

export function parseSiteArgs(argv) {
	const idx = argv.indexOf('--site');
	const requested = idx >= 0 ? argv[idx + 1] : null;
	if (requested && !SITES[requested]) {
		throw new Error(`Unknown site "${requested}". Expected one of: ${Object.keys(SITES).join(', ')}`);
	}
	return requested ? [requested] : Object.keys(SITES);
}

export function argValue(argv, name) {
	const idx = argv.indexOf(name);
	return idx >= 0 ? argv[idx + 1] : null;
}
