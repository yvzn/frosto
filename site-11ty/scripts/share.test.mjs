import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { test } from 'node:test';

const read = (path) => readFileSync(new URL(`../${path}`, import.meta.url), 'utf8');

test('bilingual share pages contain plain fallbacks and localized flyers', () => {
	for (const lang of ['fr', 'en']) {
		const page = read(`src/content/${lang}/share.njk`);
		assert.match(page, /mailto:\?/);
		assert.match(page, /data-copy-share/);
		assert.match(page, /WhatsApp/);
		assert.match(page, /recommend-(fr|en)\.pdf/);
		assert.match(read(`src/content/${lang}/share.njk`), /ogDescription:/);
	}
});

test('base layout and sitemap include share metadata and index entry', () => {
	const layout = read('src/_includes/layouts/base.njk');
	const sitemap = read('src/_data/sitemap.js');
	assert.match(layout, /property="og:title"/);
	assert.match(layout, /property="og:image"/);
	assert.match(layout, /name="twitter:card"/);
	assert.match(sitemap, /\/share\.html/);
});

test('footer and signup-complete entry points include recommendations', () => {
	for (const lang of ['fr', 'en']) {
		assert.match(read(`src/_includes/partials/${lang}/footer.njk`), /share\.html/);
		const signup = read(`src/content/${lang}/sign-up-complete.njk`);
		assert.match(signup, /data-native-share/);
		assert.match(signup, /data-copy-share/);
	}
});

test('alert formatters include the recommend link in HTML and text', () => {
	const french = readFileSync(new URL('../../batch/Services/FrenchFormatter.cs', import.meta.url), 'utf8');
	const english = readFileSync(new URL('../../batch/Services/EnglishFormatter.cs', import.meta.url), 'utf8');
	assert.match(french, /Recommander le service/);
	assert.match(french, /share\.html/);
	assert.match(english, /Recommend the service/);
	assert.match(english, /share\.html/);
});
