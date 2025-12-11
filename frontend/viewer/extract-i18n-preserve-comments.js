import { readFileSync, writeFileSync, copyFileSync, readdirSync, unlinkSync } from 'fs';
import { execSync } from 'child_process';

const baseLocalesDir = 'src/locales';

// Determine locales based on .po files in the directory
const locales = readdirSync(baseLocalesDir)
  .filter(file => file.endsWith('.po'))
  .map(file => file.replace('.po', ''))
  .sort();

// Parse the original po file and build a map of msgid -> comments
function parsePo(content) {
  const entries = [];
  let currentEntry = {};
  const lines = content.split('\n');
  
  for (let i = 0; i < lines.length; i++) {
    const line = lines[i];
    
    // Blank line = end of entry
    if (line.trim() === '') {
      if (currentEntry.msgid) {
        entries.push(currentEntry);
      }
      currentEntry = {};
      continue;
    }
    
    // Collect developer comments
    if (line.startsWith('#.')) {
      if (!currentEntry.comments) currentEntry.comments = [];
      currentEntry.comments.push(line);
    }
    // Extract msgid
    else if (line.startsWith('msgid "')) {
      const match = line.match(/^msgid "(.*)"/);
      if (match) {
        currentEntry.msgid = match[1];
      }
    }
  }
  
  return entries;
}

// Backup the original locale files before extraction
const backups = {};
for (const locale of locales) {
  const poPath = `${baseLocalesDir}/${locale}.po`;
  const backupPath = `${baseLocalesDir}/${locale}.po.bak`;
  copyFileSync(poPath, backupPath);
  backups[locale] = backupPath;
}

// Build a map of msgid -> comments from the en.po (source language)
const enBackupPath = backups['en'];
const originalContent = readFileSync(enBackupPath, 'utf8');
const originalEntries = parsePo(originalContent);
const msgidToComments = new Map();

for (const entry of originalEntries) {
  if (entry.msgid && entry.comments && entry.comments.length > 0) {
    msgidToComments.set(entry.msgid, entry.comments);
  }
}

console.log(`Saved ${msgidToComments.size} msgids with comments before extraction`);

// Run extraction
console.log('Running lingui extract...');
execSync('lingui extract --clean', { stdio: 'inherit' });

// Function to restore comments in a locale file
function restoreCommentsInFile(poPath) {
  const newContent = readFileSync(poPath, 'utf8');
  const lines = newContent.split('\n');
  const result = [];
  
  const addedCommentsFor = new Set(); // Track which msgids we've already added comments for
  
  for (let i = 0; i < lines.length; i++) {
    const line = lines[i];
    
    // If we find a source location comment (#:), check if we need to add developer comments before it
    if (line.startsWith('#:')) {
      // Look ahead to find the msgid
      let msgid = null;
      for (let j = i + 1; j < lines.length && j < i + 10; j++) {
        if (lines[j].startsWith('msgid "')) {
          const match = lines[j].match(/^msgid "(.*)"/);
          if (match) {
            msgid = match[1];
          }
          break;
        }
      }
      
      // If we have comments for this msgid and haven't added them yet, add them before the #: line
      if (msgid && msgidToComments.has(msgid) && !addedCommentsFor.has(msgid)) {
        // Check if comments already exist right before this #: line
        let hasComments = false;
        for (let j = i - 1; j >= 0 && j >= i - 10; j--) {
          if (lines[j].startsWith('#.')) {
            hasComments = true;
            break;
          }
          if (lines[j].trim() === '') break; // Stop at blank line
        }
        
        // Add comments if they're not already there
        if (!hasComments) {
          const comments = msgidToComments.get(msgid);
          for (const comment of comments) {
            result.push(comment);
          }
          addedCommentsFor.add(msgid); // Mark that we've added comments for this msgid
        }
      }
    }
    
    result.push(line);
  }
  
  let updatedContent = result.join('\n');
  
  // Reorder comments so developer comments (#.) come before source location comments (#:)
  const finalLines = updatedContent.split('\n');
  const finalResult = [];
  let i = 0;
  while (i < finalLines.length) {
    const line = finalLines[i];
    
    // Collect all comments before msgid
    if (line.startsWith('#')) {
      const comments = [];
      const devComments = [];
      const srcComments = [];
      
      while (i < finalLines.length && finalLines[i].startsWith('#')) {
        if (finalLines[i].startsWith('#.')) {
          devComments.push(finalLines[i]);
        } else if (finalLines[i].startsWith('#:')) {
          srcComments.push(finalLines[i]);
        } else {
          comments.push(finalLines[i]);
        }
        i++;
      }
      
      // Add in order: dev comments first, then other comments, then source comments
      finalResult.push(...devComments);
      finalResult.push(...comments);
      finalResult.push(...srcComments);
      
      // Don't increment i or add the line again; we're already positioned for the next iteration
      continue;
    }
    
    finalResult.push(line);
    i++;
  }
  
  updatedContent = finalResult.join('\n');
  writeFileSync(poPath, updatedContent);
}

// Restore comments in all locale files
for (const locale of locales) {
  const poPath = `${baseLocalesDir}/${locale}.po`;
  restoreCommentsInFile(poPath);
}

// Clean up backup files
for (const locale of locales) {
  const backupPath = `${baseLocalesDir}/${locale}.po.bak`;
  unlinkSync(backupPath);
}

console.log(`Comments restored for ${msgidToComments.size} entries across ${locales.length} locale files`);
