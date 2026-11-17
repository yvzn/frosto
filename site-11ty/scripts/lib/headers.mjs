// Response headers for the local servers (see vite.config.js):
//   preview: the production headers of src/public/.htaccess, as deployed
//   dev:     the same, plus the origins of the endpoints in env/<site>/.env* added to connect-src,
//            so a local API (http://localhost:7071…) can be called while coding,
//            and blob: workers for Vite's dev client

import { readFileSync } from 'node:fs';
import path from 'node:path';
import { PROJECT_ROOT } from './sites.mjs';

const CSP = 'Content-Security-Policy';

/**
 * Every "Header set" line of an .htaccess file, including those inside <FilesMatch>.
 * Cache-Control is left out: the "immutable" value for assets would break live reload.
 */
export function parseHtaccessHeaders(htaccess) {
	const headers = {};
	for (const [, name, value] of htaccess.matchAll(/^\s*Header set (\S+) "(.+)"\s*$/gm)) {
		if (name.toLowerCase() !== 'cache-control') headers[name] = value;
	}
	return headers;
}

/** Production headers, from src/public/.htaccess. */
export function productionHeaders() {
	return parseHtaccessHeaders(readFileSync(path.join(PROJECT_ROOT, 'src', 'public', '.htaccess'), 'utf8'));
}

/** Origins of the VITE_*_URL values: { VITE_SUPPORT_URL: 'http://localhost:7071/api/x' } -> ['http://localhost:7071']. */
export function endpointOrigins(env) {
	const origins = new Set();
	for (const [name, value] of Object.entries(env)) {
		if (!/^VITE_\w+_URL$/.test(name) || !value) continue;
		try {
			origins.add(new URL(value).origin);
		} catch {
			// not an absolute URL: nothing to allow
		}
	}
	return [...origins];
}

/**
 * Copy of the headers allowing Vite's dev client: after a dev server restart, it polls for the server
 * from a blob: worker, which the production CSP blocks (worker-src falls back to default-src 'self').
 */
export function withViteClient(headers) {
	const csp = headers[CSP];
	if (!csp || /(^|;)\s*worker-src /.test(csp)) return headers;
	return { ...headers, [CSP]: `${csp}; worker-src 'self' blob:` };
}

/** Copy of the headers with the origins added to the CSP connect-src directive, if missing. Returns the origins added. */
export function withConnectSrc(headers, origins) {
	const csp = headers[CSP];
	if (!csp) return { headers, added: [] };
	const directives = csp.split(';').map((d) => d.trim());
	const index = directives.findIndex((d) => d.startsWith('connect-src '));
	if (index < 0) return { headers, added: [] };
	const sources = directives[index].split(/\s+/).slice(1);
	const added = origins.filter((origin) => !sources.includes(origin));
	if (!added.length) return { headers, added };
	directives[index] = ['connect-src', ...sources, ...added].join(' ');
	return { headers: { ...headers, [CSP]: directives.join('; ') }, added };
}
