import path from 'node:path';

const site = process.env.SITE || 'fr';
const contentRoot = path.join('src', 'content', site);
const distRoot = path.join('dist', site);

export default function (eleventyConfig) {
  eleventyConfig.setInputDirectory(contentRoot);
  eleventyConfig.setOutputDirectory(distRoot);
  eleventyConfig.addPassthroughCopy({
    [path.join(contentRoot, 'asset')]: 'asset',
    [path.join(contentRoot, 'public')]: '/',
  });

  eleventyConfig.addGlobalData('site', site);

  return {
    dir: {
      input: contentRoot,
      output: distRoot,
      includes: '_includes',
      layouts: '_includes/layouts',
      data: '_data',
    },
    templateFormats: ['html', 'njk', 'md'],
    htmlTemplateEngine: 'njk',
    markdownTemplateEngine: 'njk',
  };
}
