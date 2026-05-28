const fs = require('node:fs');
const path = require('node:path');

const extensionRoot = path.resolve(__dirname, '..');
const releaseDir = path.join(extensionRoot, 'release');
const paranextCoreDir = path.resolve(extensionRoot, '..', '..', 'paranext-core');

if (!fs.existsSync(releaseDir)) {
  throw new Error(`Release directory not found: ${releaseDir}`);
}
if (!fs.existsSync(paranextCoreDir)) {
  throw new Error(`Core directory not found: ${paranextCoreDir}`);
}

const releaseZips = fs
  .readdirSync(releaseDir, { withFileTypes: true })
  .filter((f) => path.extname(f.name).toLowerCase() === '.zip');

if (releaseZips.length !== 1) {
  throw new Error(`Expected 1 file matching release/*.zip, but found ${releaseZips.length}.`);
}

const zipName = releaseZips[0].name;
const sourceZipPath = path.join(releaseDir, zipName);
const destDir = path.resolve(paranextCoreDir, 'dev-appdata', 'installed-extensions');
fs.mkdirSync(destDir, { recursive: true });
const destZipPath = path.join(destDir, zipName);
fs.copyFileSync(sourceZipPath, destZipPath);

console.log(`Copied ${sourceZipPath} -> ${destZipPath}`);
