#!/usr/bin/env node
/**
 * Goal: run `lingui extract --clean` but avoid churn in reference line numbers.
 * Approach:
 *  1. Snapshot existing PO files (if any) mapping msgid -> reference lines (raw string lines starting with '#: ').
 *  2. Run lingui extract (produces updated PO with fresh line numbers).
 *  3. For each message block in the new file, if the msgid existed before AND the set of referenced *files* (ignoring line numbers)
 *     matches the previous set, then restore the previous reference line numbers verbatim, keeping their ordering.
 *  4. Write back only if changes applied.
 *
 * This keeps meaningful line number changes (e.g., new files or removed references) while preventing mass renumbering
 * when only upstream code shifted lines.
 */

import {readFileSync, readdirSync, writeFileSync} from 'node:fs';

import {execSync} from 'node:child_process';
import {join} from 'node:path';

const LOCALES_DIR = join(process.cwd(), 'src', 'locales');

function parseCatalog(content) {
  const lines = content.split(/\r?\n/);
  const messages = [];
  let current = {refs: [], msgid: null, start: 0};
  function flush() {
    if (current.msgid !== null) messages.push({...current});
    current = {refs: [], msgid: null, start: 0};
  }
  for (let i = 0; i < lines.length; i++) {
    const line = lines[i];
    if (line.startsWith('#: ')) {
      current.refs.push(line); // keep raw
    } else if (line.startsWith('msgid ')) {
      current.msgid = line.replace(/^msgid\s+/, '').replace(/^"|"$/g, '');
      current.start = i;
    } else if (line === '' && current.msgid) {
      flush();
    }
  }
  flush();
  return {lines, messages};
}

function buildRefIndex(messages) {
  const map = new Map();
  for (const m of messages) {
    if (!m.msgid) continue;
    map.set(m.msgid, m.refs.slice());
  }
  return map;
}

function refFiles(refLine) {
  // refLine example: '#: src/a.svelte:12 src/b.svelte:34'
  return refLine.slice(3).split(/\s+/).filter(Boolean).map(r => r.replace(/:(\d+)(?:,\d+)?$/g, ''));
}

function normalizeFileSet(refs) {
  const files = new Set();
  for (const r of refs) for (const f of refFiles(r)) files.add(f);
  return [...files].sort().join('\u0000');
}

function indexFileSets(refMap) {
  const fileSetMap = new Map();
  for (const [msgid, refs] of refMap.entries()) {
    fileSetMap.set(msgid, normalizeFileSet(refs));
  }
  return fileSetMap;
}

function main() {
  // 1. Snapshot
  const existing = {};
  const existingFileSets = {};
  for (const entry of readdirSync(LOCALES_DIR, {withFileTypes: true})) {
    if (!entry.isFile() || !/\.po$/i.test(entry.name)) continue;
    const path = join(LOCALES_DIR, entry.name);
    const content = readFileSync(path, 'utf8');
    const parsed = parseCatalog(content);
    existing[entry.name] = parsed;
    existingFileSets[entry.name] = indexFileSets(buildRefIndex(parsed.messages));
  }

  // 2. Run lingui extract
  execSync('pnpm lingui extract --clean', {stdio: 'inherit'});

  // 3. Post-process new catalogs
  for (const entry of readdirSync(LOCALES_DIR, {withFileTypes: true})) {
    if (!entry.isFile() || !/\.po$/i.test(entry.name)) continue;
    const name = entry.name;
    if (!existing[name]) continue; // new locale; nothing to preserve yet

    const path = join(LOCALES_DIR, name);
    const contentNew = readFileSync(path, 'utf8');
    const eol = /\r\n/.test(contentNew) ? '\r\n' : '\n';
    const parsedNew = parseCatalog(contentNew);

    const oldRefMap = buildRefIndex(existing[name].messages);
    const oldFileSetMap = existingFileSets[name];

    let changed = false;
    for (const msg of parsedNew.messages) {
      if (!msg.msgid) continue;
      const oldRefs = oldRefMap.get(msg.msgid);
      if (!oldRefs || oldRefs.length === 0) continue;
      const newFileSet = normalizeFileSet(msg.refs);
      const oldFileSet = oldFileSetMap.get(msg.msgid);
      if (newFileSet === oldFileSet) {
        // Replace current ref block with old one (may be multiple lines)
        if (msg.refs.length !== oldRefs.length || msg.refs.some((r,i)=>r!==oldRefs[i])) {
          // Find where to patch lines: refs appear immediately before msgid line; there can be multiple ref lines.
          // We'll remove existing ref lines and insert old ones.
          // Locate first ref line index scanning backward from msg.start.
          let firstRefIndex = msg.start - 1;
          while (firstRefIndex >=0 && parsedNew.lines[firstRefIndex].startsWith('#: ')) firstRefIndex--;
          firstRefIndex++;
          // Remove current refs
          parsedNew.lines.splice(firstRefIndex, msg.refs.length, ...oldRefs);
          changed = true;
        }
      }
    }

    if (changed) {
      writeFileSync(path, parsedNew.lines.join(eol));
    }
  }
}

main();
