import assert from 'node:assert/strict';
import { test } from 'node:test';
import { plainEmails } from './lib/email-guard.mjs';

test('accepts addresses split in parts, as built today', () => {
	assert.deepEqual(plainEmails('innerHTML=[`yvan`,`%40`,`alertegelee.`,`fr`].map(String).join(``)'), []);
	assert.deepEqual(plainEmails('innerHTML=[`yvan`,`&#`,`64;`,`alertegelees.`,`fr`].map(String).join(``)'), []);
});

test('rejects addresses folded into a plain string, in any encoding', () => {
	for (const text of [
		'href="mailto:yvan%40alertegelee.fr"',
		'innerHTML=`yvan&#64;alertegelees.fr`',
		'innerHTML=`yvan&#x40;frostalert.net`',
		'<a href="mailto:contact@frostalert.net">',
	]) {
		assert.equal(plainEmails(text).length, 1, text);
	}
});
