#!/usr/bin/env node
// For each non-English locale, list msgstrs that the merged Crowdin commits ADDED or CHANGED
// vs `origin/develop`. Output is JSON consumed by the i18n-translation-reviewer agent so it
// only reviews what's new — not the whole 375-string catalog.
//
// Usage: node list-incoming-translations.mjs
//        run after merge-crowdin.ps1 has committed the merge; assumes HEAD is the merge commit on l10n_develop.

import {execSync} from 'child_process';

const locales = ['es', 'fr', 'id', 'ko', 'ms', 'sw', 'vi'];

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

function readRef(ref, file) {
  return execSync(`git show ${ref}:${file}`, {encoding: 'utf8', stdio: ['pipe', 'pipe', 'pipe']});
}

const result = {};
for (const locale of locales) {
  const file = `frontend/viewer/src/locales/${locale}.po`;
  let dev, l10n;
  try {
    l10n = parsePo(readRef('HEAD', file));
  } catch (e) {
    console.error(`warn: skipping ${locale} — cannot read ${file} from HEAD`);
    continue;
  }
  try {
    dev = parsePo(readRef('origin/develop', file));
  } catch (e) {
    dev = new Map(); // locale absent on develop (newly added) — every translation is incoming
  }

  const incoming = [];
  for (const [msgid, l10nMs] of l10n.entries()) {
    const devMs = dev.get(msgid);
    if (devMs === undefined) {
      // new msgid; if Crowdin already translated it, it's worth reviewing
      if (l10nMs !== '') incoming.push({msgid, msgstr: l10nMs, change: 'new'});
    } else if (devMs === '' && l10nMs !== '') {
      incoming.push({msgid, msgstr: l10nMs, change: 'filled'});
    } else if (devMs !== '' && l10nMs !== devMs) {
      incoming.push({msgid, msgstr: l10nMs, prevMsgstr: devMs, change: 'retranslated'});
    }
  }
  if (incoming.length > 0) result[locale] = incoming;
}

console.log(JSON.stringify(result, null, 2));
