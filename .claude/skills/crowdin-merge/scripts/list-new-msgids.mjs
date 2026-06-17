#!/usr/bin/env node
// Emit JSON list of msgids added to en.po by the most recent merge commit.
// Used to seed the i18n-context-writer agent so it processes exactly the new
// strings — no more, no less. Determinism > "ask the model to find them".
//
// Usage: node list-new-msgids.mjs [<merge-commit-ref>]
//        defaults to HEAD (assumes you just ran `git merge` and committed)

import fs from 'fs';
import {execSync} from 'child_process';

const ref = process.argv[2] || 'HEAD';
const file = 'frontend/viewer/src/locales/en.po';

// Strategy: parse the full en.po at HEAD and at origin/develop, then emit any
// msgid present in HEAD but not in develop. More reliable than parsing the diff.
const headEn = execSync(`git show ${ref}:${file}`, {encoding: 'utf8'});
const devEn = execSync(`git show origin/develop:${file}`, {encoding: 'utf8'});

function parseEntries(content) {
  const lines = content.split('\n');
  const entries = new Map();
  let cur = {msgid: null, msgidParts: [], srcRefs: [], comments: []};
  let inMsgid = false, inMsgstr = false;
  function flush() {
    if (cur.msgid !== null && cur.msgid !== '') {
      entries.set(cur.msgid, {sources: cur.srcRefs, comments: cur.comments});
    }
    cur = {msgid: null, msgidParts: [], srcRefs: [], comments: []};
    inMsgid = false; inMsgstr = false;
  }
  for (const line of lines) {
    if (line.trim() === '') { flush(); continue; }
    if (line.startsWith('#.')) cur.comments.push(line.slice(2).trim());
    else if (line.startsWith('#:')) cur.srcRefs.push(line.slice(2).trim());
    else if (line.startsWith('msgid "')) {
      const m = line.match(/^msgid "(.*)"$/);
      cur.msgidParts = [m ? m[1] : '']; cur.msgid = cur.msgidParts.join('');
      inMsgid = true; inMsgstr = false;
    } else if (line.startsWith('msgstr "')) { inMsgstr = true; inMsgid = false; }
    else if (line.startsWith('"') && line.endsWith('"')) {
      if (inMsgid) { const m = line.match(/^"(.*)"$/); cur.msgidParts.push(m ? m[1] : ''); cur.msgid = cur.msgidParts.join(''); }
    }
  }
  flush();
  return entries;
}

const headEntries = parseEntries(headEn);
const devEntries = parseEntries(devEn);

const result = [];
for (const [msgid, info] of headEntries.entries()) {
  if (!devEntries.has(msgid)) {
    result.push({
      msgid,
      sources: info.sources,
      hasContext: info.comments.length > 0,
    });
  }
}

console.log(JSON.stringify(result, null, 2));
