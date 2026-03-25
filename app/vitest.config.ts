import { fileURLToPath } from 'node:url';
import { defineConfig, mergeConfig } from 'vitest/config';
import viteConfig from './vite.config';

export default mergeConfig(
	viteConfig({ mode: 'test', command: 'serve', isPreview: false, isSsrBuild: false }),
	defineConfig({
		test: {
			environment: 'jsdom',
			root: fileURLToPath(new URL('./', import.meta.url)),
		},
	}),
);
