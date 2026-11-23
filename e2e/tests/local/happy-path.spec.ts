// Full happy path on the local stack with mocks (MOCKS=all aspire run): one user goes from sign-up
// to unsubscription, through admin, support and batch. One test, one step per stage: each stage needs
// the state the previous one left. Every run uses a new e-mail and city, so runs do not see each other's data.
//
// Parameters: project local-fr / local-en (site language), E2E_HEMISPHERE=N|S|auto (see support/local.ts).

import type { APIRequestContext, Page } from '@playwright/test';
import { batchSlotOf } from '../../support/azurite';
import { header, link, mailsTo, type Mail } from '../../support/inbox';
import { expect, isWinter, places, randomLetters, test, texts, urls } from '../../support/local';

// batch schedules the notification in the background (BatchCronDebug answers first): mails arrive a bit later
const mailTimeout = { timeout: 60_000, intervals: [1_000, 2_000] };

test('happy path: sign-up, alert, unsubscribe', async ({ page, request, lang, hemisphere }) => {
	test.info().annotations.push({ type: 'parameters', description: `lang=${lang} hemisphere=${hemisphere}` });
	if (!isWinter(hemisphere)) {
		throw new Error(`E2E_HEMISPHERE=${hemisphere} is out of its winter months: batch would send no alert. Use auto or the other hemisphere.`);
	}

	const t = texts[lang];
	const place = places[hemisphere];
	const suffix = randomLetters(8);
	const email = `e2e.${suffix}@example.com`;
	const city = `Testville ${suffix}`;

	const mails = () => mailsTo(request, email);
	const withSubject = (subject: string) => async () => (await mails()).filter((m) => m.subject === subject);
	const alerts = async () => (await mails()).filter((m) => m.via === 'scaleway');

	// "PartitionKey|RowKey" of the valid location, read from admin once validated
	let locationId = '';

	await test.step('site: sign up', async () => {
		await page.goto(`${t.siteUrl}sign-up.html`);
		await page.getByRole('textbox', { name: t.signUp.email }).fill(email);
		await page.getByRole('textbox', { name: t.signUp.city }).fill(city);
		await page.getByRole('textbox', { name: t.signUp.zipCode }).fill(place.zipCode);
		await page.getByRole('textbox', { name: t.signUp.country }).fill(place.country);
		await page.getByRole('checkbox', { name: t.consent }).check();
		await page.getByRole('button', { name: t.signUp.submit }).click();
		await expect(page.getByRole('heading', { name: t.signUp.done })).toBeVisible();
	});

	await test.step('admin: validate the sign-up', async () => {
		await page.goto(`${urls.admin}/`);
		await page.getByRole('row').filter({ hasText: city }).getByRole('link', { name: 'Validate' }).click();
		await expect(page.locator('#valid-location-users')).toHaveValue(email);
		await expect(page.locator('#ValidLocation_lang')).toHaveValue(lang);

		// a new coordinate triggers a time zone lookup that fills time zone and offset: let it finish, then set ours
		const coordinates = page.locator('#valid-location-coordinates');
		await coordinates.fill(place.coordinates);
		const timezoneLookup = page.waitForResponse((r) => r.url().includes('/api/location/timezone'));
		await coordinates.press('Tab');
		await timezoneLookup;
		await page.locator('#valid-location-timezone').fill(place.timezone);
		await page.locator('#valid-location-offset').fill(place.offset);

		await page.getByRole('button', { name: 'Sign-up', exact: true }).click();
		await expect(page.getByRole('alert').filter({ hasText: `Updated ${city}` })).toBeVisible();
	});

	await test.step('admin: mark the location as UAT (a debug batch only notifies those)', async () => {
		await page.goto(`${urls.admin}/FindByEmail`);
		await page.getByRole('textbox', { name: 'Email address' }).fill(email);
		await page.getByRole('button', { name: 'Search' }).click();
		const edit = page.getByRole('row').filter({ hasText: city }).getByRole('link', { name: 'Edit' });
		const href = await edit.getAttribute('href');
		locationId = decodeURIComponent(href!.split('/').pop()!);
		expect(locationId).toMatch(/^[^|]+\|.+$/);

		await edit.click();
		await page.getByRole('switch', { name: 'UAT' }).check();
		await page.getByRole('button', { name: 'Update' }).click();
		await expect(page.getByRole('alert').filter({ hasText: `Updated ${city}` })).toBeVisible();
	});

	await test.step('site: check the subscription', async () => {
		await page.goto(`${t.siteUrl}check-subscription.html`);
		await page.getByRole('textbox', { name: t.checkSubscription.email }).fill(email);
		await page.getByRole('checkbox', { name: t.consent }).check();
		await page.getByRole('button', { name: t.checkSubscription.submit }).click();
		await expect(page.getByRole('heading', { name: t.checkSubscription.done })).toBeVisible();
	});

	await test.step('admin: answer the subscription check by mail', async () => {
		await page.goto(`${urls.admin}/CheckSubscription`);
		await page.getByRole('row').filter({ hasText: email }).getByRole('button', { name: 'Search subscription' }).click();
		await expect(page.getByText(`User ${email} is subscribed!`)).toBeVisible();
		await page.getByRole('button', { name: 'Send confirmation mail' }).click();
		await expect(page.locator('#send-mail-result')).toContainText('Confirmation mail sent successfully');
		await completeRequest(page);
		await expect(page.getByRole('row').filter({ hasText: email })).toHaveCount(0);
	});

	await test.step('mail: subscription confirmation', async () => {
		await expect.poll(async () => (await withSubject(t.mail.subscriptionSubject)()).length, mailTimeout).toBe(1);
		const [mail] = await withSubject(t.mail.subscriptionSubject)();
		expect(mail.html).toContain(city);
	});

	let slot = { slotIndex: -1, dayIndex: -1 };

	await test.step('admin: rebuild the batches', async () => {
		await page.goto(`${urls.admin}/Batch`);
		await page.getByRole('spinbutton', { name: 'Cycle length in days (N)' }).fill('1');
		await page.getByRole('checkbox', { name: 'Rebuild all batches' }).check();
		await page.getByRole('button', { name: 'Create Batches' }).click();
		await expect(page).toHaveURL(/\/BatchResult/);

		const [partitionKey, rowKey] = locationId.split('|');
		slot = await batchSlotOf(partitionKey, rowKey);
		expect(slot.dayIndex).toBe(0);
	});

	await test.step('batch: run the slot of the location', async () => {
		await runBatchSlot(request, slot);
	});

	let alert: Mail;

	await test.step('mail: frost alert', async () => {
		await expect.poll(async () => (await alerts()).length, mailTimeout).toBe(1);
		[alert] = await alerts();
		expect(header(alert, 'From')).toContain(t.mail.alertFrom);
		expect(alert.html).toContain(city);
		expect(header(alert, 'List-Unsubscribe-Post')).toBe('List-Unsubscribe=One-Click');
		expect(header(alert, 'List-Unsubscribe')).toMatch(/<mailto:[^>]+>, <http[^>]+\/api\/unsubscribe\?[^>]+>/);
	});

	await test.step('mail: unsubscribe with the link of the alert', async () => {
		const unsubscribeLink = link(alert, '/api/unsubscribe');
		expect(unsubscribeLink, 'unsubscribe link in the alert body').toBeDefined();
		await page.goto(unsubscribeLink!); // support redirects to the site confirmation page
		await expect(page).toHaveURL(new RegExp(`^${escapeRegExp(t.siteUrl)}unsubscribe\\.html`));
		await expect(page.getByRole('textbox', { name: t.unsubscribe.email })).toHaveValue(email);
		await page.getByRole('button', { name: t.unsubscribe.submit }).click();
		await expect(page.getByRole('heading', { name: t.unsubscribe.done })).toBeVisible();
	});

	await test.step('admin: unsubscribe the user and send the confirmation', async () => {
		await page.goto(`${urls.admin}/UnsubscribeRequest`);
		const requests = page.getByRole('row').filter({ hasText: email });
		await expect(requests).toHaveCount(1);
		await requests.getByRole('link', { name: 'Process' }).click();
		const processUrl = page.url();

		// "Proceed to Unsubscribe" opens a new tab: follow its link in this one
		const proceed = await page.getByRole('link', { name: 'Proceed to Unsubscribe' }).getAttribute('href');
		await page.goto(new URL(proceed!, urls.admin).toString());

		for (const choice of ['SelectedValidLocationId', 'SelectedLocationId', 'SelectedUserId']) {
			const options = page.locator(`input[type=radio][name=${choice}]`);
			if (await options.count()) await options.first().check();
		}
		await page.getByRole('button', { name: 'Unsubscribe', exact: true }).click();
		await expect(page.getByRole('heading', { name: 'Unsubscribe completed' })).toBeVisible();
		await page.getByRole('button', { name: 'Send confirmation mail' }).click();
		await expect(page.locator('#send-mail-result')).toContainText('Confirmation mail sent successfully');

		// the request is handled: mark it completed
		await page.goto(processUrl);
		await completeRequest(page);
		await expect(page.getByRole('heading', { name: 'Request Deleted Successfully' })).toBeVisible();
		await page.goto(`${urls.admin}/UnsubscribeRequest`);
		await expect(requests).toHaveCount(0);
	});

	await test.step('mail: unsubscription confirmation', async () => {
		await expect.poll(async () => (await withSubject(t.mail.unsubscriptionSubject)()).length, mailTimeout).toBe(1);
	});

	await test.step('batch: no more alert after unsubscription', async () => {
		await runBatchSlot(request, slot);
		// asserting that nothing arrives: give batch the time it took to send the first alert
		await page.waitForTimeout(10_000);
		expect(await alerts()).toHaveLength(1);
	});
});

async function runBatchSlot(request: APIRequestContext, slot: { slotIndex: number; dayIndex: number }) {
	// Debug build only (#if DEBUG in batch/LocationLoop2.cs); function keys are not checked by the local host
	const response = await request.get(`${urls.batch}/api/BatchCronDebug`, {
		params: { slotIndex: slot.slotIndex, dayIndex: slot.dayIndex },
		timeout: 60_000,
	});
	expect(response.status(), 'BatchCronDebug').toBe(200);
}

/** "Complete & discard" button of the danger zone of admin's request pages. */
async function completeRequest(page: Page) {
	await page.locator('details > summary').filter({ hasText: /completed/i }).click();
	await page.getByRole('button', { name: 'Complete & discard' }).click();
}

const escapeRegExp = (s: string) => s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
