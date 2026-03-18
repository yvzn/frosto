<script setup lang="ts">
import TheWelcome from '../components/TheWelcome.vue';
import { Head } from '@unhead/vue/components';
import { useI18n } from 'vue-i18n';
const { t } = useI18n();
</script>

<template>
	<Head>
		<title>{{ t('app.homePage') }} &ndash; {{ t('app.title') }}</title>
	</Head>
	<main>
		<TheWelcome />
	</main>
</template>
