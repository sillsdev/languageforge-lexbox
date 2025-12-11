#!/usr/bin/env node

/**
 * review-po.js - Review PO file translations in batches with progress tracking
 * 
 * Usage:
 *   node review-po.js es 20         # Review Spanish, 20 entries per batch
 *   node review-po.js fr 15         # Review French, 15 entries per batch
 *   node review-po.js de            # Review German, default 20 entries per batch
 * 
 * Language is required. Batch size defaults to 20 if not specified.
 */

import fs from 'fs';
import path from 'path';

function main() {
  const language = process.argv[2];
  const batchSize = parseInt(process.argv[3]) || 20;

  // Language is required
  if (!language) {
    console.error('❌ Error: Language code is required');
    console.error('Usage: node review-po.js <language> [batchSize]');
    console.error('Example: node review-po.js es 20');
    process.exit(1);
  }

  const poFile = path.join('src/locales', `${language}.po`);
  const progressFile = `${poFile}.progress`;

  // Validate file exists
  if (!fs.existsSync(poFile)) {
    console.error(`❌ Error: File not found: ${poFile}`);
    console.error(`Ensure you have a translation file at: ${poFile}`);
    process.exit(1);
  }

  // Initialize progress if needed
  if (!fs.existsSync(progressFile)) {
    fs.writeFileSync(progressFile, '0\n');
  }

  const startEntryIndex = parseInt(fs.readFileSync(progressFile, 'utf-8').trim());

  // Read all lines
  const allLines = fs.readFileSync(poFile, 'utf-8').split('\n');
  const totalLines = allLines.length;

  // Parse entries: collect lines between blank lines, each entry contains one msgid/msgstr pair
  const entries = [];
  let currentEntry = [];

  for (let i = 0; i < allLines.length; i++) {
    const line = allLines[i];
    
    // Blank lines separate entries
    if (line.trim() === '') {
      // If we have content and it has a msgid, save the entry
      if (currentEntry.length > 0 && currentEntry.some(l => l.startsWith('msgid'))) {
        entries.push(currentEntry);
      }
      currentEntry = [];
    } else {
      currentEntry.push(line);
    }
  }
  
  // Add final entry if it has msgid
  if (currentEntry.length > 0 && currentEntry.some(l => l.startsWith('msgid'))) {
    entries.push(currentEntry);
  }

  // Skip the first entry (PO file metadata header) - it's not a translation to review
  const reviewableEntries = entries.slice(1);
  const totalEntries = reviewableEntries.length;

  // Check if review is complete
  if (startEntryIndex >= totalEntries) {
    console.log();
    console.log('✅ REVIEW COMPLETE!');
    console.log(`All ${totalEntries} strings in ${poFile} have been reviewed.`);
    fs.unlinkSync(progressFile);
    process.exit(0);
  }

  // Extract next batch of entries
  const endEntryIndex = Math.min(startEntryIndex + batchSize, totalEntries);
  const batchEntries = reviewableEntries.slice(startEntryIndex, endEntryIndex);

  // Show batch header
  console.log('='.repeat(80));
  const batchNum = Math.floor(startEntryIndex / batchSize) + 1;
  const entryRange = `${startEntryIndex + 1} to ${endEntryIndex}`;
  console.log(`BATCH ${batchNum}`);
  console.log(`Entries ${entryRange} of ${totalEntries}`);
  console.log('='.repeat(80));
  console.log();

  // Display the batch
  for (let i = 0; i < batchEntries.length; i++) {
    const entry = batchEntries[i];
    for (const line of entry) {
      console.log(line);
    }
    // Add blank line between entries (except after the last one)
    if (i < batchEntries.length - 1) {
      console.log();
    }
  }

  console.log();
  console.log('='.repeat(80));
  const progressPercent = Math.round((endEntryIndex * 100) / totalEntries);
  console.log(`Progress: ${progressPercent}% complete`);
  console.log('='.repeat(80));
  console.log();
  console.log('After reviewing this batch, run:');
  console.log(`  node review-po.js ${language} ${batchSize}`);
  console.log();

  // Update progress file for next run
  fs.writeFileSync(progressFile, `${endEntryIndex}\n`);
}

main();
