import assert from 'node:assert/strict';
import { test } from 'node:test';
import { endpointOrigins, parseHtaccessHeaders, productionHeaders, withConnectSrc, withViteClient } from './lib/headers.mjs';

const CSP = 'Content-Security-Policy';

test('reads every Header set line of .htaccess, except Cache-Control', () => {
	const headers = parseHtaccessHeaders([
		'Header set X-Frame-Options "deny"',
		'<FilesMatch "\\.html$">',
		'\tHeader set Content-Security-Policy "default-src \'self\'; connect-src \'self\'"',
		'\tHeader set Cache-Control "no-cache"',
		'</FilesMatch>',
	].join('\r\n'));
	assert.deepEqual(headers, {
		'X-Frame-Options': 'deny',
		[CSP]: "default-src 'self'; connect-src 'self'",
	});
});

test('the real .htaccess has a CSP with a connect-src directive', () => {
	assert.match(productionHeaders()[CSP], /(^|; )connect-src /);
});

test('keeps the origin of absolute endpoint URLs only', () => {
	assert.deepEqual(
		endpointOrigins({
			VITE_SIGNUP_URL: 'http://localhost:7071/api/signup',
			VITE_SUPPORT_URL: 'http://localhost:7071/api/support',
			VITE_HEALTHCHECK_URL: 'https://api.example.org/health',
			VITE_UNSUBSCRIBE_URL: '',
			VITE_CHECKSUBSCRIPTION_URL: '/relative',
			VITE_OTHER: 'https://not-an-endpoint.example.org',
		}),
		['http://localhost:7071', 'https://api.example.org'],
	);
});

test('adds missing origins to connect-src and leaves the other directives alone', () => {
	const headers = { [CSP]: "default-src 'self'; connect-src 'self' https://a.example.org; form-action *" };
	const { headers: result, added } = withConnectSrc(headers, ['https://a.example.org', 'http://localhost:7071']);
	assert.deepEqual(added, ['http://localhost:7071']);
	assert.equal(result[CSP], "default-src 'self'; connect-src 'self' https://a.example.org http://localhost:7071; form-action *");
	assert.equal(headers[CSP], "default-src 'self'; connect-src 'self' https://a.example.org; form-action *", 'input unchanged');
});

test('allows blob: workers for the Vite dev client, unless worker-src is already set', () => {
	assert.equal(withViteClient({ [CSP]: "default-src 'self'" })[CSP], "default-src 'self'; worker-src 'self' blob:");
	const explicit = { [CSP]: "default-src 'self'; worker-src 'none'" };
	assert.equal(withViteClient(explicit), explicit);
});

test('changes nothing when every origin is already allowed', () => {
	const headers = { [CSP]: "connect-src 'self' https://a.example.org" };
	assert.deepEqual(withConnectSrc(headers, ['https://a.example.org']), { headers, added: [] });
});
