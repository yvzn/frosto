<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { Head } from '@unhead/vue/components';
import TemperatureCard from '@/components/TemperatureCard.vue';

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
const { t, d } = useI18n({
	messages: {
		en: {
			weatherForecast: {
				title: 'Weather Forecast',
				loading: 'Loading forecast…',
				error: 'An error occurred while loading the forecast.',
				retry: 'Retry',
				thresholdLabel: 'Alert threshold (°C)',
				tableDate: 'Date',
				tableMinTemp: 'Min (°C)',
				tableMaxTemp: 'Max (°C)',
				tableFrost: 'Frost expected',
			},
		},
		fr: {
			weatherForecast: {
				title: 'Prévisions météo',
				loading: 'Chargement des prévisions…',
				error: 'Une erreur est survenue lors du chargement des prévisions.',
				retry: 'Réessayer',
				thresholdLabel: "Seuil d'alerte (°C)",
				tableDate: 'Date',
				tableMinTemp: 'Min (°C)',
				tableMaxTemp: 'Max (°C)',
				tableFrost: 'Gelée prévue',
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
			<h2 class="h4 mb-3 placeholder-glow">
				<span class="placeholder bg-secondary w-50"></span>
			</h2>

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
			<h2 class="h4 mb-3">{{ data.location.city }}, {{ data.location.country }}</h2>

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
					</div>
				</article>
			</div>

			<div class="mb-4">
				<label for="threshold-slider" class="form-label">
					{{ t('weatherForecast.thresholdLabel') }}: {{ threshold }}
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
</template>
