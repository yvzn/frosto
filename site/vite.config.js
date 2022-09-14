import { resolve } from 'path';
import { defineConfig } from 'vite';
import dotEnvHTMLPlugin from 'vite-plugin-dotenv-in-html';

const config = defineConfig(({ mode }) => {
	return {
		build: {
			emptyOutDir: true,
			rollupOptions: {
				input: {
					main: resolve(__dirname, 'index.html'),
					'sign-up': resolve(__dirname, 'sign-up.html'),
					'sign-up-complete': resolve(__dirname, 'sign-up-complete.html')
				}
			}
		},
		base: '',
		plugins: [
			dotEnvHTMLPlugin(mode),
		]
	};
});

export default config;
