#!/usr/bin/env node
// Copies the types files from src/types/ into release-types/ ready for publishing.
const fs = require('fs');
const path = require('path');

const srcDir = path.join(__dirname, '..', 'src', 'types');
const destDir = path.join(__dirname, '..', 'release-types');
const files = ['fw-lite-extension.d.ts', 'enums.ts'];

for (const file of files) {
  const src = path.join(srcDir, file);
  const dest = path.join(destDir, file);
  fs.copyFileSync(src, dest);
  console.log(`Copied ${file} → release-types/${file}`);
}
