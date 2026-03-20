import { fileURLToPath, URL } from 'node:url';

import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';

// https://vite.dev/config/
export default defineConfig(({ mode }) => ({
	base: mode === 'production' ? '/app/' : '/',
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
				silenceDeprecations: [
					'import',
					'mixed-decls',
					'color-functions',
					'global-builtin',
					'if-function',
				],
			},
		},
	}
}),
);
