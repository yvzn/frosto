<script setup lang="ts">
import { RouterLink, RouterView } from 'vue-router';
import AppHeader from './components/AppHeader.vue';
import HelloWorld from './components/HelloWorld.vue';
import { Head } from '@unhead/vue/components';
</script>

<template>
	<Head>
		<title>{{ $t('app.title') }}</title>
	</Head>

	<AppHeader />

	<main>
		<RouterView />
	</main>
</template>
