// The contact address is split in parts in the page scripts (contact.js, sign-up-complete.js) and only
// assembled in the browser, out of reach of simple scrapers. A minifier update once folded the parts back
// into a plain string without anyone noticing: the build now fails if an address of the sites' domains
// appears in the output, plain or encoded.

import { readFileSync, readdirSync } from 'node:fs';
import path from 'node:path';

const PLAIN_EMAIL = /[A-Za-z0-9._%+-]+(?:@|%40|&#0*64;|&#x0*40;)(?:alertegelees?\.fr|frostalert\.net)/gi;

/** Plain email addresses in a text: [] when every address is obfuscated. */
export function plainEmails(text) {
	return text.match(PLAIN_EMAIL) ?? [];
}

/** Plain email addresses in the .html and .js files of a built site, as "<file>: <address>". */
export function findPlainEmails(dir) {
	const found = [];
	for (const entry of readdirSync(dir, { recursive: true, withFileTypes: true })) {
		if (!entry.isFile() || !/\.(html|js)$/.test(entry.name)) continue;
		const file = path.join(entry.parentPath, entry.name);
		for (const email of plainEmails(readFileSync(file, 'utf8'))) {
			found.push(`${path.relative(dir, file)}: ${email}`);
		}
	}
	return found;
}
