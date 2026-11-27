import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { test } from 'node:test';

const root = new URL('../src/', import.meta.url);
const read = (path) => readFile(new URL(path, root), 'utf8');

test.each = undefined;

for (const [lang, heading, providers, footerLabel] of [
	['en', 'Help &amp; feedback', ['Gmail', 'Outlook', 'Apple Mail'], 'Help &amp; feedback'],
	['fr', 'Aide et avis', ['Gmail', 'Outlook', 'Apple Mail', 'Orange', 'Free', 'SFR'], 'Aide et avis'],
]) {
	test(`${lang} help page has indexable guidance and the required entry points`, async () => {
		const page = await read(`content/${lang}/help.njk`);
		const footer = await read(`_includes/partials/${lang}/footer.njk`);

		assert.match(page, new RegExp(`<h1[^>]*>${heading}</h1>`));
		assert.match(page, /socialDescription:/);
		assert.doesNotMatch(page, /noindex:/);
		for (const provider of providers) {
			assert.match(page, new RegExp(`<summary[^>]*>${provider}</summary>`));
		}
		for (const detail of page.matchAll(/<details[\s\S]*?<\/details>/g)) {
			assert.ok((detail[0].match(/<li>/g) ?? []).length <= 3);
		}
		assert.match(page, /id="ademl"/);
		assert.match(page, /unsubscribe\.html/);
		assert.match(page, /feedback\.html/);
		assert.match(page, /share\.html/);
		assert.match(page, /donate\.html/);
		assert.match(footer, new RegExp(`href="./help.html">${footerLabel}</a>`));
		assert.doesNotMatch(footer, /(?:share|feedback|donate)\.html/);
	});
}

test('email footers link to the help page in HTML and plain text', async () => {
	const english = await readFile(new URL('../../batch/Services/EnglishFormatter.cs', import.meta.url), 'utf8');
	const french = await readFile(new URL('../../batch/Services/FrenchFormatter.cs', import.meta.url), 'utf8');

	assert.match(english, /help\.html[^]*Help &amp; feedback/);
	assert.match(english, /Help & feedback: \{4\}help\.html/);
	assert.match(french, /help\.html[^]*Aide et avis/);
	assert.match(french, /Aide et avis : \{4\}help\.html/);
});
