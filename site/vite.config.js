import { resolve } from 'path';
import { defineConfig } from 'vite';

var pages = [
	'index', 'sign-up', 'sign-up-complete', 'legal', 'contact', 'donate', 'check-subscription', 'check-subscription-complete', 'unsubscribe', 'unsubscribe-complete'
]

const config = defineConfig(() => {
	return {
		build: {
			emptyOutDir: true,
			rollupOptions: {
				input: pages.reduce((inputs, page) => {
					inputs[page] = resolve(__dirname, `${page}.html`);
					return inputs;
				}, {})
			}
		},
		base: '',
		plugins: []
	};
});

export default config;
