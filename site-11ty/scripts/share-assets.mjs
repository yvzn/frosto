// Renders the share assets of each site into src/public/asset/:
//   flyer-Partager-Alerte-Gelees.pdf, flyer-Share-Frost-Alerts.pdf  A4 flyer (share page)
//   share-fr.png, share-en.png                                      1200x630 social preview (og:image)
//
//   node scripts/share-assets.mjs             both sites
//   node scripts/share-assets.mjs --site fr   one site
//
// Not part of the build: run it by hand after changing the texts below, the site name or domain
// (src/_data/sites/) or the hero illustration (src/assets/asset/frost-alert-hero.svg).
// Needs Chrome or Edge (set BROWSER to its path if not found) and network access: the flyer loads
// the QR code library from cdnjs.

import { execFileSync } from 'node:child_process';
import { copyFileSync, existsSync, mkdtempSync, rmSync, statSync, writeFileSync } from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { pathToFileURL } from 'node:url';
import { PROJECT_ROOT, parseSiteArgs } from './lib/sites.mjs';

// Headline and lead: same as the home page (src/content/<site>/index.njk).
const TEXTS = {
	fr: {
		flyer: 'flyer-Partager-Alerte-Gelees.pdf',
		headline: 'Anticipez les gelées',
		lead: 'Recevez gratuitement une alerte lorsque la météo prévoit des températures négatives dans votre ville.',
		scan: 'Scannez pour vous inscrire',
		footer: 'Service gratuit • Sans publicité',
	},
	en: {
		flyer: 'flyer-Share-Frost-Alerts.pdf',
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

// Bootstrap 5 colors and font stack, as on the site.
const STYLE = `
* { box-sizing: border-box; margin: 0; }
body {
	font-family: system-ui, -apple-system, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif;
	color: #212529; background: #fff; display: flex; flex-direction: column; overflow: hidden;
	-webkit-print-color-adjust: exact; print-color-adjust: exact;
}
header { background: #212529; color: #fff; }
h1 { font-weight: 300; }
.lead { font-weight: 300; color: #6c757d; line-height: 1.45; }
.host { color: #0d6efd; }
.muted { color: #6c757d; }
footer { background: #0d6efd; }
`;

function flyerPage(site, t) {
	return `<!DOCTYPE html>
<html lang="${site.lang}">
<head>
<meta charset="UTF-8">
<title>${site.name}</title>
<script src="https://cdnjs.cloudflare.com/ajax/libs/qrcode-generator/1.4.4/qrcode.min.js"></script>
<style>${STYLE}
@page { size: A4; margin: 0; }
html, body { width: 210mm; height: 297mm; }
header { padding: 9mm 16mm; font-size: 20pt; }
main { flex: 1; padding: 14mm 16mm 0; display: flex; flex-direction: column; align-items: center; text-align: center; }
.hero { width: 150mm; }
h1 { font-size: 40pt; margin-top: 10mm; }
.lead { font-size: 17pt; margin-top: 5mm; max-width: 150mm; }
.qr { margin-top: 12mm; width: 52mm; height: 52mm; }
.qr svg { width: 100%; height: 100%; display: block; }
.scan { font-size: 15pt; margin-top: 3mm; }
.host { font-size: 17pt; margin-top: 2mm; }
.muted { font-size: 12pt; margin: auto 0 8mm; }
footer { height: 8mm; }
</style>
</head>
<body>
<header>${site.name}</header>
<main>
	<img class="hero" src="hero.svg" alt="">
	<h1>${t.headline}</h1>
	<p class="lead">${t.lead}</p>
	<div class="qr" id="qr"></div>
	<p class="scan">${t.scan}</p>
	<p class="host">${new URL(site.domain).host}</p>
	<p class="muted">${t.footer}</p>
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

// Social networks may crop the edges: keep the content inside a safe margin.
function socialPage(site, t) {
	return `<!DOCTYPE html>
<html lang="${site.lang}">
<head>
<meta charset="UTF-8">
<title>${site.name}</title>
<style>${STYLE}
html, body { width: 1200px; height: 630px; }
header { padding: 26px 70px; font-size: 32px; }
main { flex: 1; padding: 0 70px; display: flex; align-items: center; gap: 40px; }
.text { flex: 1; }
h1 { font-size: 64px; line-height: 1.1; }
.lead { font-size: 28px; margin-top: 20px; }
.host { font-size: 28px; margin-top: 28px; }
.hero { width: 470px; }
footer { height: 16px; }
</style>
</head>
<body>
<header>${site.name}</header>
<main>
	<div class="text">
		<h1>${t.headline}</h1>
		<p class="lead">${t.lead}</p>
		<p class="host">${new URL(site.domain).host}</p>
	</div>
	<img class="hero" src="hero.svg" alt="">
</main>
<footer></footer>
</body>
</html>
`;
}

function findBrowser() {
	const browser = BROWSERS.find((candidate) => candidate && existsSync(candidate));
	if (!browser) throw new Error('Chrome or Edge not found: set BROWSER to its path');
	return browser;
}

function render(browser, html, output, args) {
	rmSync(output, { force: true });
	execFileSync(browser, ['--headless', '--disable-gpu', '--hide-scrollbars', '--virtual-time-budget=10000', ...args, pathToFileURL(html).href], {
		stdio: 'ignore',
	});
	if (!existsSync(output) || statSync(output).size === 0) throw new Error(`${output} not written`);
	console.log(`✓ ${path.relative(PROJECT_ROOT, output)}`);
}

const browser = findBrowser();
const assetDir = path.join(PROJECT_ROOT, 'src/public/asset');
const workDir = mkdtempSync(path.join(os.tmpdir(), 'share-assets-'));
try {
	copyFileSync(path.join(PROJECT_ROOT, 'src/assets/asset/frost-alert-hero.svg'), path.join(workDir, 'hero.svg'));
	for (const lang of parseSiteArgs(process.argv.slice(2))) {
		const { default: site } = await import(pathToFileURL(path.join(PROJECT_ROOT, `src/_data/sites/${lang}.js`)));
		const t = TEXTS[lang];

		const flyerHtml = path.join(workDir, `flyer-${lang}.html`);
		writeFileSync(flyerHtml, flyerPage(site, t));
		const flyer = path.join(assetDir, t.flyer);
		render(browser, flyerHtml, flyer, ['--no-pdf-header-footer', `--print-to-pdf=${flyer}`]);

		const socialHtml = path.join(workDir, `share-${lang}.html`);
		writeFileSync(socialHtml, socialPage(site, t));
		const social = path.join(assetDir, `share-${lang}.png`);
		render(browser, socialHtml, social, ['--window-size=1200,630', '--force-device-scale-factor=1', `--screenshot=${social}`]);
	}
} finally {
	rmSync(workDir, { recursive: true, force: true });
}
