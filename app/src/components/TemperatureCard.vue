<script setup lang="ts">
import { useI18n } from 'vue-i18n';
const { t } = useI18n({
	messages: {
		en: {
			isDropping: 'dropping',
		},
		fr: {
			isDropping: 'en baisse',
		},
	},
});

const props = defineProps<{
	label: string;
	value: number;
	isDropping: boolean;
	isBelowThreshold: boolean;
}>();
</script>

<template>
	<div
		class="d-flex flex-row flex-wrap column-gap-1 align-items-center p-3 rounded-4"
		:class="{
			'bg-body-tertiary': !props.isBelowThreshold,
			'bg-info': props.isBelowThreshold,
			'bg-opacity-10': props.isBelowThreshold,
		}"
	>
		<div class="w-100 text-body-secondary small mb-1">{{ props.label }}</div>
		<div class="fs-4 lh-sm text-body-emphasis">
			<strong> {{ props.value }}&deg; </strong>
		</div>
		<div
			v-if="props.isDropping"
			class="badge rounded-pill text-bg-secondary bg-opacity-10"
			:title="t('isDropping')"
		>
			📉
		</div>
	</div>
</template>
