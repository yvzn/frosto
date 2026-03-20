<script setup lang="ts">
import { Head } from '@unhead/vue/components';
import { useI18n } from 'vue-i18n';
const { t, locale } = useI18n();

const checkSubscriptionUrl = import.meta.env.VITE_CHECKSUBSCRIPTION_URL;
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
			</article>
			<article class="col-md-10 mx-auto col-lg-6">
				<form
					class="p-4 p-md-5 border rounded-3 bg-light"
					:action="checkSubscriptionUrl"
					:aria-label="t('checkSubscription.formLabel')"
					method="post"
					enctype="application/x-www-form-urlencoded"
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
					<div class="form-floating mb-3" aria-hidden="true">
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
					<div class="checkbox mb-3">
						<label for="userConsent">
							<input
								type="checkbox"
								id="userConsent"
								name="userConsent"
								value="true"
								:required="true"
							/>
							<small>{{ t('checkSubscription.consent') }}</small>
						</label>
					</div>
					<input type="hidden" id="lang" name="lang" :value="locale" />
					<input type="hidden" id="source" name="source" value="app" />
					<button class="w-100 btn btn-lg btn-primary" type="submit" aria-live="polite">
						{{ t('checkSubscription.submit') }}
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
						<a class="link-dark" href="./legal.html">{{ t('checkSubscription.legalLinkText') }}</a>
					</template>
				</i18n-t>
			</p>
		</div>
	</section>
</template>
