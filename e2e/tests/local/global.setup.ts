// Waits until every resource of the local Aspire stack answers, so the happy path does not fail on a cold start.
// The stack itself is started outside the tests: `MOCKS=all aspire run` in ../apphost.

import { test as setup, expect } from '@playwright/test';
import { urls } from '../../support/local';

const healthChecks = {
	api: `${urls.api}/api/healthz`,
	support: `${urls.support}/api/healthz`,
	batch: `${urls.batch}/api/healthz`,
	admin: `${urls.admin}/`,
	'site fr': urls.siteFr,
	'site en': urls.siteEn,
	mocks: `${urls.mocks}/healthz`,
};

setup.setTimeout(5 * 60_000);

setup('local stack is up (MOCKS=all aspire run)', async ({ request }) => {
	for (const [name, url] of Object.entries(healthChecks)) {
		await expect
			.poll(
				async () => {
					try {
						return (await request.get(url, { timeout: 10_000 })).status();
					} catch {
						return 0; // not listening yet
					}
				},
				{ message: `${name} (${url}) is not up. Start the stack: cd apphost; MOCKS=all aspire run`, timeout: 4 * 60_000, intervals: [2_000] },
			)
			.toBe(200);
	}

	// the mock inbox JSON API is what the happy path reads: fails here if mocks are off or outdated
	expect((await request.get(`${urls.mocks}/api/mails`)).ok(), 'mock inbox API /api/mails').toBe(true);
});
