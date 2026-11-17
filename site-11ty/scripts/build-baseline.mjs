// Builds the legacy Vite sites (site/ and site-en/) with dummy endpoints and saves
// their output as the parity baseline.
//
//   node scripts/build-baseline.mjs            rebuild baseline/<site>/ (commit the result)
//   node scripts/build-baseline.mjs --verify   rebuild into .tmp/ and compare with baseline/
//   ... --site fr                              only one site
//
// The legacy folders are never modified: the build only writes their (git-ignored)
// node_modules/ and dist/, and the script fails if any tracked file changes.

import { spawnSync } from 'node:child_process';
import { cpSync, existsSync, rmSync } from 'node:fs';
import path from 'node:path';
import { compareOutputs } from './lib/parity.mjs';
import { PROJECT_ROOT, REPO_ROOT, SITES, dummyEndpoints, parseSiteArgs } from './lib/sites.mjs';

const argv = process.argv.slice(2);
const verify = argv.includes('--verify');
const sites = parseSiteArgs(argv);

function npm(args, options) {
	// When started through "npm run", call npm's own CLI with node directly (no shell needed, also on Windows).
	const npmCli = process.env.npm_execpath;
	const result = npmCli
		? spawnSync(process.execPath, [npmCli, ...args], { stdio: 'inherit', ...options })
		: spawnSync('npm', args, { stdio: 'inherit', shell: process.platform === 'win32', ...options });
	if (result.status !== 0) {
		throw new Error(`"npm ${args.join(' ')}" failed in ${options.cwd} (exit ${result.status})`);
	}
}

function gitStatus(dir) {
	const result = spawnSync('git', ['status', '--porcelain', '--', dir], { cwd: REPO_ROOT, encoding: 'utf8' });
	if (result.status !== 0) throw new Error(`git status failed: ${result.stderr}`);
	return result.stdout;
}

let failed = false;

for (const site of sites) {
	const legacyDir = path.join(REPO_ROOT, SITES[site].legacyDir);
	const legacyDist = path.join(legacyDir, 'dist');
	const target = verify
		? path.join(PROJECT_ROOT, '.tmp', 'baseline', site)
		: path.join(PROJECT_ROOT, 'baseline', site);

	console.log(`\n=== ${site}: building ${SITES[site].legacyDir}/ with dummy endpoints`);
	const statusBefore = gitStatus(legacyDir);

	npm(['ci', '--no-audit', '--no-fund'], { cwd: legacyDir });
	// Vite gives process.env VITE_* variables priority over .env files,
	// so no .env.production needs to be written into the legacy folder.
	npm(['run', 'build'], { cwd: legacyDir, env: { ...process.env, ...dummyEndpoints(site) } });

	if (gitStatus(legacyDir) !== statusBefore) {
		throw new Error(`Building ${SITES[site].legacyDir}/ changed tracked files. It must stay untouched.`);
	}
	if (!existsSync(path.join(legacyDist, '.htaccess'))) {
		throw new Error(`${SITES[site].legacyDir}/dist/.htaccess is missing after the build.`);
	}

	rmSync(target, { recursive: true, force: true });
	cpSync(legacyDist, target, { recursive: true });
	console.log(`=== ${site}: output copied to ${path.relative(PROJECT_ROOT, target)}/`);

	if (verify) {
		const differences = compareOutputs(path.join(PROJECT_ROOT, 'baseline', site), target);
		if (differences.length) {
			failed = true;
			console.error(`\n✗ ${site}: legacy build no longer matches baseline/${site}/ (${differences.length} difference(s)):`);
			for (const d of differences) console.error(`  - ${d}`);
			console.error('  If site/ or site-en/ changed on purpose, run "npm run baseline" and commit the new baseline.');
		} else {
			console.log(`✓ ${site}: legacy build matches baseline/${site}/`);
		}
	}
}

if (failed) process.exit(1);
