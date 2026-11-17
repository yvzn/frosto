// Tests of the live snapshot and check (lib/live.mjs), against a fake host serving scripts/fixtures/site/ from disk.

import assert from 'node:assert/strict';
import { existsSync, readFileSync, readdirSync, rmSync } from 'node:fs';
import path from 'node:path';
import { after, test } from 'node:test';
import { compareSnapshots, findReferences, takeSnapshot } from './lib/live.mjs';
import { PROJECT_ROOT } from './lib/sites.mjs';

const SITE = path.join(PROJECT_ROOT, 'scripts', 'fixtures', 'site');
const TMP = path.join(PROJECT_ROOT, '.tmp', 'live-test');
const BASE = 'https://www.alertegelee.fr';
const SEEDS = ['/', '/sign-up.html', '/contact.html', '/robots.txt', '/sitemap.xml'];
const STATUS_ONLY = ['/.htaccess', '/missing.html'];

const FAVICON = `/assets/${readdirSync(path.join(SITE, 'assets')).find((f) => f.startsWith('favicon-'))}`;

const TYPES = { html: 'text/html', js: 'text/javascript', css: 'text/css', svg: 'image/svg+xml', txt: 'text/plain', xml: 'application/xml' };

/** A host serving the fixture site, with hooks to alter one response. */
function fakeHost({ headers = {}, missing = [], replace = null } = {}) {
	return async (url) => {
		const p = new URL(url).pathname;
		const file = p === '/' ? 'index.html' : p.slice(1);
		const full = path.join(SITE, file);
		if (p === '/.htaccess') return new Response('', { status: 403 });
		if (missing.includes(p) || !existsSync(full)) return new Response('not found', { status: 404 });
		let body = readFileSync(full);
		if (replace && file.endsWith(replace.file)) body = Buffer.from(body.toString('utf8').replace(replace.from, replace.to));
		const ext = path.extname(file).slice(1);
		return new Response(body, {
			status: 200,
			headers: { 'content-type': TYPES[ext] ?? 'application/octet-stream', 'x-frame-options': 'deny', date: new Date().toUTCString(), ...headers },
		});
	};
}

async function snapshot(name, host) {
	const dir = path.join(TMP, name);
	await takeSnapshot(BASE, SEEDS, STATUS_ONLY, dir, host);
	return dir;
}

after(() => rmSync(TMP, { recursive: true, force: true }));

test('finds same-site references in pages, scripts and styles', () => {
	assert.deepEqual(
		findReferences('a.html', '<link rel="stylesheet" href="./assets/main-AbCdEfGh.css"><a href="mailto:x">m</a><form action="%VITE_SIGNUP_URL%"></form><img src="./asset/x.svg"><a href="#top">t</a>'),
		['./assets/main-AbCdEfGh.css', './asset/x.svg'],
	);
	assert.deepEqual(findReferences('assets/a.js', 'import"./main-AbCdEfGh.js";const x=await import(`./lazy-12345678.js`)'), ['./main-AbCdEfGh.js', './lazy-12345678.js']);
	assert.deepEqual(findReferences('assets/a.css', 'a{background:url(../asset/x.svg)}b{background:url("data:image/png;base64,AA")}'), ['../asset/x.svg']);
});

test('fetches the seeds and everything they reference', async () => {
	const dir = await snapshot('crawl', fakeHost());
	const { responses } = JSON.parse(readFileSync(path.join(dir, 'responses.json'), 'utf8'));
	assert.equal(responses['/.htaccess'].status, 403);
	assert.equal(responses['/missing.html'].status, 404);
	for (const p of ['/', '/index.html', '/legal.html', '/robots.txt', '/sitemap.xml']) assert.equal(responses[p]?.status, 200, p);
	assert.ok(Object.keys(responses).some((p) => /^\/assets\/main-.+\.css$/.test(p)), 'stylesheet');
	assert.ok(Object.keys(responses).some((p) => /^\/assets\/sign-up-.+\.js$/.test(p)), 'page script');
	assert.ok(existsSync(path.join(dir, 'files', FAVICON)), 'icon saved');
	assert.equal(responses['/'].headers['x-frame-options'], 'deny');
	assert.equal(responses['/'].headers.date, undefined, 'volatile headers not recorded');
});

test('two snapshots of the same host are equal', async () => {
	assert.deepEqual(compareSnapshots(await snapshot('a', fakeHost()), await snapshot('b', fakeHost())), []);
});

test('reports a changed header, a missing file and a changed page', async () => {
	const before = await snapshot('before', fakeHost());
	const diffs = compareSnapshots(before, await snapshot('header', fakeHost({ headers: { 'x-frame-options': 'sameorigin' } })));
	assert.ok(diffs.some((d) => d.startsWith('/: x-frame-options')), diffs.join('\n'));

	const missing = compareSnapshots(before, await snapshot('missing', fakeHost({ missing: [FAVICON] })));
	assert.ok(missing.some((d) => d === '/assets/favicon.svg: status 200 -> 404'), missing.join('\n'));

	const changed = compareSnapshots(before, await snapshot('changed', fakeHost({ replace: { file: 'contact.html', from: 'Nous contacter</h1>', to: 'Contact</h1>' } })));
	assert.ok(changed.some((d) => d.startsWith('contact.html headings')), changed.join('\n'));
});
