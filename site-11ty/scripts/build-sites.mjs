import { cpSync, existsSync, mkdirSync, readdirSync, readFileSync, writeFileSync } from 'node:fs';
import path from 'node:path';

const root = path.resolve(new URL('..', import.meta.url).pathname);
const baselineRoot = path.join(root, 'baseline');
const distRoot = path.join(root, 'dist');

function copyDir(src, dest) {
  mkdirSync(dest, { recursive: true });
  for (const entry of readdirSync(src, { withFileTypes: true })) {
    const srcPath = path.join(src, entry.name);
    const targetPath = path.join(dest, entry.name);
    if (entry.isDirectory()) {
      copyDir(srcPath, targetPath);
    } else {
      cpSync(srcPath, targetPath, { recursive: false });
    }
  }
}

for (const site of ['fr', 'en']) {
  const source = path.join(baselineRoot, site);
  const target = path.join(distRoot, site);
  mkdirSync(path.dirname(target), { recursive: true });
  copyDir(source, target);

  const staticFiles = [
    ['.htaccess', '.htaccess'],
    ['robots.txt', 'robots.txt'],
    ['sitemap.xml', 'sitemap.xml'],
  ];

  for (const [inputName, outputName] of staticFiles) {
    const inputPath = path.join(source, inputName);
    if (existsSync(inputPath)) {
      writeFileSync(path.join(target, outputName), readFileSync(inputPath));
    }
  }
}

console.log('Built static outputs for fr and en into dist/fr and dist/en');
