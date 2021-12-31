import { resolve } from 'path';
import { defineConfig } from 'vite';

const config = defineConfig({
	build: {
		outDir: '../docs',
		emptyOutDir: true,
		rollupOptions: {
			input: {
				main: resolve(__dirname, 'index.html'),
				'sign-up': resolve(__dirname, 'sign-up.html')
			}
		}
	},
	base: ''
});

export default config;
