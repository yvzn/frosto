// Eleventy renders the pages of ONE site (selected with SITE=fr|en) into .11ty/<site>/.
// Vite then builds .11ty/<site>/ into dist/<site>/ (see vite.config.js and scripts/build.mjs).
//
// .11ty/<site>/ mirrors the layout of the legacy Vite projects:
//   *.html            pages (from src/content/<site>/)
//   main.js, main.css shared assets (from src/assets/)
//   <page>.js         page scripts (from src/scripts/<site>/, or src/assets/ when identical on both sites)
//   asset/            images (from src/assets/asset/)
//   public/           files Vite copies untouched to the output root (from src/public/):
//                     .htaccess (shared), robots.txt and sitemap.xml (rendered from site settings)

import { existsSync, readdirSync } from 'node:fs';
import path from 'node:path';
import { REPO_ROOT, SITES, currentSite, eleventyOutDir } from './scripts/lib/sites.mjs';

const site = currentSite();

/**
 * MIGRATION FALLBACK (temporary, removed once every page is migrated):
 * any page or page script not yet provided by site-11ty is copied as-is
 * from the legacy folder (site/ or site-en/), so the full site always builds and the
 * parity check always covers all pages.
 */
function legacyFallbacks() {
	const legacyDir = path.join(REPO_ROOT, SITES[site].legacyDir);
	const contentDir = path.join('src', 'content', site);
	const templated = new Set(existsSync(contentDir) ? readdirSync(contentDir).map((f) => path.parse(f).name) : []);
	const scriptsDir = path.join('src', 'scripts', site);
	const shared = new Set([
		...readdirSync(path.join('src', 'assets')),
		...(existsSync(scriptsDir) ? readdirSync(scriptsDir) : []),
	]);

	const copies = {};
	for (const file of readdirSync(legacyDir)) {
		const { name, ext } = path.parse(file);
		if (ext === '.html' && !templated.has(name)) copies[path.join(legacyDir, file)] = file;
		if (ext === '.js' && file !== 'vite.config.js' && !shared.has(file)) copies[path.join(legacyDir, file)] = file;
	}
	return copies;
}

export default function (eleventyConfig) {
	eleventyConfig.setInputDirectory('src');
	eleventyConfig.setOutputDirectory(path.relative('.', eleventyOutDir(site)));

	// Only this site's pages are templates; the other site's folder, the assets and the scripts are not.
	for (const other of Object.keys(SITES).filter((s) => s !== site)) {
		eleventyConfig.ignores.add(`src/content/${other}/**`);
	}
	eleventyConfig.ignores.add('src/assets/**');
	eleventyConfig.ignores.add('src/scripts/**');

	// Shared assets and this site's page scripts, next to the pages (pages load them as ./<name>.js).
	eleventyConfig.addPassthroughCopy({ 'src/assets': '.' });
	eleventyConfig.addPassthroughCopy({ 'src/public/.htaccess': 'public/.htaccess' });
	if (existsSync(path.join('src', 'scripts', site))) {
		eleventyConfig.addPassthroughCopy({ [`src/scripts/${site}`]: '.' });
	}

	const fallbacks = legacyFallbacks();
	if (Object.keys(fallbacks).length) {
		eleventyConfig.addPassthroughCopy(fallbacks);
		console.log(`[11ty:${site}] from legacy ${SITES[site].legacyDir}/: ${Object.values(fallbacks).sort().join(', ')}`);
	}

	// Permalinks: see src/content/content.11tydata.js.

	// A typo in a variable name fails the build instead of silently rendering an empty string.
	eleventyConfig.setNunjucksEnvironmentOptions({ throwOnUndefined: true });
}
