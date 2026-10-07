import { readFileSync, writeFileSync, mkdirSync, existsSync } from 'node:fs';
import { resolve, dirname, sep } from 'node:path';
import { gunzipSync } from 'node:zlib';

// Unity packages are gzipped tar files with one GUID directory per asset.
// Keep GUID references intact while isolating every imported asset under our folder.
const packagePath = process.argv[2];
if (!packagePath) throw new Error('Usage: node import-package.mjs package.unitypackage');
const target = resolve('Assets/Visualization/Imported');
const bytes = readFileSync(packagePath);
const archive = bytes[0] === 0x1f && bytes[1] === 0x8b ? gunzipSync(bytes) : bytes;
const entries = new Map();
for (let offset = 0; offset + 512 <= archive.length;) {
  const header = archive.subarray(offset, offset + 512);
  if (header.every(b => b === 0)) break;
  const name = header.subarray(0, 100).toString().split('\0')[0].replace(/^\.\//, '');
  const size = parseInt(header.subarray(124, 136).toString().replace(/\0/g, '').trim(), 8) || 0;
  const parts = name.split('/');
  if (parts.length === 2 && ['pathname', 'asset', 'asset.meta'].includes(parts[1])) {
    if (!entries.has(parts[0])) entries.set(parts[0], {});
    entries.get(parts[0])[parts[1]] = archive.subarray(offset + 512, offset + 512 + size);
  }
  offset += 512 + Math.ceil(size / 512) * 512;
}
let count = 0;
const paths = [];
for (const value of entries.values()) {
  const source = value.pathname?.toString().split(/[\r\n\0]/)[0].trim();
  if (!source?.startsWith('Assets/') || !value.asset) continue;
  // Package editor scripts and demo scenes are unnecessary for staged renders.
  if (/\.(cs|dll|unity|zip|unitypackage)$/i.test(source)) continue;
  const path = resolve(target, source.slice(7));
  if (!path.startsWith(target + sep)) throw new Error(`Unsafe asset path: ${source}`);
  if (existsSync(path)) continue;
  mkdirSync(dirname(path), { recursive: true });
  writeFileSync(path, value.asset);
  if (value['asset.meta']) writeFileSync(path + '.meta', value['asset.meta']);
  paths.push(source);
  count++;
}
mkdirSync('output/asset-visualization/import-reports', { recursive: true });
const label = packagePath.split(/[\\/]/).pop().replace(/\.unitypackage$/i, '');
writeFileSync(`output/asset-visualization/import-reports/${label}.json`, JSON.stringify({ package: packagePath, importedRoot: target, count, paths }, null, 2));
console.log(`Imported ${count} assets from ${label} into Assets/Visualization/Imported`);
