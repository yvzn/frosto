// Client of the mock mail inbox (../apphost/mocks/server.mjs, GET /api/mails).

import type { APIRequestContext } from '@playwright/test';
import { urls } from './local';

export type Mail = {
	id: number;
	/** How the mail reached the mock: smtp (admin), scaleway (batch default channel), send-mail. */
	via: 'smtp' | 'scaleway' | 'send-mail';
	at: string;
	to: string;
	subject: string;
	html?: string;
	text?: string;
	/** Header block: received as is (smtp) or rebuilt from the API request (scaleway, send-mail). */
	headers?: string;
};

export async function mailsTo(request: APIRequestContext, address: string): Promise<Mail[]> {
	const response = await request.get(`${urls.mocks}/api/mails`, { params: { to: address } });
	if (!response.ok()) throw new Error(`Mock inbox answered HTTP ${response.status()}`);
	return response.json();
}

/** Value of a header in a header block (first match, folded lines joined). */
export function header(mail: Mail, name: string): string | undefined {
	const unfolded = (mail.headers ?? '').replace(/\r?\n[ \t]+/g, ' ');
	const line = unfolded.split(/\r?\n/).find((l) => l.toLowerCase().startsWith(`${name.toLowerCase()}:`));
	return line?.slice(name.length + 1).trim();
}

/** href of the first link of the HTML body whose URL contains `fragment`, HTML entities decoded. */
export function link(mail: Mail, fragment: string): string | undefined {
	for (const [, href] of (mail.html ?? '').matchAll(/href\s*=\s*["']([^"']+)["']/gi)) {
		const decoded = href.replace(/&amp;/g, '&').replace(/&#x3D;|&#61;/gi, '=');
		if (decoded.includes(fragment)) return decoded;
	}
	return undefined;
}
