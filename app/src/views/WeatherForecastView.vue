<script setup lang="ts">
import { Head } from '@unhead/vue/components';
import { ref, onMounted, computed } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRoute, useRouter } from 'vue-router';

import AddToCalendarButton from '@/components/AddToCalendarButton.vue';
import TemperatureCard from '@/components/TemperatureCard.vue';
import type { CalendarEvent } from '@/utils/calendarLinks';

interface ForecastEntry {
	date: string;
	minimum: number;
	maximum: number;
}

interface WeatherForecastResponse {
	location: {
		city: string;
		country: string;
		temperatureThreshold: number;
	};
	forecasts: ForecastEntry[];
}

const route = useRoute();
const router = useRouter();
const { t, d, locale } = useI18n({
	messages: {
		en: {
			weatherForecast: {
				title: 'Weather Forecast',
				loading: 'Loading forecast…',
				error: 'An error occurred while loading the forecast.',
				retry: 'Retry',
				refresh: 'Refresh',
				thresholdLabel: 'Alert threshold (°C)',
				tableDate: 'Date',
				tableMinTemp: 'Min (°C)',
				tableMaxTemp: 'Max (°C)',
				tableFrost: 'Frost expected',
				calendarEventTitle: 'Frost alert — {city}, {country}',
				calendarEventBody:
					'Frost expected — Temperature forecast for {city}, {country}: Min {min}°C, Max {max}°C.',
			},
			footer: {
				credits: 'Credits',
				weatherData: 'Weather data by Open-Meteo.com',
				illustrations: 'Illustrations by',
				links: 'Links',
				contact: 'Contact us',
				sourceCode: 'Open source project',
				sourceCodeSuffix: 'maintained by volunteers.',
				donate: 'Support the service with an optional donation',
			},
		},
		fr: {
			weatherForecast: {
				title: 'Prévisions météo',
				loading: 'Chargement des prévisions…',
				error: 'Une erreur est survenue lors du chargement des prévisions.',
				retry: 'Réessayer',
				refresh: 'Actualiser',
				thresholdLabel: "Seuil d'alerte (°C)",
				tableDate: 'Date',
				tableMinTemp: 'Min (°C)',
				tableMaxTemp: 'Max (°C)',
				tableFrost: 'Gelée prévue',
				calendarEventTitle: 'Alerte gel — {city}, {country}',
				calendarEventBody:
					'Gelée prévue — Prévisions de température pour {city}, {country} : Min {min}°C, Max {max}°C.',
			},
			footer: {
				credits: 'Crédits',
				weatherData: 'Weather data by Open-Meteo.com',
				illustrations: 'Illustrations par',
				links: 'Liens',
				contact: 'Nous contacter',
				sourceCode: 'Projet open source',
				sourceCodeSuffix: 'maintenu par des bénévoles.',
				donate: 'Soutenir le projet avec un don optionnel',
			},
		},
	},
	datetimeFormats: {
		en: {
			short: {
				weekday: 'long',
				month: 'long',
				day: 'numeric',
			},
		},
		fr: {
			short: {
				weekday: 'long',
				month: 'long',
				day: 'numeric',
			},
		},
	},
});

const loading = ref(false);
const error = ref(false);
const data = ref<WeatherForecastResponse | null>(null);
const threshold = ref(0);

const THRESHOLD_MIN = -20;
const THRESHOLD_MAX = 20;
const THRESHOLD_STEP = 0.5;

async function fetchForecast() {
	loading.value = true;
	error.value = false;
	data.value = null;

	const partitionKey = route.params.partitionKey as string;
	const rowKey = route.params.rowKey as string;
	const weatherForecastUrl = import.meta.env.VITE_WEATHERFORECAST_URL as string;
	const url = `${weatherForecastUrl}?p=${encodeURIComponent(partitionKey)}&r=${encodeURIComponent(rowKey)}`;

	try {
		const response = await fetch(url);

		if (response.status === 400 || response.status === 401 || response.status === 403) {
			await router.push({ name: 'check-subscription' });
			return;
		}

		if (!response.ok) {
			error.value = true;
			return;
		}

		const json = (await response.json()) as WeatherForecastResponse;
		data.value = json;
		threshold.value = json.location.temperatureThreshold;
	} catch {
		error.value = true;
	} finally {
		loading.value = false;
	}
}

function isTemperatureDropping(currentValue: number, previousValue?: number): boolean {
	return previousValue !== undefined && currentValue < previousValue;
}

const contactUrl = computed(() => {
	switch (locale.value) {
		case 'fr':
			return import.meta.env.VITE_SITE_FR_URL + '/contact.html';
		default:
			return import.meta.env.VITE_SITE_EN_URL + '/contact.html';
	}
});

const donateUrl = computed(() => {
	switch (locale.value) {
		case 'fr':
			return import.meta.env.VITE_SITE_FR_URL + '/donate.html';
		default:
			return import.meta.env.VITE_SITE_EN_URL + '/donate.html';
	}
});

function buildCalendarEvent(forecast: ForecastEntry): CalendarEvent {
	const city = data.value?.location.city ?? '';
	const country = data.value?.location.country ?? '';

	return {
		title: t('weatherForecast.calendarEventTitle', { city, country }),
		description: t('weatherForecast.calendarEventBody', {
			city,
			country,
			min: forecast.minimum,
			max: forecast.maximum,
		}),
		date: forecast.date,
	};
}

onMounted(fetchForecast);
</script>

