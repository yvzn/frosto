import { readdirSync, readFileSync } from 'node:fs';
import path from 'node:path';

const root = path.resolve(new URL('..', import.meta.url).pathname);
const baselineRoot = path.join(root, 'baseline');
const distRoot = path.join(root, 'dist');

function normalize(text) {
  return text
    .replace(/\r\n/g, '\n')
    .replace(/\s+/g, ' ')
    .trim();
}

function collectFiles(rootDir) {
  const out = new Set();

  function visit(dir) {
    for (const entry of readdirSync(dir, { withFileTypes: true })) {
      const full = path.join(dir, entry.name);
      if (entry.isDirectory()) {
        visit(full);
      } else {
        const rel = path.relative(rootDir, full).split(path.sep).join('/');
        out.add(rel);
      }
    }
  }

  visit(rootDir);
  return out;
}

for (const site of ['fr', 'en']) {
  const baselineDir = path.join(baselineRoot, site);
  const distDir = path.join(distRoot, site);

  const baseFiles = collectFiles(baselineDir);
  const distFiles = collectFiles(distDir);
  const missing = [...baseFiles].filter((file) => !distFiles.has(file));
  const extra = [...distFiles].filter((file) => !baseFiles.has(file));

  if (missing.length || extra.length) {
    console.error(`Mismatch for ${site}: missing=${missing.length} extra=${extra.length}`);
    if (missing.length) console.error('Missing:', missing);
    if (extra.length) console.error('Extra:', extra);
    process.exit(1);
  }

  const htmlFiles = [...baseFiles].filter((file) => file.endsWith('.html') || file === '.htaccess' || file === 'robots.txt' || file === 'sitemap.xml');

  for (const file of htmlFiles) {
    const basePath = path.join(baselineDir, file.split('/').join(path.sep));
    const distPath = path.join(distDir, file.split('/').join(path.sep));

    const a = normalize(readFileSync(basePath, 'utf8'));
    const b = normalize(readFileSync(distPath, 'utf8'));

    if (a !== b) {
      console.error(`HTML mismatch for ${site}/${file}`);
      process.exit(1);
    }
  }
}

console.log('Parity check passed for fr and en outputs.');
