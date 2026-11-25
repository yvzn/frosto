import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { test } from 'node:test';
import { PROJECT_ROOT } from './lib/sites.mjs';

const read = (path) => readFile(`${PROJECT_ROOT}/${path}`, 'utf8');

for (const lang of ['fr', 'en']) {
	test(`${lang} share page is indexable and has progressive share actions`, async () => {
		const page = await read(`src/content/${lang}/share.njk`);
		assert.doesNotMatch(page, /(?:^|\n)(?:canonical|noindex):/);
		assert.match(page, /mailto:\?subject=/);
		assert.match(page, /data-share-native/);
		assert.match(page, /data-share-copy/);
		assert.match(page, /WhatsApp/);
		assert.match(page, /Facebook/);
		assert.match(page, /Bluesky/);
		assert.match(page, /\.pdf" download/);
	});
}

test('share page is included in the bilingual sitemap', async () => {
	const sitemap = await read('src/_data/sitemap.js');
	assert.match(sitemap, /path: '\/share\.html'/);
});

test('base layout supplies both Open Graph and Twitter preview images', async () => {
	const layout = await read('src/_includes/layouts/base.njk');
	assert.match(layout, /property="og:image"/);
	assert.match(layout, /name="twitter:card" content="summary_large_image"/);
	assert.match(layout, /name="twitter:image"/);
});
