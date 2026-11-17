// Snapshot of the deployed sites, and check of a deployment against a snapshot. Made for the cutover:
//
//   node scripts/live.mjs snapshot                         before: save production to live-snapshots/<timestamp>/
//   node scripts/live.mjs check --snapshot live-snapshots/<timestamp>
//                                                          after: compare production with the snapshot
//
// Options:
//   --site fr|en     one site (default: both)
//   --base <url>     host to fetch (default: the site's domain), e.g. a staging URL; one site only
//
// Fetched: every page of src/content/<site>/, robots.txt, sitemap.xml and every same-origin script,
// stylesheet, image and link they reference. Compared: HTTP status, the headers of lib/live.mjs (HEADERS),
// and the file contents with the parity check rules (whitespace, attribute order and asset hashes ignored).
// Also recorded: the status of /.htaccess and of a missing page.

import { existsSync, readdirSync } from 'node:fs';
import path from 'node:path';
import { compareSnapshots, takeSnapshot } from './lib/live.mjs';
import { PROJECT_ROOT, argValue, parseSiteArgs } from './lib/sites.mjs';

const [command, ...argv] = process.argv.slice(2);
const sites = parseSiteArgs(argv);
const base = argValue(argv, '--base');
if (base && sites.length > 1) fail('--base needs --site: each site has its own host.');

async function siteDomain(site) {
	const { default: settings } = await import(`../src/_data/sites/${site}.js`);
	return settings.domain;
}

function seeds(site) {
	const pages = readdirSync(path.join(PROJECT_ROOT, 'src', 'content', site))
		.filter((f) => f.endsWith('.njk'))
		.map((f) => `/${path.parse(f).name}.html`);
	return ['/', ...pages.sort(), '/robots.txt', '/sitemap.xml'];
}

const STATUS_ONLY = ['/.htaccess', '/this-page-does-not-exist.html'];

function fail(message) {
	console.error(message);
	process.exit(2);
}

async function snapshot(site, outDir) {
	const baseUrl = base ?? (await siteDomain(site));
	const responses = await takeSnapshot(baseUrl, seeds(site), STATUS_ONLY, outDir);
	console.log(`✓ ${site}: ${Object.keys(responses).length} URLs of ${baseUrl} saved to ${path.relative(PROJECT_ROOT, outDir)}`);
}

if (command === 'snapshot') {
	const stamp = new Date().toISOString().replace(/[:.]/g, '-').slice(0, 19);
	const root = argValue(argv, '--out') ?? path.join(PROJECT_ROOT, 'live-snapshots', stamp);
	for (const site of sites) await snapshot(site, path.join(root, site));
} else if (command === 'check') {
	const root = argValue(argv, '--snapshot') ?? fail('check needs --snapshot live-snapshots/<timestamp>');
	let failed = false;
	for (const site of sites) {
		const expected = path.resolve(root, site);
		if (!existsSync(path.join(expected, 'responses.json'))) {
			if (sites.length > 1) continue; // that snapshot covers one site only
			fail(`no snapshot of ${site} in ${root}`);
		}
		const actual = path.join(PROJECT_ROOT, '.tmp', 'live', site);
		await snapshot(site, actual);
		const diffs = compareSnapshots(expected, actual);
		if (diffs.length) {
			failed = true;
			console.log(`✗ ${site}: ${diffs.length} difference(s) with ${path.relative(PROJECT_ROOT, expected)}`);
			for (const d of diffs) console.log(`  - ${d}`);
		} else {
			console.log(`✓ ${site}: same as ${path.relative(PROJECT_ROOT, expected)}`);
		}
	}
	process.exit(failed ? 1 : 0);
} else {
	fail('Usage: node scripts/live.mjs snapshot|check [--site fr|en] [--base <url>] [--out <dir>] [--snapshot <dir>]');
}
