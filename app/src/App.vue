<script setup lang="ts">
import { useI18n } from 'vue-i18n';
import { RouterView } from 'vue-router';

import AppHeader from './components/AppHeader.vue';
const { locale } = useI18n();
</script>

<template>
	<AppHeader />

	<main :lang="locale">
		<RouterView />
	</main>
</template>
