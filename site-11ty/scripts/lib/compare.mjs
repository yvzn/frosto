// Compares two copies of a site (folders of HTML, assets, robots.txt, sitemap.xml…) and returns a list of
// human-readable differences. An empty list means "no SEO / behaviour change".
// Used by the live check (lib/live.mjs); written for the Eleventy migration's parity check.
//
// What is ignored on purpose:
//   - whitespace (runs collapsed, whitespace-only text nodes dropped, CRLF vs LF)
//   - attribute order
//   - Vite content hashes in asset file names (assets/name-XXXXXXXX.ext)
// Everything else in the HTML must match exactly (zero-diff principle).

import { readdirSync, readFileSync } from 'node:fs';
import path from 'node:path';
import { parse } from 'parse5';

const HASHED_ASSET = /^(.+)-[A-Za-z0-9_-]{8}(\.[A-Za-z0-9]+)$/;
const TEXT_FILES = /\.(html|xml|txt|js|mjs|css|svg|json|webmanifest)$|^\.htaccess$/;
const PLACEHOLDER = /%VITE_[A-Z0-9_]+%/;

// ---------------------------------------------------------------- files

function listFiles(rootDir) {
	const files = [];
	(function visit(dir) {
		for (const entry of readdirSync(dir, { withFileTypes: true })) {
			const full = path.join(dir, entry.name);
			if (entry.isDirectory()) visit(full);
			else files.push(path.relative(rootDir, full).split(path.sep).join('/'));
		}
	})(rootDir);
	return files.sort();
}

/** assets/main-DV-nNJPE.js -> assets/main.js; other paths unchanged. */
export function stripHash(relPath) {
	const dir = path.posix.dirname(relPath);
	if (dir !== 'assets') return relPath;
	const match = path.posix.basename(relPath).match(HASHED_ASSET);
	return match ? `${dir}/${match[1]}${match[2]}` : relPath;
}

function escapeRegExp(s) {
	return s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}

/** Reads a site: normalized path -> list of { path, content } (several hashed files can share a name). */
function loadSite(rootDir) {
	const files = listFiles(rootDir);
	// Replace every hashed asset name that actually exists in this output by its unhashed name.
	const renames = files
		.filter((f) => stripHash(f) !== f)
		.map((f) => [path.posix.basename(f), path.posix.basename(stripHash(f))])
		.sort((a, b) => b[0].length - a[0].length);
	const renameRe = renames.length ? new RegExp(renames.map(([from]) => escapeRegExp(from)).join('|'), 'g') : null;
	const renameMap = new Map(renames);

	const byKey = new Map();
	for (const file of files) {
		const key = stripHash(file);
		let content = readFileSync(path.join(rootDir, file));
		if (TEXT_FILES.test(path.posix.basename(file))) {
			content = content.toString('utf8').replace(/\r\n?/g, '\n');
			if (renameRe) content = content.replace(renameRe, (m) => renameMap.get(m));
		}
		if (!byKey.has(key)) byKey.set(key, []);
		byKey.get(key).push({ path: file, content });
	}
	return byKey;
}

// ---------------------------------------------------------------- HTML helpers

function attrs(node) {
	return Object.fromEntries((node.attrs || []).map((a) => [a.name, a.value]));
}

function* walk(node) {
	yield node;
	for (const child of node.childNodes || []) yield* walk(child);
	if (node.content) yield* walk(node.content); // <template>
}

function elements(doc, predicate) {
	return [...walk(doc)].filter((n) => n.tagName && predicate(n, attrs(n)));
}

function collapse(text) {
	return text.replace(/\s+/g, ' ');
}

function textContent(node) {
	return collapse([...walk(node)].filter((n) => n.nodeName === '#text').map((n) => n.value).join('')).trim();
}

