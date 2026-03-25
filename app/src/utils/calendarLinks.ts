/**
 * Utility functions for generating "Add to Calendar" links for various providers.
 */

export interface CalendarEvent {
	title: string;
	description: string;
	date: string; // ISO date string (YYYY-MM-DD)
	startHour?: number; // Default: 8
	durationHours?: number; // Default: 1
}

function toLocalDateTimeStrings(
	dateStr: string,
	startHour: number,
	durationHours: number,
): { start: string; end: string } {
	const start = new Date(`${dateStr}T00:00:00`);
	start.setHours(startHour, 0, 0, 0);

	const end = new Date(start);
	end.setHours(start.getHours() + durationHours);

	const fmt = (d: Date) =>
		d.getFullYear().toString() +
		String(d.getMonth() + 1).padStart(2, '0') +
		String(d.getDate()).padStart(2, '0') +
		'T' +
		String(d.getHours()).padStart(2, '0') +
		String(d.getMinutes()).padStart(2, '0') +
		String(d.getSeconds()).padStart(2, '0');

	return { start: fmt(start), end: fmt(end) };
}

export function buildGoogleCalendarUrl(event: CalendarEvent): string {
	const hour = event.startHour ?? 8;
	const duration = event.durationHours ?? 1;
	const { start, end } = toLocalDateTimeStrings(event.date, hour, duration);

	const params = new URLSearchParams({
		action: 'TEMPLATE',
		text: event.title,
		dates: `${start}/${end}`,
		details: event.description,
	});

	return `https://calendar.google.com/calendar/render?${params.toString()}`;
}

export function buildOutlookCalendarUrl(event: CalendarEvent): string {
	const hour = event.startHour ?? 8;
	const duration = event.durationHours ?? 1;

	const start = new Date(`${event.date}T00:00:00`);
	start.setHours(hour, 0, 0, 0);

	const end = new Date(start);
	end.setHours(start.getHours() + duration);

	const fmt = (d: Date) => d.toISOString().replace(/\.\d{3}Z$/, '');

	const params = new URLSearchParams({
		path: '/calendar/action/compose',
		rru: 'addevent',
		subject: event.title,
		startdt: fmt(start),
		enddt: fmt(end),
		body: event.description,
	});

	return `https://outlook.live.com/calendar/0/action/compose?${params.toString()}`;
}

function escapeIcsText(text: string): string {
	return text.replace(/\\/g, '\\\\').replace(/;/g, '\\;').replace(/,/g, '\\,').replace(/\n/g, '\\n');
}

export function buildIcsFileContent(event: CalendarEvent): string {
	const hour = event.startHour ?? 8;
	const duration = event.durationHours ?? 1;
	const { start, end } = toLocalDateTimeStrings(event.date, hour, duration);

	const lines = [
		'BEGIN:VCALENDAR',
		'VERSION:2.0',
		'PRODID:-//FrostAlert//FrostAlert//EN',
		'BEGIN:VEVENT',
		`DTSTART:${start}`,
		`DTEND:${end}`,
		`SUMMARY:${escapeIcsText(event.title)}`,
		`DESCRIPTION:${escapeIcsText(event.description)}`,
		'END:VEVENT',
		'END:VCALENDAR',
	];

	return lines.join('\r\n');
}

export function downloadIcsFile(event: CalendarEvent): void {
	const content = buildIcsFileContent(event);
	const blob = new Blob([content], { type: 'text/calendar;charset=utf-8' });
	const url = URL.createObjectURL(blob);

	const a = document.createElement('a');
	a.href = url;
	a.download = 'frost-alert.ics';
	document.body.appendChild(a);
	a.click();
	document.body.removeChild(a);
	URL.revokeObjectURL(url);
}
