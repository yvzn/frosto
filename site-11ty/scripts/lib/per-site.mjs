// Runs a long-lived script (dev or preview server) once per site, in its own process:
// the Eleventy and Vite configs read SITE when they load. Output lines are prefixed with the site,
// and stopping one stops all.

import { spawn } from 'node:child_process';
import { PROJECT_ROOT, parseSiteArgs } from './sites.mjs';

export function runPerSite(scriptPath, argv) {
	const children = parseSiteArgs(argv).map((site) => {
		const child = spawn(process.execPath, [scriptPath], {
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
}