/** The SEO- and behaviour-relevant facts of a page, each as a list of strings. */
export function extractFacts(doc) {
	const html = elements(doc, (n) => n.tagName === 'html')[0];
	const field = (n, a) => {
		const parts = [n.tagName, a.type && `type=${a.type}`, a.name && `name=${a.name}`];
		if (a.type === 'hidden' || n.tagName === 'option') parts.push(`value=${a.value ?? ''}`);
		return parts.filter(Boolean).join(' ');
	};
	return {
		'html lang': [html ? (attrs(html).lang ?? '(none)') : '(no <html>)'],
		title: elements(doc, (n) => n.tagName === 'title').map(textContent),
		'meta tags': elements(doc, (n) => n.tagName === 'meta').map((n) => {
			const a = attrs(n);
			const key = a.name ? `name=${a.name}` : a.property ? `property=${a.property}` : a.charset ? 'charset' : a['http-equiv'] ? `http-equiv=${a['http-equiv']}` : '?';
			return `${key} → ${a.content ?? a.charset ?? ''}`;
		}),
		canonical: elements(doc, (n, a) => n.tagName === 'link' && a.rel === 'canonical').map((n) => attrs(n).href),
		hreflang: elements(doc, (n, a) => n.tagName === 'link' && a.hreflang !== undefined).map((n) => {
			const a = attrs(n);
			return `${a.hreflang} → ${a.href}`;
		}),
		headings: elements(doc, (n) => /^h[1-6]$/.test(n.tagName)).map((n) => `${n.tagName}: ${textContent(n)}`),
		links: elements(doc, (n, a) => n.tagName === 'a' && a.href !== undefined).map((n) => attrs(n).href),
		forms: elements(doc, (n) => n.tagName === 'form').map((form) => {
			const a = attrs(form);
			const fields = elements(form, (n) => ['input', 'select', 'textarea', 'button', 'option'].includes(n.tagName)).map((n) => field(n, attrs(n)));
			return `action=${a.action ?? '(none)'} method=${a.method ?? '(none)'} fields=[${fields.join('; ')}]`;
		}),
	};
}

/** One line per node, attributes sorted, whitespace collapsed. */
export function canonicalHtml(doc) {
	const lines = [];
	(function visit(node, depth) {
		const indent = '  '.repeat(depth);
		if (node.nodeName === '#document' || node.nodeName === '#document-fragment') {
			for (const c of node.childNodes) visit(c, depth);
			return;
		}
		if (node.nodeName === '#documentType') {
			lines.push(`<!DOCTYPE ${node.name}>`);
			return;
		}
		if (node.nodeName === '#text') {
			const text = collapse(node.value);
			if (text.trim()) lines.push(indent + JSON.stringify(text));
			return;
		}
		if (node.nodeName === '#comment') {
			lines.push(`${indent}<!--${collapse(node.data)}-->`);
			return;
		}
		const a = (node.attrs || [])
			.map((x) => `${x.name}=${JSON.stringify(collapse(x.value))}`)
			.sort();
		lines.push(`${indent}<${node.tagName}${a.length ? ' ' + a.join(' ') : ''}>`);
		for (const c of node.childNodes || []) visit(c, depth + 1);
		if (node.content) visit(node.content, depth + 1);
	})(doc, 0);
	return lines;
}

// ---------------------------------------------------------------- comparisons

function compareLists(label, base, actual) {
	if (JSON.stringify(base) === JSON.stringify(actual)) return [];
	const removed = base.filter((x) => !actual.includes(x));
	const added = actual.filter((x) => !base.includes(x));
	const parts = [];
	if (removed.length) parts.push(`missing ${JSON.stringify(removed)}`);
	if (added.length) parts.push(`unexpected ${JSON.stringify(added)}`);
	if (!parts.length) parts.push(`order changed: was ${JSON.stringify(base)}, now ${JSON.stringify(actual)}`);
	return [`${label}: ${parts.join('; ')}`];
}

/** Shortens two differing lines to a window around their first differing character (minified JS/CSS is one long line). */
function excerpt(a, b, width = 160) {
	if (a === undefined || b === undefined) return [a?.trim() ?? '(end)', b?.trim() ?? '(end)'];
	let i = 0;
	while (i < a.length && a[i] === b[i]) i++;
	const start = Math.max(0, i - width / 2);
	const cut = (s) => (start > 0 ? '…' : '') + s.slice(start, start + width).trim() + (s.length > start + width ? '…' : '');
	return [cut(a), cut(b)];
}

/** Reports the first differing line only: after an insertion or deletion every following line shifts. */
function firstLineDiff(label, baseLines, actualLines) {
	const n = Math.max(baseLines.length, actualLines.length);
	for (let i = 0; i < n; i++) {
		if (baseLines[i] !== actualLines[i]) {
			const [was, now] = excerpt(baseLines[i], actualLines[i]);
			return [`${label} line ${i + 1}:\n      was: ${was}\n      now: ${now}`];
		}
	}
	return [];
}

