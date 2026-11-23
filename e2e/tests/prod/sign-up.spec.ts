import { test, expect } from '@playwright/test';

test('sign-up page in French', async ({ page }) => {
	await page.goto('https://www.alertegelee.fr/sign-up.html');

	// check for presence of required fields
	await expect(page.getByRole('textbox', { name: 'Adresse e-mail' })).toBeVisible();
	await expect(page.getByRole('textbox', { name: 'Ville' })).toBeVisible();
	await expect(page.getByRole('textbox', { name: 'Code postal' })).toBeVisible();
	await expect(page.getByRole('textbox', { name: 'Pays' })).toBeVisible();

	// input sample data
	await page.getByRole('textbox', { name: 'Adresse e-mail' }).fill('test@example.com');
	await page.getByRole('textbox', { name: 'Ville' }).fill('End-to-End Test');
	await page.getByRole('textbox', { name: 'Code postal' }).fill('44000');
	await page.getByRole('textbox', { name: 'Pays' }).fill('France');

	// accept terms and conditions
	await page.getByRole('checkbox', { name: "J'accepte que mes coordonnées personnelles soient utilisées dans le cadre de ce service." }).check();

	// submit the form
	await page.getByRole('button', { name: "S'inscrire" }).click();

	// confirmation page
	await expect(page.getByRole('heading', { name: 'Inscription prise en compte' })).toBeVisible();
});

test('sign-up page in English', async ({ page }) => {
	await page.goto('https://www.frostalert.net/sign-up.html');

	// check for presence of required fields
	await expect(page.getByRole('textbox', { name: 'Email address' })).toBeVisible();
	await expect(page.getByRole('textbox', { name: 'City' })).toBeVisible();
	await expect(page.getByRole('textbox', { name: 'Postal code' })).toBeVisible();
	await expect(page.getByRole('textbox', { name: 'Country' })).toBeVisible();

	// input sample data
	await page.getByRole('textbox', { name: 'Email address' }).fill('test@example.com');
	await page.getByRole('textbox', { name: 'City' }).fill('End-to-End Test');
	await page.getByRole('textbox', { name: 'Postal code' }).fill('44000');
	await page.getByRole('textbox', { name: 'Country' }).fill('France');

	// accept terms and conditions
	await page.getByRole('checkbox', { name: "I agree that my personal contact details are used as part of this service." }).check();

	// submit the form
	await page.getByRole('button', { name: 'Sign up' }).click();

	// confirmation page
	await expect(page.getByRole('heading', { name: 'Sign-up completed' })).toBeVisible();
});
