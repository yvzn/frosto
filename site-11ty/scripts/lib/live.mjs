// Snapshot of a deployed site, and comparison of two snapshots (see scripts/live.mjs).
//
// A snapshot is a folder:
//   files/            every page and asset fetched, at its URL path (same layout as dist/<site>/)
//   responses.json    per URL path: HTTP status and the headers that matter (see HEADERS)
// so that the file contents can be compared with lib/compare.mjs:
// same normalization of whitespace, attribute order and asset hashes.

import { mkdirSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import path from 'node:path';
import { parse } from 'parse5';
import { compareOutputs, stripHash } from './compare.mjs';

/** Response headers compared between snapshots. Others (date, etag, server…) vary from one request to the next. */
export const HEADERS = [
	'content-type',
	'cache-control',
	'content-security-policy',
	'x-frame-options',
	'x-content-type-options',
	'strict-transport-security',
	'referrer-policy',
	'location',
];

const TEXT = /\.(html|xml|txt|js|css|svg)$/;

/** Same-origin references to fetch, found in a page or asset: scripts, styles, images, links, imports, url(). */
export function findReferences(file, content) {
	const refs = [];
	if (file.endsWith('.html')) {
		(function visit(node) {
			for (const { name, value } of node.attrs || []) {
				if (['src', 'href'].includes(name) && !/^(#|mailto:|tel:|javascript:|%VITE_)/.test(value)) refs.push(value);
			}
			for (const child of node.childNodes || []) visit(child);
		})(parse(content));
	} else if (file.endsWith('.js')) {
		for (const [, ref] of content.matchAll(/(?:\bimport|\bfrom)\s*\(?\s*["'`](\.{1,2}\/[^"'`]+)["'`]/g)) refs.push(ref);
	} else if (file.endsWith('.css')) {
		for (const [, ref] of content.matchAll(/url\(\s*["']?([^"')]+)["']?\s*\)/g)) if (!ref.startsWith('data:')) refs.push(ref);
	}
	return refs;
}

/** "/" -> "index.html", "/assets/main-AbC.js" -> "assets/main-AbC.js". */
function filePath(pathname) {
	const p = decodeURIComponent(pathname).replace(/^\/+/, '');
	return p === '' || p.endsWith('/') ? `${p}index.html` : p;
}

/**
 * Fetches a site from its seed paths, following same-origin references, into outDir.
 * @param {string} baseUrl            e.g. https://www.alertegelee.fr
 * @param {string[]} seeds            paths to fetch and follow, e.g. ['/', '/sign-up.html', '/robots.txt']
 * @param {string[]} statusOnly       paths whose status and headers are recorded, content not saved nor followed
 * @param {typeof fetch} fetchFn
 */
export async function takeSnapshot(baseUrl, seeds, statusOnly, outDir, fetchFn = fetch) {
	const origin = new URL(baseUrl).origin;
	rmSync(outDir, { recursive: true, force: true });
	mkdirSync(path.join(outDir, 'files'), { recursive: true });

	const responses = {};
	const queue = seeds.map((p) => new URL(p, origin));
	const seen = new Set();

	async function get(url) {
		const response = await fetchFn(url, { redirect: 'manual', headers: { 'user-agent': 'frosto-live-check' } });
		const headers = Object.fromEntries(HEADERS.map((h) => [h, response.headers.get(h)]).filter(([, v]) => v !== null));
		responses[url.pathname] = { status: response.status, headers };
		return response;
	}

	for (const p of statusOnly) await get(new URL(p, origin));

	while (queue.length) {
		const url = queue.shift();
		if (url.origin !== origin || seen.has(url.pathname)) continue;
		seen.add(url.pathname);
		const response = await get(url);
		if (response.status !== 200) continue;

		const file = filePath(url.pathname);
		const body = Buffer.from(await response.arrayBuffer());
		mkdirSync(path.dirname(path.join(outDir, 'files', file)), { recursive: true });
		writeFileSync(path.join(outDir, 'files', file), body);
		if (!TEXT.test(file)) continue;
		for (const ref of findReferences(file, body.toString('utf8'))) {
			const next = new URL(ref, url);
			next.hash = '';
			next.search = '';
			queue.push(next);
		}
	}

	writeFileSync(path.join(outDir, 'responses.json'), `${JSON.stringify({ baseUrl, takenAt: new Date().toISOString(), responses }, null, '\t')}\n`);
	return responses;
}

/** Responses keyed by path with asset hashes removed: /assets/main-AbC.js -> /assets/main.js. */
function byStableKey(responses) {
	const map = new Map();
	for (const [p, r] of Object.entries(responses)) {
		const key = `/${stripHash(p.replace(/^\//, ''))}`;
		if (!map.has(key)) map.set(key, r);
	}
	return map;
}

/** Differences in status codes and headers between two snapshots' responses. */
export function compareResponses(base, actual) {
	const b = byStableKey(base);
	const a = byStableKey(actual);
	const diffs = [];
	for (const key of b.keys()) if (!a.has(key)) diffs.push(`${key}: not fetched (no longer referenced?)`);
	for (const key of a.keys()) if (!b.has(key)) diffs.push(`${key}: new URL`);
	for (const [key, br] of b) {
		const ar = a.get(key);
		if (!ar) continue;
		if (br.status !== ar.status) diffs.push(`${key}: status ${br.status} -> ${ar.status}`);
		for (const h of HEADERS) {
			if ((br.headers[h] ?? null) !== (ar.headers[h] ?? null)) {
				diffs.push(`${key}: ${h}\n      was: ${br.headers[h] ?? '(none)'}\n      now: ${ar.headers[h] ?? '(none)'}`);
			}
		}
	}
	return diffs;
}

/** All differences between two snapshot folders: statuses, headers, then file contents. */
export function compareSnapshots(baseDir, actualDir) {
	const read = (dir) => JSON.parse(readFileSync(path.join(dir, 'responses.json'), 'utf8')).responses;
	return [
		...compareResponses(read(baseDir), read(actualDir)),
		...compareOutputs(path.join(baseDir, 'files'), path.join(actualDir, 'files')),
	];
}
