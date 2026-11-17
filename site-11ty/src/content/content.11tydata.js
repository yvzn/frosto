import path from 'node:path';

export default {
	// Flat URLs, as on the legacy sites: content/fr/sign-up.njk -> /sign-up.html, content/fr/index.njk -> /index.html.
	// Never Eleventy's default /sign-up/index.html: it would change every URL.
	permalink: (data) => `${path.posix.basename(data.page.filePathStem)}.html`,
};
