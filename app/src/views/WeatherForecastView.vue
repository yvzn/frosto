<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useI18n } from 'vue-i18n';

interface ForecastEntry {
	date: string;
	minTemperature: number;
	maxTemperature: number;
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
const { t } = useI18n();

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
	const baseUrl = import.meta.env.VITE_WEATHERFORECAST_URL as string;
	const url = `${baseUrl}/api/weather-forecast?p=${encodeURIComponent(partitionKey)}&r=${encodeURIComponent(rowKey)}`;

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

onMounted(fetchForecast);
</script>

<template>
	<div class="container py-5">
		<h1 class="fw-light mb-4">{{ t('weatherForecast.title') }}</h1>

		<div v-if="loading" class="text-muted">{{ t('weatherForecast.loading') }}</div>

		<div v-else-if="error" class="my-3">
			<p class="text-danger">{{ t('weatherForecast.error') }}</p>
			<button class="btn btn-primary" @click="fetchForecast">{{ t('weatherForecast.retry') }}</button>
		</div>

		<template v-else-if="data">
			<h2 class="h4 mb-3">{{ data.location.city }}, {{ data.location.country }}</h2>

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

			<table class="table table-striped table-bordered">
				<thead>
					<tr>
						<th>{{ t('weatherForecast.tableDate') }}</th>
						<th>{{ t('weatherForecast.tableMinTemp') }}</th>
						<th>{{ t('weatherForecast.tableMaxTemp') }}</th>
					</tr>
				</thead>
				<tbody>
					<tr v-for="forecast in data.forecasts" :key="forecast.date">
						<td>{{ forecast.date }}</td>
						<td>
							<span v-if="forecast.minTemperature < threshold" aria-label="frost">❄️</span>
							{{ forecast.minTemperature }}
						</td>
						<td>
							<span v-if="forecast.maxTemperature < threshold" aria-label="frost">❄️</span>
							{{ forecast.maxTemperature }}
						</td>
					</tr>
				</tbody>
			</table>
		</template>
	</div>
</template>
