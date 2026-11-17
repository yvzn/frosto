import path from 'node:path';

const pageName = (data) => path.posix.basename(data.page.filePathStem);

export default {
	// Flat URLs: content/fr/sign-up.njk -> /sign-up.html, content/fr/index.njk -> /index.html.
	// Never Eleventy's default /sign-up/index.html: it would change every URL.
	permalink: (data) => `${pageName(data)}.html`,

	// Defaults for the front matter read by src/_includes/layouts/base.njk.
	canonical: true, // canonical + hreflang links; false on the "…-complete" pages
	noindex: false, // <meta name="robots" content="noindex" />
	header: 'full', // 'full' (with the language switch) or 'simple'
	script: 'main', // page script: ./<script>.js

	eleventyComputed: {
		// Public path of the page, used in canonical, hreflang and og:url: "/" for the home page.
		pagePath: (data) => (pageName(data) === 'index' ? '/' : `/${pageName(data)}.html`),
	},
};
