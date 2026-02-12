#!/usr/bin/env node
/**
 * Converts LingUI PO catalogs → platform.bible localizedStrings.json
 *
 * This script reads PO files produced by `lingui extract` (one per locale)
 * and generates the exact JSON format that platform.bible extensions expect
 * in their `contributions/localizedStrings.json`.
 *
 * Usage:  node scripts/po-to-platform-bible.mjs
 *
 * Input:  src/locales/{locale}.po   (from lingui extract)
 * Output: output/localizedStrings.json   (platform.bible format)
 */

import { readFileSync, writeFileSync, readdirSync, mkdirSync, existsSync } from 'fs';
import { join, basename } from 'path';

const LOCALES_DIR = join(import.meta.dirname, '..', 'src', 'locales');
const OUTPUT_DIR = join(import.meta.dirname, '..', 'output');

// ---------------------------------------------------------------------------
// Minimal PO parser — extracts msgid, msgstr, and translator comments
// ---------------------------------------------------------------------------

function parsePo(content) {
  const entries = {};
  let currentMsgid = null;
  let currentMsgstr = null;
  let currentComments = [];
  let inMsgid = false;
  let inMsgstr = false;

  function flush() {
    if (currentMsgid !== null && currentMsgid !== '') {
      entries[currentMsgid] = {
        translation: currentMsgstr || '',
        comments: currentComments.length > 0 ? currentComments.join('\n') : undefined,
      };
    }
    currentMsgid = null;
    currentMsgstr = null;
    currentComments = [];
    inMsgid = false;
    inMsgstr = false;
  }

  for (const line of content.split('\n')) {
    const trimmed = line.trim();

    // Translator comment
    if (trimmed.startsWith('#.')) {
      currentComments.push(trimmed.slice(2).trim());
      continue;
    }

    // Skip other comments and blank lines at entry boundaries
    if (trimmed.startsWith('#') && !trimmed.startsWith('#.')) {
      continue;
    }

    if (trimmed === '') {
      flush();
      continue;
    }

    if (trimmed.startsWith('msgid ')) {
      flush();
      currentMsgid = extractQuoted(trimmed.slice(6));
      inMsgid = true;
      inMsgstr = false;
      continue;
    }

    if (trimmed.startsWith('msgstr ')) {
      currentMsgstr = extractQuoted(trimmed.slice(7));
      inMsgstr = true;
      inMsgid = false;
      continue;
    }

    // Continuation line (quoted string on its own)
    if (trimmed.startsWith('"')) {
      const text = extractQuoted(trimmed);
      if (inMsgid) currentMsgid += text;
      if (inMsgstr) currentMsgstr += text;
    }
  }
  flush();

  return entries;
}

function extractQuoted(s) {
  const match = s.match(/^"(.*)"$/);
  return match
    ? match[1]
        .replace(/\\n/g, '\n')
        .replace(/\\"/g, '"')
        .replace(/\\\\/g, '\\')
    : s;
}

// ---------------------------------------------------------------------------
// Build localizedStrings.json from all locale PO files
// ---------------------------------------------------------------------------

function buildLocalizedStrings() {
  if (!existsSync(LOCALES_DIR)) {
    console.error(`Locales directory not found: ${LOCALES_DIR}`);
    console.error('Run "npm run extract" first to generate PO files.');
    process.exit(1);
  }

  const poFiles = readdirSync(LOCALES_DIR).filter((f) => f.endsWith('.po'));
  if (poFiles.length === 0) {
    console.error('No .po files found. Run "npm run extract" first.');
    process.exit(1);
  }

  const localizedStrings = {};
  const metadata = {};

  for (const filename of poFiles) {
    const locale = basename(filename, '.po');
    const content = readFileSync(join(LOCALES_DIR, filename), 'utf8');
    const entries = parsePo(content);

    const localeStrings = {};
    for (const [msgid, { translation, comments }] of Object.entries(entries)) {
      // Only include entries with platform.bible key pattern
      if (msgid.startsWith('%') && msgid.endsWith('%')) {
        localeStrings[msgid] = translation;

        // Add metadata from source locale comments
        if (locale === 'en' && comments) {
          metadata[msgid] = { notes: comments };
        }
      }
    }

    if (Object.keys(localeStrings).length > 0) {
      localizedStrings[locale] = localeStrings;
    }
  }

  const output = {
    metadata,
    localizedStrings,
  };

  if (!existsSync(OUTPUT_DIR)) {
    mkdirSync(OUTPUT_DIR, { recursive: true });
  }

  const outputPath = join(OUTPUT_DIR, 'localizedStrings.json');
  writeFileSync(outputPath, JSON.stringify(output, null, 2) + '\n');
  console.log(`\n✓ Generated ${outputPath}`);
  console.log(`  Locales: ${Object.keys(localizedStrings).join(', ')}`);
  console.log(`  Strings per locale: ${Object.keys(localizedStrings.en || {}).length}`);

  return output;
}

// ---------------------------------------------------------------------------
// Also generate the LOCALIZED_STRING_KEYS array (TypeScript)
// ---------------------------------------------------------------------------

function buildKeysList(localizedStrings) {
  const keys = Object.keys(localizedStrings.en || {}).sort();

  const tsContent = `import { LocalizeKey } from 'platform-bible-utils';

/**
 * Auto-generated from PO catalogs.
 * Do not edit manually — run \`npm run generate\` to regenerate.
 */
export const LOCALIZED_STRING_KEYS: LocalizeKey[] = [
${keys.map((k) => `  '${k}',`).join('\n')}
];
`;

  const outputPath = join(OUTPUT_DIR, 'localized-string-keys.ts');
  writeFileSync(outputPath, tsContent);
  console.log(`✓ Generated ${outputPath}`);
  console.log(`  Keys: ${keys.length}`);
}

// ---------------------------------------------------------------------------
// Run
// ---------------------------------------------------------------------------

const result = buildLocalizedStrings();
buildKeysList(result.localizedStrings);

// Print a diff summary comparing with the current localizedStrings.json
const currentPath = join(import.meta.dirname, '..', '..', 'contributions', 'localizedStrings.json');
if (existsSync(currentPath)) {
  const current = JSON.parse(readFileSync(currentPath, 'utf8'));
  const currentKeys = Object.keys(current.localizedStrings?.en || {}).sort();
  const newKeys = Object.keys(result.localizedStrings?.en || {}).sort();

  const added = newKeys.filter((k) => !currentKeys.includes(k));
  const removed = currentKeys.filter((k) => !newKeys.includes(k));
  const matched = newKeys.filter((k) => currentKeys.includes(k));

  console.log(`\n--- Comparison with current contributions/localizedStrings.json ---`);
  console.log(`  Matched keys: ${matched.length}`);
  if (added.length) console.log(`  Added:   ${added.join(', ')}`);
  if (removed.length) console.log(`  Missing: ${removed.join(', ')}`);

  // Verify values match for English
  let valueMismatches = 0;
  for (const key of matched) {
    const currentVal = current.localizedStrings.en[key];
    const newVal = result.localizedStrings.en[key];
    if (currentVal !== newVal) {
      console.log(`  Value differs for ${key}:`);
      console.log(`    current: "${currentVal}"`);
      console.log(`    new:     "${newVal}"`);
      valueMismatches++;
    }
  }
  if (valueMismatches === 0) {
    console.log(`  ✓ All matched English values are identical`);
  }
}
