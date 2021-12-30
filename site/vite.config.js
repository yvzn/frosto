import { defineConfig } from 'vite';

const config = defineConfig({
	build: {
		outDir: '../docs',
		emptyOutDir: true,
	},
	base: ""
});

export default config;
