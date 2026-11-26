// Eleventy renders the pages of ONE site (selected with SITE=fr|en) into .11ty/<site>/.
// Vite then builds .11ty/<site>/ into dist/<site>/ (see vite.config.js and scripts/build.mjs).
//
// .11ty/<site>/ is laid out as a Vite project:
//   *.html            pages (from src/content/<site>/)
//   main.js, main.css shared assets (from src/assets/)
//   <page>.js         page scripts (from src/scripts/<site>/, or src/assets/ when identical on both sites)
//   asset/            images (from src/assets/asset/)
//   public/           files Vite copies untouched to the output root (from src/public/):
//                     .htaccess (shared), robots.txt and sitemap.xml (rendered from site settings)

import path from 'node:path';
import { SITES, currentSite, eleventyOutDir } from './scripts/lib/sites.mjs';

const site = currentSite();

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
	eleventyConfig.addPassthroughCopy({ [`src/scripts/${site}`]: '.' });
	eleventyConfig.addPassthroughCopy({ 'src/public/.htaccess': 'public/.htaccess' });
	eleventyConfig.addPassthroughCopy({ 'src/public/asset': 'public/asset' });

	// Permalinks: see src/content/content.11tydata.js.

	// A typo in a variable name fails the build instead of silently rendering an empty string.
	eleventyConfig.setNunjucksEnvironmentOptions({ throwOnUndefined: true });
}
