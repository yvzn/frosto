// Compares the new build (dist/<site>/) with the legacy baseline (baseline/<site>/).
// Exit code 0 = no SEO / behaviour difference, 1 = differences found.
//
//   node scripts/parity-check.mjs                    both sites
//   node scripts/parity-check.mjs --site fr          one site
//   node scripts/parity-check.mjs --site fr --baseline <dir> --actual <dir>

import path from 'node:path';
import { compareOutputs } from './lib/parity.mjs';
import { PROJECT_ROOT, argValue, parseSiteArgs } from './lib/sites.mjs';

const argv = process.argv.slice(2);
const sites = parseSiteArgs(argv);
let failed = false;

for (const site of sites) {
	const baseDir = path.resolve(argValue(argv, '--baseline') ?? path.join(PROJECT_ROOT, 'baseline', site));
	const actualDir = path.resolve(argValue(argv, '--actual') ?? path.join(PROJECT_ROOT, 'dist', site));
	const differences = compareOutputs(baseDir, actualDir);
	if (differences.length) {
		failed = true;
		console.error(`✗ ${site}: ${differences.length} difference(s) between ${path.relative(PROJECT_ROOT, baseDir)} and ${path.relative(PROJECT_ROOT, actualDir)}`);
		for (const d of differences) console.error(`  - ${d}`);
	} else {
		console.log(`✓ ${site}: ${path.relative(PROJECT_ROOT, actualDir)} matches ${path.relative(PROJECT_ROOT, baseDir)}`);
	}
}

process.exit(failed ? 1 : 0);
