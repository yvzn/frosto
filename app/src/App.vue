<script setup lang="ts">
import { RouterLink, RouterView } from 'vue-router';
import AppHeader from './components/AppHeader.vue';
import HelloWorld from './components/HelloWorld.vue';
</script>

<template>
	<AppHeader />

	<main>
		<RouterView />
	</main>
</template>
