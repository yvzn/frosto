<script setup lang="ts">
import { ref, onMounted, onBeforeUnmount, computed, useTemplateRef } from 'vue';
import { Head } from '@unhead/vue/components';
import { useI18n } from 'vue-i18n';
const { t, locale } = useI18n();

const checkSubscriptionUrl = import.meta.env.VITE_CHECKSUBSCRIPTION_URL;
const supportUrl = import.meta.env.VITE_SUPPORT_URL;
const signUpFormUrl = computed(() => {
	switch (locale.value) {
		case 'fr':
			return import.meta.env.VITE_SITE_FR_URL + '/sign-up.html';
		default:
			return import.meta.env.VITE_SITE_EN_URL + '/sign-up.html';
	}
});
const legalUrl = computed(() => {
	switch (locale.value) {
		case 'fr':
			return import.meta.env.VITE_SITE_FR_URL + '/legal.html';
		default:
			return import.meta.env.VITE_SITE_EN_URL + '/legal.html';
	}
});

const SLOW_LOADING_THRESHOLD_MS = 10_000;
const FAILURE_TIMEOUT_MS = 50_000;
const HEALTH_CHECK_TIMEOUT_MS = 30_000;

type FormStatus = 'IDLE' | 'PENDING' | 'FAILED';
const formStatus = ref<FormStatus>('IDLE');
const submitLabel = ref<string | null>(null);
const failureMessage = ref(false);
const reasonField = useTemplateRef('reasonField');

let updateLoadingTextTimeoutId: ReturnType<typeof setTimeout> | null = null;
let showFailureTextTimeoutId: ReturnType<typeof setTimeout> | null = null;

function onSubmit(event: Event) {
	if (formStatus.value === 'IDLE') {
		formStatus.value = 'PENDING';
		submitLabel.value = null;

		updateLoadingTextTimeoutId = setTimeout(updateLoadingText, SLOW_LOADING_THRESHOLD_MS);

		// trick to allow setTimeout during form submission
		event.preventDefault();
		(event.target as HTMLFormElement).submit();
	} else {
		event.preventDefault();
	}
}

function updateLoadingText() {
	submitLabel.value = 'slow';
	showFailureTextTimeoutId = setTimeout(showFailureText, FAILURE_TIMEOUT_MS);
}

function showFailureText() {
	formStatus.value = 'FAILED';
	failureMessage.value = true;
}

function retryPage() {
	location.reload();
}

function clearLoadingTimeouts() {
	if (updateLoadingTextTimeoutId !== null) {
		clearTimeout(updateLoadingTextTimeoutId);
	}
	if (showFailureTextTimeoutId !== null) {
		clearTimeout(showFailureTextTimeoutId);
	}
}

function healthCheck(retries: number) {
	const request = new XMLHttpRequest();

	request.timeout = HEALTH_CHECK_TIMEOUT_MS;
	const onRetry = function () {
		if (retries > 1) {
			healthCheck(retries - 1);
		}
	};
	request.ontimeout = onRetry;
	request.onerror = onRetry;

	request.open('GET', supportUrl, true);
	request.send();
}

onMounted(() => {
	healthCheck(3);
	fixAccessibility();
});

function fixAccessibility() {
	const style = reasonField.value?.style;
	if (style) {
		style.position = 'absolute';
		style.width = '1px';
		style.height = '1px';
		style.overflow = 'hidden';
	}
}

onBeforeUnmount(() => {
	clearLoadingTimeouts();
});
</script>

<template>
	<Head>
		<title>{{ t('checkSubscription.pageTitle') }} &ndash; {{ t('app.title') }}</title>
		<meta name="robots" content="noindex" />
	</Head>
	<section class="py-5 container">
		<div class="row py-lg-5">
			<article class="col-lg-6 p-4 p-md-5">
				<h1 class="fw-light mb-3">{{ t('checkSubscription.heading') }}</h1>
				<p class="lead text-muted col-lg-10">{{ t('checkSubscription.lead') }}</p>
				<p class="text-muted col-lg-10">{{ t('checkSubscription.description') }}</p>
				<p class="text-muted">
					{{ t('checkSubscription.noSubscription') }}
					<a :href="signUpFormUrl" class="link-secondary">{{
						t('checkSubscription.signUpLink')
					}}</a>
				</p>
			</article>
			<article class="col-md-10 mx-auto col-lg-6">
				<form
					class="p-4 p-md-5 border rounded-3 bg-light"
					:action="checkSubscriptionUrl"
					:aria-label="t('checkSubscription.formLabel')"
					method="post"
					enctype="application/x-www-form-urlencoded"
					@submit="onSubmit"
				>
					<div class="form-floating mb-3">
						<input
							type="email"
							class="form-control"
							id="email"
							name="email"
							:placeholder="t('checkSubscription.emailPlaceholder')"
							:required="true"
							autocomplete="email"
						/>
						<label for="email">{{ t('checkSubscription.emailLabel') }}</label>
					</div>
					<div class="form-floating mb-3" inert aria-hidden="true" ref="reasonField">
						<input
							type="text"
							class="form-control"
							id="reason"
							name="reason"
							:placeholder="t('checkSubscription.reasonPlaceholder')"
							autocomplete="off"
						/>
						<label for="reason">{{ t('checkSubscription.reasonLabel') }}</label>
					</div>
					<div class="form-check mb-3">
						<input
							type="checkbox"
							class="form-check-input"
							id="userConsent"
							name="userConsent"
							value="true"
							:required="true"
						/>
						<label class="form-check-label" for="userConsent">
							<small>{{ t('checkSubscription.consent') }}</small>
						</label>
					</div>
					<input type="hidden" id="lang" name="lang" :value="locale" />
					<input type="hidden" id="source" name="source" value="app" />
					<div v-if="failureMessage" class="alert alert-danger mt-3" role="alert">
						{{ t('checkSubscription.failureMessage') }}
					</div>
					<button
						v-if="formStatus !== 'FAILED'"
						class="w-100 btn btn-lg btn-primary"
						type="submit"
						aria-live="polite"
					>
						<template v-if="formStatus === 'IDLE'">
							{{ t('checkSubscription.submit') }}
						</template>
						<template v-else-if="submitLabel === 'slow'">
							<span
								class="spinner-border spinner-border-sm fs-6"
								role="status"
								aria-hidden="true"
							></span>
							{{ t('checkSubscription.takingLonger') }}
						</template>
						<template v-else>
							<span
								class="spinner-border spinner-border-sm fs-6"
								role="status"
								aria-hidden="true"
							></span>
							{{ t('checkSubscription.pleaseWait') }}
						</template>
					</button>
					<button v-else class="w-100 btn btn-lg btn-primary" type="button" @click="retryPage">
						{{ t('checkSubscription.retry') }}
					</button>
				</form>
			</article>
		</div>
	</section>
	<section class="py-5 bg-light">
		<div class="container pb-lg-5">
			<p>{{ t('checkSubscription.legalText') }}</p>
			<p>
				<i18n-t keypath="checkSubscription.legalLink" tag="span">
					<template #link>
						<a class="link-dark" :href="legalUrl" target="_blank">{{
							t('checkSubscription.legalLinkText')
						}}</a>
					</template>
				</i18n-t>
			</p>
		</div>
	</section>
</template>
