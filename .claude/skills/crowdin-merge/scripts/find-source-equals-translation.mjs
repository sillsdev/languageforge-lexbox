#!/usr/bin/env node
// Find msgstrs that are identical to the msgid (non-empty translation = source).
// Useful for spotting brand-name passthroughs, intentional non-translations, and
// also potential translator-forgot cases.
//
// Usage: node find-source-equals-translation.mjs [<locale> ...]

import fs from 'fs';
import path from 'path';

function parsePo(content) {
  const lines = content.split('\n');
  const entries = new Map();
  let msgid = null, midParts = [], msParts = [], inMs = false, inMid = false;
  function flush() {
    if (msgid !== null) entries.set(msgid, msParts.join(''));
    msgid = null; midParts = []; msParts = []; inMs = false; inMid = false;
  }
  for (const line of lines) {
    if (line.trim() === '') { if (msgid !== null) flush(); continue; }
    if (line.startsWith('msgid "')) {
      const m = line.match(/^msgid "(.*)"$/);
      midParts = [m ? m[1] : '']; inMid = true; inMs = false;
      msgid = midParts.join(''); continue;
    }
    if (line.startsWith('msgstr "')) {
      const m = line.match(/^msgstr "(.*)"$/);
      msParts = [m ? m[1] : '']; inMs = true; inMid = false; continue;
    }
    if (line.startsWith('"') && line.endsWith('"')) {
      const m = line.match(/^"(.*)"$/); const part = m ? m[1] : '';
      if (inMs) msParts.push(part);
      else if (inMid) { midParts.push(part); msgid = midParts.join(''); }
    } else if (line.startsWith('#')) { inMs = false; inMid = false; }
  }
  if (msgid !== null) flush();
  entries.delete('');
  return entries;
}

const locales = process.argv.slice(2);
const targets = locales.length ? locales : ['es', 'fr', 'id', 'ko', 'ms', 'sw', 'vi'];

for (const l of targets) {
  const po = fs.readFileSync(`frontend/viewer/src/locales/${l}.po`, 'utf8');
  const entries = parsePo(po);
  const same = [...entries.entries()].filter(([msgid, msgstr]) => msgstr !== '' && msgstr === msgid);
  console.log(`\n=== ${l} (${same.length} msgstr=msgid) ===`);
  for (const [msgid] of same) console.log(`  ${JSON.stringify(msgid)}`);
}
