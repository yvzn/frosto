<script setup lang="ts">
import { Head } from '@unhead/vue/components';
import { RouterLink } from 'vue-router';
import { useI18n } from 'vue-i18n';
const { t } = useI18n();
</script>

<template>
	<Head>
		<title>{{ t('checkSubscriptionComplete.pageTitle') }} &ndash; {{ t('app.title') }}</title>
		<meta name="robots" content="noindex" />
	</Head>
	<section class="container py-5">
		<div class="row flex-lg-row-reverse align-items-center">
			<article class="col-8 col-sm-6 col-lg-5 p-4 p-md-5">
				<img
					src="/undraw_preferences-popup_cru5.svg"
					class="d-block mx-lg-auto img-fluid"
					:alt="t('checkSubscriptionComplete.imageAlt')"
					width="600"
					height="429"
				/>
			</article>
			<article class="col-lg-7 p-4 p-md-5">
				<h1 class="fw-light">{{ t('checkSubscriptionComplete.heading') }}</h1>
				<p class="lead text-muted">{{ t('checkSubscriptionComplete.lead') }}</p>
				<p>
					<RouterLink to="/" class="btn btn-outline-success my-2">{{
						t('app.backToHome')
					}}</RouterLink>
				</p>
			</article>
		</div>
	</section>
</template>