<template>
	<Head>
		<title>{{ t('weatherForecast.title') }} &ndash; {{ t('app.title') }}</title>
		<meta name="description" :content="t('app.description')" />
	</Head>
	<div class="container py-5">
		<h1 class="fw-light mb-4">{{ t('weatherForecast.title') }}</h1>

		<div v-if="loading" class="d-grid gap-3 mb-4">
			<h2 class="h4 mb-3 placeholder-glow">{{ t('weatherForecast.loading') }}</h2>

			<article class="card border shadow-sm rounded-4" v-for="i in 3" :key="i">
				<div class="card-body">
					<div class="card-title placeholder-glow">
						<h3 class="h5 placeholder bg-secondary w-25"></h3>
					</div>
					<div class="card-text row row-cols-1 row-cols-sm-2 g-3">
						<div class="col">
							<div class="p-3 rounded-4 bg-body-tertiary bg-opacity-50">
								<div class="placeholder-glow">
									<span class="placeholder bg-secondary w-50"></span>
								</div>
								<div class="placeholder-glow">
									<span class="placeholder bg-secondary w-25"></span>
								</div>
							</div>
						</div>
						<div class="col">
							<div class="p-3 rounded-4 bg-body-tertiary bg-opacity-50">
								<div class="placeholder-glow">
									<span class="placeholder bg-secondary w-50"></span>
								</div>
								<div class="placeholder-glow">
									<span class="placeholder bg-secondary w-25"></span>
								</div>
							</div>
						</div>
					</div>
				</div>
			</article>
		</div>

		<div v-else-if="error" class="my-3">
			<p class="text-danger">{{ t('weatherForecast.error') }}</p>
			<button class="btn btn-primary" @click="fetchForecast">
				{{ t('weatherForecast.retry') }}
			</button>
		</div>

		<template v-else-if="data">
			<div class="d-flex mb-3">
				<h2 class="h4 flex-grow-1">{{ data.location.city }}, {{ data.location.country }}</h2>
				<div class="">
					<button class="btn btn-outline-primary" @click="fetchForecast">
						{{ t('weatherForecast.refresh') }}
					</button>
				</div>
			</div>

			<div class="d-grid gap-3 mb-4">
				<article
					v-for="(forecast, index) in data.forecasts"
					:key="forecast.date"
					class="card border shadow-sm rounded-4"
				>
					<div class="card-body">
						<div class="d-grid d-sm-flex align-items-start justify-content-between gap-3 mb-3">
							<h3 class="h5 mb-0 fw-semibold text-body-emphasis">
								{{ d(forecast.date, 'short') }}
							</h3>
							<span
								v-if="forecast.minimum < threshold"
								class="badge text-bg-info align-self-start fw-medium"
								:title="t('weatherForecast.tableFrost')"
							>
								❄️ {{ t('weatherForecast.tableFrost') }}
							</span>
						</div>

						<div class="row row-cols-1 row-cols-sm-2 g-3">
							<div class="col">
								<TemperatureCard
									:label="t('weatherForecast.tableMinTemp')"
									:value="forecast.minimum"
									:isDropping="
										isTemperatureDropping(forecast.minimum, data.forecasts[index - 1]?.minimum)
									"
									:isBelowThreshold="forecast.minimum < threshold"
								/>
							</div>
							<div class="col">
								<TemperatureCard
									:label="t('weatherForecast.tableMaxTemp')"
									:value="forecast.maximum"
									:isDropping="
										isTemperatureDropping(forecast.maximum, data.forecasts[index - 1]?.maximum)
									"
									:isBelowThreshold="forecast.maximum < threshold"
								/>
							</div>
						</div>

						<div v-if="forecast.minimum < threshold" class="mt-3">
							<AddToCalendarButton :event="buildCalendarEvent(forecast)" />
						</div>
					</div>
				</article>
			</div>

			<div class="mb-4">
				<label for="threshold-slider" class="form-label">
					{{ t('weatherForecast.thresholdLabel') }}: {{ threshold }}&deg;
				</label>
				<input
					id="threshold-slider"
					v-model.number="threshold"
					type="range"
					class="form-range"
					:min="THRESHOLD_MIN"
					:max="THRESHOLD_MAX"
					:step="THRESHOLD_STEP"
				/>
			</div>
		</template>
	</div>
	<footer class="text-muted py-5 bg-light">
		<div class="container">
			<div class="row">
				<div class="col-lg-6 py-3">
					<h2 class="h4">{{ t('footer.credits') }}</h2>
					<p>
						<a
							class="link-dark"
							href="https://open-meteo.com/"
							target="_blank"
							rel="noopener noreferrer"
							>{{ t('footer.weatherData') }}</a
						>
					</p>
					<p>
						{{ t('footer.illustrations') }}
						<a class="link-dark" href="https://undraw.co/" target="_blank" rel="noopener">unDraw</a
						>.
					</p>
				</div>
				<div class="col-lg-6 py-3">
					<h2 class="h4">{{ t('footer.links') }}</h2>
					<p>
						<a class="link-dark" :href="contactUrl">{{ t('footer.contact') }}</a>
					</p>
					<p>
						<a
							class="link-dark"
							href="https://github.com/yvzn/frosto/"
							target="_blank"
							rel="noopener"
							>{{ t('footer.sourceCode') }}</a
						>
						{{ t('footer.sourceCodeSuffix') }}
					</p>
					<p>
						<a class="link-dark" :href="donateUrl">{{ t('footer.donate') }}</a>
					</p>
				</div>
			</div>
		</div>
	</footer>
</template>
