import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { test } from 'node:test';

const homes = {
	en: readFileSync(new URL('../src/content/en/index.njk', import.meta.url), 'utf8'),
	fr: readFileSync(new URL('../src/content/fr/index.njk', import.meta.url), 'utf8'),
};

test('home pages expose matching visible FAQ cards without JSON-LD', () => {
	for (const [site, source] of Object.entries(homes)) {
		assert.match(source, /<h2 id="faq-heading"/, `${site} FAQ heading`);
		assert.equal((source.match(/<article class="col-lg-6 mb-3">/g) ?? []).length, 5, `${site} FAQ cards`);
		assert.doesNotMatch(source, /application\/ld\+json|FAQPage/, `${site} FAQ JSON-LD`);
	}
});
