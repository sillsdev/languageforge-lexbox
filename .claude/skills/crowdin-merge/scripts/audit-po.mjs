#!/usr/bin/env node
// Pre-flight audit. Compares each target locale on `develop` vs `origin/l10n_develop`
// to detect translations that would be silently lost if we merge the Crowdin PR as-is.
//
// Exits 1 if data loss is detected (msgid still present in source but msgstr
// would go non-empty → empty). Run before merge-crowdin.ps1 does anything destructive.
//
// Usage: node audit-po.mjs [<locale> ...]
//        defaults to all 7 target locales

import fs from 'fs';
import path from 'path';
import os from 'os';
import {execSync} from 'child_process';

const locales = process.argv.slice(2);
const targetLocales = locales.length ? locales : ['es', 'fr', 'id', 'ko', 'ms', 'sw', 'vi'];

function parsePo(content) {
  const lines = content.split('\n');
  const entries = new Map();
  let msgid = null, msgidParts = [], msgstrParts = [], inMsgstr = false, inMsgid = false;
  function flush() {
    if (msgid !== null) entries.set(msgid, msgstrParts.join(''));
    msgid = null; msgidParts = []; msgstrParts = []; inMsgstr = false; inMsgid = false;
  }
  for (const line of lines) {
    if (line.trim() === '') { if (msgid !== null) flush(); continue; }
    if (line.startsWith('msgid "')) {
      const m = line.match(/^msgid "(.*)"$/);
      msgidParts = [m ? m[1] : '']; inMsgid = true; inMsgstr = false;
      msgid = msgidParts.join(''); continue;
    }
    if (line.startsWith('msgstr "')) {
      const m = line.match(/^msgstr "(.*)"$/);
      msgstrParts = [m ? m[1] : '']; inMsgstr = true; inMsgid = false; continue;
    }
    if (line.startsWith('"') && line.endsWith('"')) {
      const m = line.match(/^"(.*)"$/); const part = m ? m[1] : '';
      if (inMsgstr) msgstrParts.push(part);
      else if (inMsgid) { msgidParts.push(part); msgid = msgidParts.join(''); }
      continue;
    }
    if (line.startsWith('#')) { inMsgstr = false; inMsgid = false; }
  }
  if (msgid !== null) flush();
  entries.delete('');
  return entries;
}

function readRef(ref, file) {
  return execSync(`git show ${ref}:${file}`, {encoding: 'utf8', stdio: ['pipe', 'pipe', 'pipe']});
}

function audit(locale) {
  const file = `frontend/viewer/src/locales/${locale}.po`;
  let devContent, l10nContent;
  try {
    devContent = readRef('origin/develop', file);
    l10nContent = readRef('origin/l10n_develop', file);
  } catch (e) {
    console.error(`Cannot read ${file} from both refs: ${e.message}`);
    return {locale, error: e.message};
  }
  const dev = parsePo(devContent);
  const l10n = parsePo(l10nContent);

  const dataLoss = [], legitDel = [], changed = [], gained = [];
  for (const [msgid, devMsgstr] of dev.entries()) {
    if (devMsgstr === '') {
      if (l10n.has(msgid) && l10n.get(msgid) !== '') gained.push({msgid, msgstr: l10n.get(msgid)});
      continue;
    }
    if (!l10n.has(msgid)) legitDel.push({msgid, msgstr: devMsgstr});
    else {
      const l10nM = l10n.get(msgid);
      if (l10nM === '') dataLoss.push({msgid, msgstr: devMsgstr});
      else if (l10nM !== devMsgstr) changed.push({msgid, develop: devMsgstr, l10n: l10nM});
    }
  }
  return {locale, dataLoss, legitDel, changed, gained, devSize: dev.size, l10nSize: l10n.size};
}

let anyDataLoss = false;
let anyReadError = false;
const results = targetLocales.map(audit);
for (const r of results) {
  console.log(`\n=== ${r.locale} ===`);
  if (r.error) { console.log(`error: ${r.error}`); anyReadError = true; continue; }
  console.log(`  develop entries: ${r.devSize}  |  l10n_develop entries: ${r.l10nSize}`);
  console.log(`  legitimate deletions: ${r.legitDel.length}`);
  console.log(`  crowdin-changed translations: ${r.changed.length}`);
  console.log(`  translations gained: ${r.gained.length}`);
  console.log(`  *** data loss: ${r.dataLoss.length} ***`);
  if (r.dataLoss.length) {
    anyDataLoss = true;
    console.log(`\n  Data-loss entries (msgid still in product, but Crowdin would wipe translation):`);
    for (const e of r.dataLoss) {
      console.log(`    ${JSON.stringify(e.msgid)}  →  was: ${JSON.stringify(e.msgstr)}`);
    }
  }
}

if (anyReadError) {
  console.error(`\nABORT: could not read one or more locale files from origin/develop or origin/l10n_develop.`);
  console.error(`Possible cause: locale added on one branch but not the other. Investigate before merging.`);
  process.exit(1);
}
if (anyDataLoss) {
  console.error(`\nABORT: data loss detected. Push the at-risk translations to Crowdin before merging,`);
  console.error(`or confirm with the user that the loss is intentional.`);
  process.exit(1);
}
console.log(`\nNo data loss. Safe to merge.`);
