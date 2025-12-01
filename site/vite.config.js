import { resolve } from 'path';
import { defineConfig } from 'vite';

const config = defineConfig(() => {
	return {
		build: {
			emptyOutDir: true,
			rollupOptions: {
				input: {
					main: resolve(__dirname, 'index.html'),
					'sign-up': resolve(__dirname, 'sign-up.html'),
					'sign-up-complete': resolve(__dirname, 'sign-up-complete.html'),
					legal: resolve(__dirname, 'legal.html'),
					contact: resolve(__dirname, 'contact.html'),
					donate: resolve(__dirname, 'donate.html')
				}
			}
		},
		base: '',
		plugins: []
	};
});

export default config;
