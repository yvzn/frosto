import { resolve } from 'path';
import { defineConfig } from 'vite';

const config = defineConfig({
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
	base: ''
});

export default config;
