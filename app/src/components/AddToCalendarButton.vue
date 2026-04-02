<script setup lang="ts">
import { useI18n } from 'vue-i18n';

import {
	buildGoogleCalendarUrl,
	buildOutlookCalendarUrl,
	downloadIcsFile,
	type CalendarEvent,
} from '@/utils/calendarLinks';

const { t } = useI18n({
	messages: {
		en: {
			addToCalendar: 'Add to Calendar',
			googleCalendar: 'Google Calendar',
			appleCalendar: 'Apple Calendar',
			outlook: 'Outlook',
			icsFile: 'Other (ICS file)',
		},
		fr: {
			addToCalendar: 'Ajouter au calendrier',
			googleCalendar: 'Google Calendar',
			appleCalendar: 'Apple Calendar',
			outlook: 'Outlook',
			icsFile: 'Autre (fichier ICS)',
		},
	},
});

const props = defineProps<{
	event: CalendarEvent;
}>();

function openLink(url: string) {
	window.open(url, '_blank', 'noopener,noreferrer');
}

function onGoogle() {
	openLink(buildGoogleCalendarUrl(props.event));
}

function onApple() {
	downloadIcsFile(props.event);
}

function onOutlook() {
	openLink(buildOutlookCalendarUrl(props.event));
}

function onIcs() {
	downloadIcsFile(props.event);
}
</script>

<template>
	<div class="dropdown d-inline-block">
		<button
			class="btn btn-sm btn-outline-primary dropdown-toggle"
			type="button"
			data-bs-toggle="dropdown"
			aria-expanded="false"
			:aria-label="t('addToCalendar')"
		>
			📅 {{ t('addToCalendar') }}
		</button>
		<ul class="dropdown-menu">
			<li>
				<button class="dropdown-item" type="button" @click="onGoogle">
					{{ t('googleCalendar') }}
				</button>
			</li>
			<li>
				<button class="dropdown-item" type="button" @click="onApple">
					{{ t('appleCalendar') }}
				</button>
			</li>
			<li>
				<button class="dropdown-item" type="button" @click="onOutlook">
					{{ t('outlook') }}
				</button>
			</li>
			<li>
				<hr class="dropdown-divider" />
			</li>
			<li>
				<button class="dropdown-item" type="button" @click="onIcs">
					{{ t('icsFile') }}
				</button>
			</li>
		</ul>
	</div>
</template>
