import { defineConfig, devices } from '@playwright/test';
import type { LocalOptions } from './support/local';

/**
 * Two kinds of tests:
 * - prod: smoke tests against the deployed sites (weekly pipeline, `npm run test:prod`).
 * - local-fr / local-en: the full happy path against the local Aspire stack started with mocks
 *   (`MOCKS=all aspire run` in ../apphost), `npm run test:local`. See README.md.
 *
 * See https://playwright.dev/docs/test-configuration.
 */
export default defineConfig<LocalOptions>({
	testDir: './tests',
	forbidOnly: !!process.env.CI,
	// the local projects share one database, one mail inbox and one batch configuration: never run them in parallel
	workers: 1,
	reporter: [
		['list'],
		['junit', { outputFile: 'test-results/e2e-junit-results.xml' }],
	],
	use: {
		trace: 'retain-on-failure',
	},
	projects: [
		{
			name: 'prod',
			testDir: './tests/prod',
			fullyParallel: true,
			retries: process.env.CI ? 2 : 0,
			use: { ...devices['Desktop Chrome'] },
		},
		{
			name: 'local-setup',
			testDir: './tests/local',
			testMatch: /global\.setup\.ts/,
		},
		{
			name: 'local-fr',
			testDir: './tests/local',
			testMatch: /.*\.spec\.ts/,
			dependencies: ['local-setup'],
			timeout: 5 * 60_000,
			use: { ...devices['Desktop Chrome'], lang: 'fr' },
		},
		{
			name: 'local-en',
			testDir: './tests/local',
			testMatch: /.*\.spec\.ts/,
			dependencies: ['local-setup'],
			timeout: 5 * 60_000,
			use: { ...devices['Desktop Chrome'], lang: 'en' },
		},
	],
});
