import { defineConfig } from 'vite';
import dotEnvHTMLPlugin from 'vite-plugin-dotenv-in-html';

const config = defineConfig(({ mode }) => {
	return {
		build: {
			emptyOutDir: true
		},
		base: '',
		plugins: [
			dotEnvHTMLPlugin(mode),
		]
	};
});

export default config;
