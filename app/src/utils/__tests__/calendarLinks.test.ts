import { describe, it, expect } from 'vitest';
import {
	buildGoogleCalendarUrl,
	buildOutlookCalendarUrl,
	buildIcsFileContent,
	type CalendarEvent,
} from '../calendarLinks';

const sampleEvent: CalendarEvent = {
	title: 'Frost alert — Paris, France',
	description: 'Frost expected — Temperature forecast for Paris, France: Min -2°C, Max 3°C.',
	date: '2025-01-15',
};

describe('buildGoogleCalendarUrl', () => {
	it('returns a valid Google Calendar URL with correct parameters', () => {
		const url = buildGoogleCalendarUrl(sampleEvent);
		expect(url).toContain('https://calendar.google.com/calendar/render');
		expect(url).toContain('action=TEMPLATE');
		expect(url).toContain('text=Frost+alert');
		expect(url).toContain('details=Frost+expected');
		expect(url).toContain('20250115T080000');
		expect(url).toContain('20250115T090000');
	});

	it('uses custom start hour and duration', () => {
		const event: CalendarEvent = { ...sampleEvent, startHour: 10, durationHours: 2 };
		const url = buildGoogleCalendarUrl(event);
		expect(url).toContain('20250115T100000');
		expect(url).toContain('20250115T120000');
	});
});

describe('buildOutlookCalendarUrl', () => {
	it('returns a valid Outlook calendar URL with correct parameters', () => {
		const url = buildOutlookCalendarUrl(sampleEvent);
		expect(url).toContain('https://outlook.live.com/calendar/0/action/compose');
		expect(url).toContain('subject=Frost+alert');
		expect(url).toContain('body=Frost+expected');
	});
});

describe('buildIcsFileContent', () => {
	it('returns valid ICS content', () => {
		const ics = buildIcsFileContent(sampleEvent);
		expect(ics).toContain('BEGIN:VCALENDAR');
		expect(ics).toContain('BEGIN:VEVENT');
		expect(ics).toContain('SUMMARY:Frost alert');
		expect(ics).toContain('DESCRIPTION:Frost expected');
		expect(ics).toContain('DTSTART:20250115T080000');
		expect(ics).toContain('DTEND:20250115T090000');
		expect(ics).toContain('END:VEVENT');
		expect(ics).toContain('END:VCALENDAR');
	});

	it('escapes special ICS characters in title and description', () => {
		const event: CalendarEvent = {
			title: 'Title, with; special\\chars',
			description: 'Line1\nLine2',
			date: '2025-02-01',
		};
		const ics = buildIcsFileContent(event);
		expect(ics).toContain('SUMMARY:Title\\, with\\; special\\\\chars');
		expect(ics).toContain('DESCRIPTION:Line1\\nLine2');
	});
});