function compareHtml(file, base, actual) {
	const baseDoc = parse(base);
	const actualDoc = parse(actual);
	const baseFacts = extractFacts(baseDoc);
	const actualFacts = extractFacts(actualDoc);
	const diffs = Object.keys(baseFacts).flatMap((k) => compareLists(`${file} ${k}`, baseFacts[k], actualFacts[k]));
	// Facts can all match while the body still changed (a typo fix, a CSS class...): zero-diff.
	return diffs.concat(firstLineDiff(`${file} markup`, canonicalHtml(baseDoc), canonicalHtml(actualDoc)));
}

export function parseSitemap(xml) {
	const tag = (block, name) => (block.match(new RegExp(`<${name}>\\s*([\\s\\S]*?)\\s*</${name}>`)) || [])[1] ?? '(none)';
	return [...xml.matchAll(/<url>([\s\S]*?)<\/url>/g)].map(([, block]) => {
		const alternates = [...block.matchAll(/<xhtml:link\b([^>]*)\/?>/g)].map(([, a]) => {
			const attr = (name) => (a.match(new RegExp(`${name}="([^"]*)"`)) || [])[1];
			return `${attr('hreflang')} → ${attr('href')}`;
		});
		return {
			loc: tag(block, 'loc'),
			fields: `lastmod=${tag(block, 'lastmod')} changefreq=${tag(block, 'changefreq')} priority=${tag(block, 'priority')}`,
			alternates,
		};
	});
}

function compareSitemap(file, base, actual) {
	const b = parseSitemap(base);
	const a = parseSitemap(actual);
	const diffs = compareLists(`${file} URLs`, b.map((u) => u.loc), a.map((u) => u.loc));
	for (const bu of b) {
		const au = a.find((u) => u.loc === bu.loc);
		if (!au) continue;
		diffs.push(...compareLists(`${file} <${bu.loc}> fields`, [bu.fields], [au.fields]));
		diffs.push(...compareLists(`${file} <${bu.loc}> alternates`, bu.alternates, au.alternates));
	}
	const norm = (s) => collapse(s).replace(/>\s+</g, '><').trim();
	if (!diffs.length && norm(base) !== norm(actual)) diffs.push(`${file}: content differs (outside of URLs, dates and alternates)`);
	return diffs;
}

function compareText(file, base, actual) {
	if (base === actual) return [];
	const lines = (s) => s.replace(/\n+$/, '').split('\n');
	const diffs = firstLineDiff(file, lines(base), lines(actual));
	return diffs.length ? diffs : [`${file}: trailing newline differs`];
}

function compareContent(file, base, actual) {
	if (typeof base !== 'string') return Buffer.compare(base, actual) === 0 ? [] : [`${file}: binary content differs`];
	if (file.endsWith('.html')) return compareHtml(file, base, actual);
	if (file.endsWith('sitemap.xml')) return compareSitemap(file, base, actual);
	return compareText(file, base, actual);
}

/**
 * Compares a reference copy of a site (baseDir) with another one (actualDir).
 * @returns {string[]} differences; empty when the outputs are equivalent.
 */
export function compareOutputs(baseDir, actualDir) {
	let base, actual;
	try {
		base = loadSite(baseDir);
	} catch (e) {
		return [`cannot read ${baseDir}: ${e.message}`];
	}
	try {
		actual = loadSite(actualDir);
	} catch (e) {
		return [`cannot read ${actualDir}: ${e.message}`];
	}

	const diffs = [];
	for (const key of base.keys()) if (!actual.has(key)) diffs.push(`missing file: ${key}`);
	for (const key of actual.keys()) if (!base.has(key)) diffs.push(`unexpected file: ${key}`);

	for (const [key, baseEntries] of base) {
		const actualEntries = actual.get(key);
		if (!actualEntries) continue;
		if (baseEntries.length !== actualEntries.length) {
			diffs.push(`${key}: ${baseEntries.length} file(s) before, ${actualEntries.length} now`);
			continue;
		}
		if (baseEntries.length === 1) {
			diffs.push(...compareContent(key, baseEntries[0].content, actualEntries[0].content));
		} else {
			// Several hashed files with the same name (e.g. two main-*.js chunks): compare as a set.
			const sig = (e) => (typeof e.content === 'string' ? e.content : e.content.toString('base64'));
			const b = baseEntries.map(sig).sort();
			const a = actualEntries.map(sig).sort();
			if (b.some((x, i) => x !== a[i])) diffs.push(`${key}: content of the ${b.length} hashed variants differs`);
		}
	}

	for (const [key, entries] of actual) {
		for (const e of entries) {
			const m = typeof e.content === 'string' && e.content.match(PLACEHOLDER);
			if (m) diffs.push(`${e.path}: unreplaced placeholder ${m[0]}`);
		}
	}
	return diffs;
}
