#!/usr/bin/env node
// Verify agent outputs cover every input. Two modes:
//
//   node check-completeness.mjs context <decisions.json>
//     decisions.json is array of {msgid, decision: "context-added"|"skipped-obvious"}
//     Compares against list-new-msgids.mjs output to ensure every new msgid was addressed.
//
//   node check-completeness.mjs review <verdicts.json>
//     verdicts.json is {locale: [{msgid, verdict: "ok"|"fix"|"flag", ...}]}
//     Compares against list-incoming-translations.mjs output to ensure every incoming
//     translation was reviewed.
//
// Exits 0 if complete, 1 with the missing items printed otherwise.

import fs from 'fs';
import {execSync} from 'child_process';

const mode = process.argv[2];
const decisionsPath = process.argv[3];

if (!mode || !decisionsPath) {
  console.error('usage: check-completeness.mjs <context|review> <decisions.json>');
  process.exit(2);
}

const decisions = JSON.parse(fs.readFileSync(decisionsPath, 'utf8'));

function run(cmd) {
  return execSync(`node ${cmd}`, {encoding: 'utf8', cwd: process.cwd()});
}

if (mode === 'context') {
  const inputs = JSON.parse(run('.claude/skills/crowdin-merge/scripts/list-new-msgids.mjs'));
  const expected = new Set(inputs.map(x => x.msgid));
  const got = new Set(decisions.map(d => d.msgid));
  const missing = [...expected].filter(m => !got.has(m));
  if (missing.length) {
    console.error(`Missing context decisions for ${missing.length} msgids:`);
    for (const m of missing) console.error(`  ${JSON.stringify(m)}`);
    process.exit(1);
  }
  console.log(`OK: all ${expected.size} new msgids have a context decision.`);
} else if (mode === 'review') {
  const inputs = JSON.parse(run('.claude/skills/crowdin-merge/scripts/list-incoming-translations.mjs'));
  const missing = [];
  for (const [locale, items] of Object.entries(inputs)) {
    const reviewed = new Set((decisions[locale] || []).map(v => v.msgid));
    for (const item of items) {
      if (!reviewed.has(item.msgid)) missing.push(`${locale}: ${JSON.stringify(item.msgid)}`);
    }
  }
  if (missing.length) {
    console.error(`Missing review verdicts for ${missing.length} translations:`);
    for (const m of missing) console.error(`  ${m}`);
    process.exit(1);
  }
  const total = Object.values(inputs).reduce((n, arr) => n + arr.length, 0);
  console.log(`OK: all ${total} incoming translations have a review verdict.`);
} else {
  console.error(`unknown mode: ${mode}`);
  process.exit(2);
}
