// Prints the A4 PDF flyers of the share page into src/public/asset/:
//   flyer-Partager-Alerte-Gelees.pdf  (fr)
//   flyer-Share-Frost-Alerts.pdf      (en)
//
//   node scripts/flyers.mjs             both sites
//   node scripts/flyers.mjs --site fr   one site
//
// Not part of the build: run it by hand after changing the texts below, the site name or domain
// (src/_data/sites/) or the hero illustration (src/assets/asset/frost-alert-hero.svg).
// Needs Chrome or Edge (set BROWSER to its path if not found) and network access: the page loads
// the QR code library from cdnjs.

import { execFileSync } from 'node:child_process';
import { copyFileSync, existsSync, mkdtempSync, rmSync, writeFileSync } from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { pathToFileURL } from 'node:url';
import { PROJECT_ROOT, parseSiteArgs } from './lib/sites.mjs';

const FLYERS = {
	fr: {
		file: 'flyer-Partager-Alerte-Gelees.pdf',
		headline: 'Anticipez les gelées',
		lead: 'Recevez gratuitement une alerte lorsque la météo prévoit des températures négatives dans votre ville.',
		scan: 'Scannez pour vous inscrire',
		footer: 'Service gratuit • Sans publicité',
	},
	en: {
		file: 'flyer-Share-Frost-Alerts.pdf',
		headline: 'Anticipate frost',
		lead: 'Receive a free alert when the weather forecast predicts negative temperatures in your area.',
		scan: 'Scan to sign up',
		footer: 'Free service • No advertising',
	},
};

const BROWSERS = [
	process.env.BROWSER,
	'C:/Program Files/Google/Chrome/Application/chrome.exe',
	'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe',
	'/Applications/Google Chrome.app/Contents/MacOS/Google Chrome',
	'/usr/bin/google-chrome',
	'/usr/bin/chromium',
];

function findBrowser() {
	const browser = BROWSERS.find((candidate) => candidate && existsSync(candidate));
	if (!browser) throw new Error('Chrome or Edge not found: set BROWSER to its path');
	return browser;
}

function page(site, flyer) {
	return `<!DOCTYPE html>
<html lang="${site.lang}">
<head>
<meta charset="UTF-8">
<title>${site.name}</title>
<script src="https://cdnjs.cloudflare.com/ajax/libs/qrcode-generator/1.4.4/qrcode.min.js"></script>
<style>
@page { size: A4; margin: 0; }
* { box-sizing: border-box; margin: 0; }
html, body { width: 210mm; height: 297mm; }
body {
	font-family: system-ui, -apple-system, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif;
	color: #212529; background: #fff; display: flex; flex-direction: column;
	-webkit-print-color-adjust: exact; print-color-adjust: exact;
}
header { background: #212529; color: #fff; padding: 9mm 16mm; font-size: 20pt; }
main { flex: 1; padding: 14mm 16mm 0; display: flex; flex-direction: column; align-items: center; text-align: center; }
.hero { width: 150mm; }
h1 { font-weight: 300; font-size: 40pt; margin-top: 10mm; }
.lead { font-weight: 300; font-size: 17pt; color: #6c757d; line-height: 1.45; margin-top: 5mm; max-width: 150mm; }
.qr { margin-top: 12mm; width: 52mm; height: 52mm; }
.qr svg { width: 100%; height: 100%; display: block; }
.scan { font-size: 15pt; margin-top: 3mm; }
.host { font-size: 17pt; color: #0d6efd; margin-top: 2mm; }
.free { font-size: 12pt; color: #6c757d; margin: auto 0 8mm; }
footer { height: 8mm; background: #0d6efd; }
</style>
</head>
<body>
<header>${site.name}</header>
<main>
	<img class="hero" src="hero.svg" alt="">
	<h1>${flyer.headline}</h1>
	<p class="lead">${flyer.lead}</p>
	<div class="qr" id="qr"></div>
	<p class="scan">${flyer.scan}</p>
	<p class="host">${new URL(site.domain).host}</p>
	<p class="free">${flyer.footer}</p>
</main>
<footer></footer>
<script>
const qr = qrcode(0, 'M');
qr.addData('${site.domain}/');
qr.make();
const n = qr.getModuleCount(), quiet = 2;
let d = '';
for (let r = 0; r < n; r++) for (let c = 0; c < n; c++) if (qr.isDark(r, c)) d += 'M' + (c + quiet) + ' ' + (r + quiet) + 'h1v1h-1z';
document.getElementById('qr').innerHTML = '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 ' + (n + 2 * quiet) + ' ' + (n + 2 * quiet)
	+ '" shape-rendering="crispEdges"><path fill="#212529" d="' + d + '"/></svg>';
</script>
</body>
</html>
`;
}

const browser = findBrowser();
const workDir = mkdtempSync(path.join(os.tmpdir(), 'flyers-'));
try {
	copyFileSync(path.join(PROJECT_ROOT, 'src/assets/asset/frost-alert-hero.svg'), path.join(workDir, 'hero.svg'));
	for (const lang of parseSiteArgs(process.argv.slice(2))) {
		const { default: site } = await import(pathToFileURL(path.join(PROJECT_ROOT, `src/_data/sites/${lang}.js`)));
		const flyer = FLYERS[lang];
		const html = path.join(workDir, `flyer-${lang}.html`);
		writeFileSync(html, page(site, flyer));
		const pdf = path.join(PROJECT_ROOT, 'src/public/asset', flyer.file);
		execFileSync(browser, [
			'--headless', '--disable-gpu', '--no-pdf-header-footer', '--virtual-time-budget=10000',
			`--print-to-pdf=${pdf}`, pathToFileURL(html).href,
		], { stdio: 'ignore' });
		if (!existsSync(pdf)) throw new Error(`${lang}: ${flyer.file} not written`);
		console.log(`✓ ${lang}: src/public/asset/${flyer.file}`);
	}
} finally {
	rmSync(workDir, { recursive: true, force: true });
}
