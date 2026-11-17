// Builds the websites into dist/fr/ and dist/en/:
//   1. Eleventy  src/            -> .11ty/<site>/
//   2. Vite      .11ty/<site>/   -> dist/<site>/   (bundles JS and Bootstrap CSS, hashes file names)
//   3. PurgeCSS  dist/<site>/assets/*.css          (same as the legacy "postbuild" script)
//
//   node scripts/build.mjs                       both sites
//   node scripts/build.mjs --site fr             one site
//   node scripts/build.mjs --dummy-endpoints     use the parity-check endpoints (see lib/sites.mjs)

import { spawnSync } from 'node:child_process';
import { rmSync, writeFileSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { PROJECT_ROOT, currentSite, distDir, dummyEndpoints, eleventyOutDir, parseSiteArgs } from './lib/sites.mjs';

const argv = process.argv.slice(2);

if (!process.env.SITE) {
	// Each site is built in its own process: the Eleventy and Vite configs read SITE when they load.
	for (const site of parseSiteArgs(argv)) {
		const env = { ...process.env, SITE: site, ...(argv.includes('--dummy-endpoints') ? dummyEndpoints(site) : {}) };
		const result = spawnSync(process.execPath, [fileURLToPath(import.meta.url)], { stdio: 'inherit', env, cwd: PROJECT_ROOT });
		if (result.status !== 0) process.exit(result.status ?? 1);
	}
	process.exit(0);
}

const site = currentSite();
process.chdir(PROJECT_ROOT);
const started = Date.now();

// 1. Eleventy
rmSync(eleventyOutDir(site), { recursive: true, force: true });
const { default: Eleventy } = await import('@11ty/eleventy');
const eleventy = new Eleventy(undefined, undefined, { configPath: 'eleventy.config.js', quietMode: true });
await eleventy.write();

// 2. Vite
const { build } = await import('vite');
await build({ configFile: path.join(PROJECT_ROOT, 'vite.config.js'), logLevel: 'warn' });

// 3. PurgeCSS (legacy: purgecss --content dist/*.html dist/assets/*.js --css dist/assets/*.css --output dist/assets/)
const { PurgeCSS } = await import('purgecss');
const dist = path.relative(PROJECT_ROOT, distDir(site)).split(path.sep).join('/');
const purged = await new PurgeCSS().purge({
	content: [`${dist}/*.html`, `${dist}/assets/*.js`],
	css: [`${dist}/assets/*.css`],
});
for (const { file, css } of purged) writeFileSync(file, css);

console.log(`✓ ${site}: built dist/${site}/ in ${((Date.now() - started) / 1000).toFixed(1)}s`);
