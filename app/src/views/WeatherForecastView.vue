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
		temperatureUnit?: string;
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
				thresholdLabel: 'Alert threshold',
				tableDate: 'Date',
				tableMinTemp: 'Min',
				tableMaxTemp: 'Max',
				tableFrost: 'Alert forecasted',
				calendarEventTitle: 'Temperatures below {threshold}°{unit} forecasted in {city}, {country}',
				calendarEventBody: 'Temperature forecast for {city}, {country}: Min {min}°{unit}, Max {max}°{unit}.',
				unitCelsius: '°C',
				unitFahrenheit: '°F',
				switchToCelsius: 'Switch to Celsius',
				switchToFahrenheit: 'Switch to Fahrenheit',
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
				thresholdLabel: "Seuil d'alerte",
				tableDate: 'Date',
				tableMinTemp: 'Min',
				tableMaxTemp: 'Max',
				tableFrost: 'Alerte prévue',
				calendarEventTitle: 'Températures en dessous de {threshold}°{unit} prévues — {city}, {country}',
				calendarEventBody:
					'Prévisions de température pour {city}, {country} : Min {min}°{unit}, Max {max}°{unit}.',
				unitCelsius: '°C',
				unitFahrenheit: '°F',
				switchToCelsius: 'Passer en Celsius',
				switchToFahrenheit: 'Passer en Fahrenheit',
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
const thresholdCelsius = ref(0);
const useFahrenheit = ref(false);

const THRESHOLD_MIN_C = -20;
const THRESHOLD_MAX_C = 20;
const THRESHOLD_STEP_C = 0.5;

const THRESHOLD_MIN_F = -4;
const THRESHOLD_MAX_F = 68;
const THRESHOLD_STEP_F = 1;

function celsiusToFahrenheit(c: number): number {
	return Math.round((c * 9) / 5 + 32);
}

function fahrenheitToCelsius(f: number): number {
	return Math.round(((f - 32) * 5) / 9);
}

const thresholdMin = computed(() => (useFahrenheit.value ? THRESHOLD_MIN_F : THRESHOLD_MIN_C));
const thresholdMax = computed(() => (useFahrenheit.value ? THRESHOLD_MAX_F : THRESHOLD_MAX_C));
const thresholdStep = computed(() => (useFahrenheit.value ? THRESHOLD_STEP_F : THRESHOLD_STEP_C));

const unitLabel = computed(() =>
	useFahrenheit.value ? t('weatherForecast.unitFahrenheit') : t('weatherForecast.unitCelsius'),
);

const threshold = computed({
	get() {
		return useFahrenheit.value ? celsiusToFahrenheit(thresholdCelsius.value) : thresholdCelsius.value;
	},
	set(val: number) {
		thresholdCelsius.value = useFahrenheit.value ? fahrenheitToCelsius(val) : val;
	},
});

function displayTemp(celsius: number): number {
	return useFahrenheit.value ? celsiusToFahrenheit(celsius) : celsius;
}

function toggleUnit() {
	useFahrenheit.value = !useFahrenheit.value;
}

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

		const serverUseFahrenheit =
			json.location.temperatureUnit === 'F' || json.location.temperatureUnit === 'f';
		useFahrenheit.value = serverUseFahrenheit;

		thresholdCelsius.value = json.location.temperatureThreshold;
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
	const unit = useFahrenheit.value ? 'F' : 'C';

	return {
		title: t('weatherForecast.calendarEventTitle', {
			threshold: threshold.value,
			unit,
			city,
			country,
		}),
		description: t('weatherForecast.calendarEventBody', {
			city,
			country,
			min: displayTemp(forecast.minimum),
			max: displayTemp(forecast.maximum),
			unit,
		}),
		date: forecast.date,
	};
}

function capitalize(str: string): string {
	if (str.length === 0) return str;
	return str.charAt(0).toUpperCase() + str.slice(1);
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
					<button class="btn btn-outline-secondary" @click="fetchForecast">
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
						<div class="d-flex flex-wrap gap-3 mb-3">
							<h3 class="h5 mb-0 fw-semibold text-body-emphasis">
								{{ capitalize(d(forecast.date, 'short')) }}
							</h3>

							<div
								v-if="forecast.minimum < thresholdCelsius"
								class="badge text-bg-info align-self-start fw-medium py-sm-2"
								:title="t('weatherForecast.tableFrost')"
							>
								❄️ {{ t('weatherForecast.tableFrost') }}
							</div>

							<div
								v-if="forecast.minimum < thresholdCelsius"
								class="d-none d-md-block flex-grow-1 text-end"
							>
								<AddToCalendarButton :event="buildCalendarEvent(forecast)" />
							</div>
						</div>

						<div class="row row-cols-1 row-cols-sm-2 g-3">
							<div class="col">
								<TemperatureCard
									:label="`${t('weatherForecast.tableMinTemp')} (${unitLabel})`"
									:value="displayTemp(forecast.minimum)"
									:isDropping="
										isTemperatureDropping(forecast.minimum, data.forecasts[index - 1]?.minimum)
									"
									:isBelowThreshold="forecast.minimum < thresholdCelsius"
								/>
							</div>
							<div class="col">
								<TemperatureCard
									:label="`${t('weatherForecast.tableMaxTemp')} (${unitLabel})`"
									:value="displayTemp(forecast.maximum)"
									:isDropping="
										isTemperatureDropping(forecast.maximum, data.forecasts[index - 1]?.maximum)
									"
									:isBelowThreshold="forecast.maximum < thresholdCelsius"
								/>
							</div>
						</div>

						<div
							v-if="forecast.minimum < thresholdCelsius"
							class="d-flex justify-content-end d-md-none mt-3"
						>
							<AddToCalendarButton :event="buildCalendarEvent(forecast)" />
						</div>
					</div>
				</article>
			</div>

			<div class="mb-4 col-12 col-md-6">
				<div class="d-flex align-items-center gap-2 mb-1">
					<label for="threshold-slider" class="form-label mb-0">
						{{ t('weatherForecast.thresholdLabel') }}: {{ threshold }}&deg;{{ useFahrenheit ? 'F' : 'C' }}
					</label>
					<button
						class="btn btn-sm btn-outline-secondary"
						type="button"
						@click="toggleUnit"
						:title="useFahrenheit ? t('weatherForecast.switchToCelsius') : t('weatherForecast.switchToFahrenheit')"
					>
						{{ useFahrenheit ? '°C' : '°F' }}
					</button>
				</div>
				<input
					id="threshold-slider"
					v-model.number="threshold"
					type="range"
					class="form-range"
					:min="thresholdMin"
					:max="thresholdMax"
					:step="thresholdStep"
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
