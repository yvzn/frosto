<script setup lang="ts">
import { RouterLink } from 'vue-router';
import { useI18n } from 'vue-i18n';
const { t } = useI18n({
	messages: {
		en: {
			welcome: {
				heading: 'Anticipate frost',
				lead: 'Receive a free alert when the weather forecast predicts negative temperatures in your area.',
				verifyEmail: 'Please verify your email address to continue.',
				cta: 'Check Subscription',
				imageAlt: 'A person observing weather change icons',
			},
		},
		fr: {
			welcome: {
				heading: 'Anticipez les gelées',
				lead: 'Recevez gratuitement une alerte lorsque la météo prévoit des températures négatives dans votre ville.',
				verifyEmail: 'Veuillez vérifier votre adresse e-mail pour continuer.',
				cta: 'Vérifier mon inscription',
				imageAlt: 'Une personne observe des icônes de changement de météo',
			},
		},
	},
});
</script>

<template>
	<section class="container py-5">
		<div class="row flex-lg-row-reverse align-items-center py-lg-5">
			<article class="col-10 col-sm-8 col-lg-6 p-4 p-md-5">
				<img
					src="/undraw_weather_re_qsmd.svg"
					class="d-block mx-lg-auto img-fluid"
					:alt="t('welcome.imageAlt')"
					width="600"
					height="278"
				/>
			</article>
			<article class="col-lg-6 p-4 p-md-5">
				<h1 class="fw-light">{{ t('welcome.heading') }}</h1>
				<p class="lead text-muted">{{ t('welcome.lead') }}</p>
				<p>{{ t('welcome.verifyEmail') }}</p>
				<p>
					<RouterLink to="/check-subscription" class="btn btn-primary my-2">{{
						t('welcome.cta')
					}}</RouterLink>
				</p>
			</article>
		</div>
	</section>
</template>
