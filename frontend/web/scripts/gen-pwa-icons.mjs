// Генератор PWA-иконок из public/pwa/icon.svg.
// Разовый скрипт: требует dev-зависимость sharp (в постоянные deps НЕ входит,
// чтобы не тормозить CI). Запуск при обновлении иконки:
//   npm i -D sharp && node scripts/gen-pwa-icons.mjs && npm r sharp
import sharp from 'sharp';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, resolve } from 'node:path';

const dir = dirname(fileURLToPath(import.meta.url));
const pub = resolve(dir, '..', 'public');
const svg = readFileSync(resolve(pub, 'pwa', 'icon.svg'));

const targets = [
  { size: 192, out: resolve(pub, 'pwa', 'icon-192.png') },
  { size: 512, out: resolve(pub, 'pwa', 'icon-512.png') },
  // apple-touch-icon кладём в корень public — iOS ищет его там.
  { size: 180, out: resolve(pub, 'apple-touch-icon.png') },
];

for (const t of targets) {
  await sharp(svg, { density: 384 }).resize(t.size, t.size).png().toFile(t.out);
  console.log('generated', t.out);
}
