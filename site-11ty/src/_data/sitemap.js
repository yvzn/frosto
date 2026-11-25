// Pages listed in sitemap.xml, in this order, on both sites (src/public/sitemap.xml.njk).
// Other pages (forms results, unsubscribe…) are not listed: most are noindex.
// Values are strings so they are printed as-is: "1.0", not 1.
export default [
	{ path: '/', lastmod: '2026-01-21', changefreq: 'monthly', priority: '1.0' },
	{ path: '/sign-up.html', lastmod: '2025-09-08', changefreq: 'yearly', priority: '0.9' },
	{ path: '/contact.html', lastmod: '2025-09-08', changefreq: 'yearly', priority: '0.7' },
	{ path: '/legal.html', lastmod: '2026-11-25', changefreq: 'yearly', priority: '0.5' },
];
