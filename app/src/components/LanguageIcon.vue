<script setup lang="ts">
const props = defineProps<{
	language: string;
}>();
</script>

<template>
	<span
		class="fi me-2"
		:class="{
			'fi-gb': props.language === 'en',
			'fi-fr': props.language === 'fr',
		}"
	></span>
	<span v-if="props.language === 'en'" lang="en" aria-label="Change language to English"
		>English</span
	>
	<span v-else-if="props.language === 'fr'" lang="fr" aria-label="Changer la langue en français"
		>Français</span
	>
</template>
