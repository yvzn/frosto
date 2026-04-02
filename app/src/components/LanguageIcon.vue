<script setup lang="ts">
import frFlag from 'flag-icons/flags/4x3/fr.svg';
import gbFlag from 'flag-icons/flags/4x3/gb.svg';

const props = defineProps<{
	language: string;
}>();
</script>

<template>
	<span
		v-if="props.language === 'en'"
		lang="en"
		aria-label="Change language to English"
		class="d-inline-flex align-items-center"
		><img :src="gbFlag" class="me-2" alt="" width="20" height="15" />English</span
	>
	<span
		v-else-if="props.language === 'fr'"
		lang="fr"
		aria-label="Changer la langue en français"
		class="d-inline-flex align-items-center"
		><img :src="frFlag" class="me-2" alt="" width="20" height="15" />Français</span
	>
</template>
