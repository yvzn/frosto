import { fileURLToPath, URL } from 'node:url';

import vue from '@vitejs/plugin-vue';
import { defineConfig } from 'vite';

// https://vite.dev/config/
export default defineConfig({
	base: '/app/',
	plugins: [vue()],
	resolve: {
		alias: {
			'@': fileURLToPath(new URL('./src', import.meta.url)),
		},
	},
	css: {
		preprocessorOptions: {
			scss: {
				// https://getbootstrap.com/docs/5.3/getting-started/vite/
				silenceDeprecations: ['import', 'color-functions', 'global-builtin', 'if-function'],
			},
		},
	},
});
