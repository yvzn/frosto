<script setup lang="ts">
import { useI18n } from 'vue-i18n';

import LanguageIcon from './LanguageIcon.vue';
const { t, locale, availableLocales } = useI18n();

const baseUrl = import.meta.env.BASE_URL;

function changeLocale(newLocale: string) {
	locale.value = newLocale;
}
</script>

<template>
	<header class="p-3 text-bg-dark shadow-sm" :lang="locale">
		<div class="container">
			<nav class="navbar navbar-dark d-flex flex-wrap align-items-center justify-content-start">
				<a
					:href="baseUrl"
					:title="t('app.backToHome')"
					class="navbar-brand d-flex align-items-center text-white text-decoration-none"
					>{{ t('app.title') }}</a
				>

				<div class="nav ms-auto">
					<div class="dropdown">
						<button
							class="btn btn-outline-light dropdown-toggle"
							type="button"
							data-bs-toggle="dropdown"
							aria-expanded="false"
							:title="t('app.languageSwitcher')"
						>
							<LanguageIcon :language="locale" />
						</button>
						<ul class="dropdown-menu">
							<li v-for="l in availableLocales" :key="`locale-${l}`" :value="l">
								<a class="dropdown-item" href="#" @click.prevent="changeLocale(l)">
									<LanguageIcon :language="l" />
								</a>
							</li>
						</ul>
					</div>
				</div>
			</nav>
		</div>
	</header>
</template>
