<script setup lang="ts">
import { Head } from '@unhead/vue/components';
import { useI18n } from 'vue-i18n';

import TheWelcome from '@/components/TheWelcome.vue';
const { t } = useI18n();
</script>

<template>
	<Head>
		<title>{{ t('app.homePage') }} &ndash; {{ t('app.title') }}</title>
		<meta name="description" :content="t('app.description')" />
	</Head>
	<main>
		<TheWelcome />
	</main>
</template>
