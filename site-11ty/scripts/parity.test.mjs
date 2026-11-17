// Proves the parity check is a real safety net: it must pass on harmless changes
// (whitespace, attribute order, new asset hashes) and fail on every regression
// the migration brief cares about. Run with: npm test

import assert from 'node:assert/strict';
import { cpSync, mkdtempSync, readdirSync, readFileSync, renameSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import path from 'node:path';
import { after, describe, test } from 'node:test';
import { compareOutputs, stripHash } from './lib/parity.mjs';
import { PROJECT_ROOT } from './lib/sites.mjs';

const BASELINE = path.join(PROJECT_ROOT, 'baseline', 'fr');
const work = mkdtempSync(path.join(tmpdir(), 'parity-test-'));
after(() => rmSync(work, { recursive: true, force: true }));

let counter = 0;
/** Copies the FR baseline, applies `mutate(dir)`, returns the differences found. */
function diffAfter(mutate) {
	const dir = path.join(work, String(counter++));
	cpSync(BASELINE, dir, { recursive: true });
	mutate(dir);
	return compareOutputs(BASELINE, dir);
}

/** Replaces text in a file and fails the test if the text was not there (keeps tests honest). */
function edit(dir, file, from, to) {
	const full = path.join(dir, file);
	const before = readFileSync(full, 'utf8');
	assert.ok(before.includes(from), `test setup: "${from}" not found in ${file}`);
	writeFileSync(full, before.replace(from, to));
}

function asset(dir, prefix, ext) {
	const name = readdirSync(path.join(dir, 'assets')).find((f) => stripHash(`assets/${f}`) === `assets/${prefix}${ext}`);
	assert.ok(name, `test setup: no asset ${prefix}-*${ext}`);
	return `assets/${name}`;
}

function assertFlags(diffs, ...needles) {
	assert.ok(diffs.length > 0, 'expected the parity check to fail, it passed');
	for (const needle of needles) {
		assert.ok(diffs.some((d) => d.includes(needle)), `expected a difference mentioning "${needle}", got:\n${diffs.join('\n')}`);
	}
}

describe('passes on harmless changes', () => {
	test('identical copy', () => {
		assert.deepEqual(diffAfter(() => {}), []);
	});

	test('re-indented HTML and CRLF line endings', () => {
		const diffs = diffAfter((dir) => {
			for (const f of ['index.html', 'sign-up.html', 'legal.html']) {
				const full = path.join(dir, f);
				writeFileSync(full, readFileSync(full, 'utf8').replace(/\n\t*/g, '\r\n    '));
			}
		});
		assert.deepEqual(diffs, []);
	});

	test('attribute order', () => {
		const diffs = diffAfter((dir) => edit(dir, 'index.html', '<link rel="canonical" href="https://www.alertegelee.fr/" />', '<link href="https://www.alertegelee.fr/" rel="canonical" />'));
		assert.deepEqual(diffs, []);
	});

	test('new content hash on an asset, references updated', () => {
		const diffs = diffAfter((dir) => {
			const old = asset(dir, 'sign-up', '.js');
			const renamed = 'assets/sign-up-ZZZZZZZZ.js';
			renameSync(path.join(dir, old), path.join(dir, renamed));
			edit(dir, 'sign-up.html', path.posix.basename(old), path.posix.basename(renamed));
		});
		assert.deepEqual(diffs, []);
	});
});

describe('fails on regressions', () => {
	test('missing page', () => {
		assertFlags(diffAfter((dir) => rmSync(path.join(dir, 'donate.html'))), 'missing file: donate.html');
	});

	test('page at Eleventy default path instead of flat .html', () => {
		const diffs = diffAfter((dir) => {
			cpSync(path.join(dir, 'donate.html'), path.join(dir, 'donate', 'index.html'), { recursive: true });
			rmSync(path.join(dir, 'donate.html'));
		});
		assertFlags(diffs, 'missing file: donate.html', 'unexpected file: donate/index.html');
	});

	test('.htaccess lost in copy step', () => {
		assertFlags(diffAfter((dir) => rmSync(path.join(dir, '.htaccess'))), 'missing file: .htaccess');
	});

	test('changed canonical', () => {
		const diffs = diffAfter((dir) => edit(dir, 'index.html', 'rel="canonical" href="https://www.alertegelee.fr/"', 'rel="canonical" href="https://www.alertegelee.fr/index.html"'));
		assertFlags(diffs, 'index.html canonical');
	});

	test('hreflang pointing at the wrong domain', () => {
		const diffs = diffAfter((dir) => edit(dir, 'sign-up.html', 'hreflang="en" href="https://www.frostalert.net/sign-up.html"', 'hreflang="en" href="https://www.alertegelee.fr/sign-up.html"'));
		assertFlags(diffs, 'sign-up.html hreflang');
	});

	test('changed title', () => {
		assertFlags(diffAfter((dir) => edit(dir, 'sign-up.html', '<title>Inscription - Alerte gelées</title>', '<title>Inscription | Alerte gelées</title>')), 'sign-up.html title');
	});

	test('changed html lang', () => {
		assertFlags(diffAfter((dir) => edit(dir, 'legal.html', '<html lang="fr">', '<html lang="en">')), 'legal.html html lang');
	});

	test('changed heading', () => {
		assertFlags(diffAfter((dir) => edit(dir, 'legal.html', "Charte d'utilisation du service</h1>", "Charte d’utilisation du service</h1>")), 'legal.html headings');
	});

	test('form posts to the other site endpoint', () => {
		assertFlags(diffAfter((dir) => edit(dir, 'sign-up.html', 'action="https://fr.parity.invalid/signup"', 'action="https://en.parity.invalid/signup"')), 'sign-up.html forms');
	});

	test('hidden lang field removed', () => {
		assertFlags(diffAfter((dir) => edit(dir, 'sign-up.html', '<input type="hidden" id="lang" name="lang" value="fr" />', '')), 'sign-up.html forms');
	});

	test('placeholder left unreplaced', () => {
		assertFlags(diffAfter((dir) => edit(dir, 'sign-up.html', 'action="https://fr.parity.invalid/signup"', 'action="%VITE_SIGNUP_URL%"')), 'unreplaced placeholder %VITE_SIGNUP_URL%');
	});

	test('typo fixed in body text (zero-diff principle)', () => {
		const diffs = diffAfter((dir) => {
			const full = path.join(dir, 'donate.html');
			const html = readFileSync(full, 'utf8');
			const text = html.match(/<p[^>]*>\s*([A-Za-zÀ-ÿ]{6,})/);
			assert.ok(text, 'test setup: no paragraph in donate.html');
			writeFileSync(full, html.replace(text[1], text[1].toUpperCase()));
		});
		assertFlags(diffs, 'donate.html markup');
	});

	test('CSS class changed', () => {
		assertFlags(diffAfter((dir) => edit(dir, 'legal.html', '<h1 class="fw-light mb-3">', '<h1 class="fw-light mb-4">')), 'legal.html markup');
	});

	test('JS user-facing string in the wrong language', () => {
		const diffs = diffAfter((dir) => edit(dir, asset(dir, 'sign-up', '.js'), 'Veuillez patienter...', 'Please wait...'));
		assertFlags(diffs, 'assets/sign-up.js');
	});

	test('sitemap lastmod changed to build date', () => {
		assertFlags(diffAfter((dir) => edit(dir, 'sitemap.xml', '<lastmod>2026-01-21</lastmod>', '<lastmod>2026-11-17</lastmod>')), 'sitemap.xml <https://www.alertegelee.fr/> fields');
	});

	test('sitemap x-default alternate dropped from home page', () => {
		const diffs = diffAfter((dir) => {
			const full = path.join(dir, 'sitemap.xml');
			const xml = readFileSync(full, 'utf8');
			assert.match(xml, /<xhtml:link[^>]*hreflang="x-default"[^>]*\/>/, 'test setup: no x-default alternate');
			writeFileSync(full, xml.replace(/<xhtml:link[^>]*hreflang="x-default"[^>]*\/>/, ''));
		});
		assertFlags(diffs, 'alternates');
	});

	test('robots.txt sitemap on the wrong domain', () => {
		assertFlags(diffAfter((dir) => edit(dir, 'robots.txt', 'https://www.alertegelee.fr', 'https://www.frostalert.net')), 'robots.txt');
	});

	test('.htaccess CSP host removed', () => {
		assertFlags(diffAfter((dir) => edit(dir, '.htaccess', ' https://frostalertsupport.azurewebsites.net', '')), '.htaccess');
	});

	test('unexpected extra file', () => {
		assertFlags(diffAfter((dir) => writeFileSync(path.join(dir, 'page.html'), '<!doctype html><title>x</title>')), 'unexpected file: page.html');
	});
});
