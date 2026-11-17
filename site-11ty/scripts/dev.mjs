// Dev loop: for each site, Eleventy (watch) renders into .11ty/<site>/ and a Vite dev server
// serves that folder with live reload.
//   French:  http://localhost:5180/
//   English: http://localhost:5181/
//
//   node scripts/dev.mjs             both sites
//   node scripts/dev.mjs --site fr   one site
//
// Editing a template re-renders the HTML (Vite reloads the page); editing src/assets/
// copies the file again (Vite hot-reloads CSS/JS).

import { spawn } from 'node:child_process';
import { rmSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { PROJECT_ROOT, currentSite, eleventyOutDir, parseSiteArgs } from './lib/sites.mjs';

if (!process.env.SITE) {
	// One process per site: the Eleventy and Vite configs read SITE when they load.
	const children = parseSiteArgs(process.argv.slice(2)).map((site) => {
		const child = spawn(process.execPath, [fileURLToPath(import.meta.url)], {
			cwd: PROJECT_ROOT,
			env: { ...process.env, SITE: site, FORCE_COLOR: '1' },
			stdio: ['inherit', 'pipe', 'pipe'],
		});
		for (const stream of ['stdout', 'stderr']) {
			let buffer = '';
			child[stream].on('data', (chunk) => {
				buffer += chunk;
				const lines = buffer.split('\n');
				buffer = lines.pop();
				for (const line of lines) process[stream].write(`[${site}] ${line}\n`);
			});
		}
		child.on('exit', (code) => {
			console.error(`[${site}] stopped (exit ${code})`);
			for (const other of children) other.kill();
			process.exitCode = code ?? 1;
		});
		return child;
	});
	process.on('SIGINT', () => children.forEach((c) => c.kill()));
} else {
	const site = currentSite();
	process.chdir(PROJECT_ROOT);
	rmSync(eleventyOutDir(site), { recursive: true, force: true });

	const { default: Eleventy } = await import('@11ty/eleventy');
	const eleventy = new Eleventy(undefined, undefined, { configPath: 'eleventy.config.js' });
	await eleventy.write();
	await eleventy.watch();

	const { createServer } = await import('vite');
	const server = await createServer({ configFile: path.join(PROJECT_ROOT, 'vite.config.js') });
	await server.listen();
	server.printUrls();
}
