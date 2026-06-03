#!/usr/bin/env node
// Apply verdict-driven fixes to locale .po files.
//
// Input: verdict JSON files at $TEMP/verdicts-<locale>.json produced by reviewer agents.
// For each entry with verdict === "fix", finds the msgid block in
// frontend/viewer/src/locales/<locale>.po and replaces its msgstr with `suggested`.
//
// Usage: node apply-fixes.mjs [<locale> ...]
//        defaults to all 7 target locales

import fs from 'fs';
import path from 'path';
import os from 'os';

const targets = process.argv.slice(2);
const locales = targets.length ? targets : ['es', 'fr', 'id', 'ko', 'ms', 'sw', 'vi'];
const tmp = os.tmpdir();

function escapeForRegex(s) {
  return s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}

const summary = {};

for (const locale of locales) {
  const verdictPath = path.join(tmp, `verdicts-${locale}.json`);
  if (!fs.existsSync(verdictPath)) {
    console.warn(`skip ${locale}: ${verdictPath} not found`);
    continue;
  }
  const verdicts = JSON.parse(fs.readFileSync(verdictPath, 'utf8'));
  const fixes = verdicts.filter(v => v.verdict === 'fix');
  if (fixes.length === 0) {
    summary[locale] = 0;
    continue;
  }

  const poPath = `frontend/viewer/src/locales/${locale}.po`;
  let content = fs.readFileSync(poPath, 'utf8');
  let applied = 0;
  const failed = [];

  for (const fix of fixes) {
    if (typeof fix.suggested !== 'string') {
      failed.push({msgid: fix.msgid, reason: 'no suggested value'});
      continue;
    }
    // msgid and suggested arrive in PO-escaped form (verbatim from parsePo via the verdict
    // JSON), so use them as-is — re-escaping here would double-escape `\"` `\\` `\n`.
    // Refuse a suggested value that isn't valid single-line PO content (raw control char or
    // unescaped quote) — writing it verbatim would corrupt the .po file.
    if (/[\n\r\t]/.test(fix.suggested) || /(^|[^\\])(\\\\)*"/.test(fix.suggested)) {
      failed.push({msgid: fix.msgid, reason: 'suggested is not PO-escaped (raw control char or unescaped quote) — needs manual edit'});
      continue;
    }
    // Refuse to touch multi-line msgstrs — the regex below would silently drop continuation
    // lines. Detected by: msgid line followed by `msgstr "..."` followed by a `"..."` line.
    const multiLinePattern = new RegExp(
      '^msgid "' + escapeForRegex(fix.msgid) + '"\\nmsgstr ".*"\\n"',
      'm'
    );
    if (multiLinePattern.test(content)) {
      failed.push({msgid: fix.msgid, reason: 'msgstr spans multiple lines — needs manual edit'});
      continue;
    }
    // Single-line msgstr — safe to regex-replace.
    const pattern = new RegExp(
      '^msgid "' + escapeForRegex(fix.msgid) + '"\\n(msgstr ")(.*)(")$',
      'm'
    );
    const before = content;
    content = content.replace(pattern, (_full, prefix, _old, suffix) => {
      applied++;
      return `msgid "${fix.msgid}"\n${prefix}${fix.suggested}${suffix}`;
    });
    if (content === before) {
      failed.push({msgid: fix.msgid, reason: 'msgid not found'});
    }
  }

  fs.writeFileSync(poPath, content);
  summary[locale] = applied;
  if (failed.length) {
    console.warn(`${locale}: ${failed.length} fix(es) could not be applied:`);
    for (const f of failed) console.warn(`  - ${JSON.stringify(f.msgid)}: ${f.reason}`);
  }
}

console.log('\nApplied fix counts:');
for (const [l, n] of Object.entries(summary)) console.log(`  ${l}: ${n}`);
console.log(`Total: ${Object.values(summary).reduce((a, b) => a + b, 0)}`);
