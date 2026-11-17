import path from 'node:path';
import { fileURLToPath } from 'node:url';

export const PROJECT_ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..', '..');
export const REPO_ROOT = path.resolve(PROJECT_ROOT, '..');

/** Legacy Vite project for each site, relative to the repository root. */
export const SITES = {
	fr: { legacyDir: 'site' },
	en: { legacyDir: 'site-en' },
};

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
