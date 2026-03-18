<script setup lang="ts">
import i18n from '../i18n';

function changeLocale(locale: string) {
	i18n.global.locale = locale;
}
</script>

<template>
	<header class="p-3 text-bg-dark shadow-sm">
		<div class="container">
			<nav class="navbar navbar-dark d-flex flex-wrap align-items-center justify-content-start">
				<a
					href="/"
					aria-label="Home page"
					class="navbar-brand d-flex align-items-center text-white text-decoration-none"
					>{{ $t('app.title') }}</a
				>

				<div class="nav ms-auto">
					<div class="dropdown">
						<button
							class="btn btn-outline-light dropdown-toggle"
							type="button"
							data-bs-toggle="dropdown"
							aria-expanded="false"
						>
							{{ $t('app.language switcher') }} {{ $i18n.locale }}
						</button>
						<ul class="dropdown-menu">
							<li
								v-for="locale in $i18n.availableLocales"
								:key="`locale-${locale}`"
								:value="locale"
							>
								<a class="dropdown-item" href="#" @click.prevent="changeLocale(locale)">{{
									locale
								}}</a>
							</li>
						</ul>
					</div>
				</div>
			</nav>
		</div>
	</header>
</template>
