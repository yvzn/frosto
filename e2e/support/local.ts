// Shared setup of the local tests: URLs of the Aspire stack (fixed ports, see ../apphost/apphost.mts),
// test parameters (site language, hemisphere) and the texts the tests look for in each language.

import { test as base } from '@playwright/test';

export const urls = {
	api: 'http://localhost:7071',
	batch: 'http://localhost:7072',
	support: 'http://localhost:7073',
	admin: 'http://localhost:5176',
	siteFr: 'http://localhost:5180/',
	siteEn: 'http://localhost:5181/',
	mocks: 'http://localhost:7090',
};

export type Lang = 'fr' | 'en';
export type Hemisphere = 'N' | 'S';

export type LocalOptions = {
	/** Site the user signs up on, and so the language of every page and mail. Set per project (local-fr, local-en). */
	lang: Lang;
	/** Where the test location is. batch only alerts in the winter months of the location's hemisphere. */
	hemisphere: Hemisphere;
};

// Keep in sync with batch/Services/AppSettings.cs (NorthernWinterMonths, SouthernWinterMonths).
const winterMonths: Record<Hemisphere, number[]> = {
	N: [11, 12, 1, 2, 3, 4, 5],
	S: [5, 6, 7, 8, 9, 10, 11],
};

export const isWinter = (hemisphere: Hemisphere, date = new Date()) =>
	winterMonths[hemisphere].includes(date.getUTCMonth() + 1);

/**
 * E2E_HEMISPHERE=N|S|auto (default auto: the hemisphere where it is winter now, so that batch sends an alert).
 * An explicit value outside its winter months makes the test fail early: no alert would be sent.
 */
function hemisphereFromEnvironment(): Hemisphere {
	const value = (process.env.E2E_HEMISPHERE ?? 'auto').trim().toUpperCase();
	if (value === 'N' || value === 'S') return value;
	if (value === 'AUTO') return isWinter('N') ? 'N' : 'S';
	throw new Error(`E2E_HEMISPHERE must be N, S or auto, not "${process.env.E2E_HEMISPHERE}"`);
}

/** A real place in each hemisphere. The weather mock answers frost for any coordinates. */
export const places: Record<Hemisphere, { country: string; zipCode: string; coordinates: string; timezone: string; offset: string }> = {
	N: { country: 'France', zipCode: '44000', coordinates: '47.218371, -1.553621', timezone: 'Europe/Paris', offset: '+01:00' },
	S: { country: 'Australia', zipCode: '7000', coordinates: '-42.882138, 147.327195', timezone: 'Australia/Hobart', offset: '+11:00' },
};

export const texts = {
	fr: {
		siteUrl: urls.siteFr,
		signUp: {
			email: 'Adresse e-mail',
			city: 'Ville',
			zipCode: 'Code postal',
			country: 'Pays',
			submit: "S'inscrire",
			done: 'Inscription prise en compte',
		},
		consent: "J'accepte que mes coordonnées personnelles soient utilisées dans le cadre de ce service.",
		checkSubscription: { email: 'Adresse e-mail', submit: 'Vérifier', done: 'Vérification soumise' },
		unsubscribe: { email: 'Adresse e-mail', submit: 'Se désinscrire', done: 'Demande de désinscription soumise' },
		mail: {
			subscriptionSubject: 'Votre abonnement à AlerteGelee.fr',
			unsubscriptionSubject: 'Votre désinscription de AlerteGelee.fr',
			alertFrom: 'AlerteGelee.fr',
		},
	},
	en: {
		siteUrl: urls.siteEn,
		signUp: {
			email: 'Email address',
			city: 'City',
			zipCode: 'Postal code',
			country: 'Country',
			submit: 'Sign up',
			done: 'Sign-up completed',
		},
		consent: 'I agree that my personal contact details are used as part of this service.',
		checkSubscription: { email: 'Email address', submit: 'Check Subscription', done: 'Subscription check submitted' },
		unsubscribe: { email: 'Email address', submit: 'Unsubscribe', done: 'Unsubscribe request submitted' },
		mail: {
			subscriptionSubject: 'Your Subscription Status to FrostAlert.net',
			unsubscriptionSubject: 'Your Unsubscription from FrostAlert.net',
			alertFrom: 'FrostAlert.net',
		},
	},
} as const;

/** Random lowercase letters: city names must not contain digits (admin validation). */
export function randomLetters(length: number): string {
	let result = '';
	for (let i = 0; i < length; i++) result += String.fromCharCode(97 + Math.floor(Math.random() * 26));
	return result;
}

export const test = base.extend<LocalOptions>({
	lang: ['fr', { option: true }],
	hemisphere: [hemisphereFromEnvironment(), { option: true }],
});

export { expect } from '@playwright/test';
