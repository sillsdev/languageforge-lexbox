const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');
const { execSync, spawnSync } = require('node:child_process');
const readline = require('node:readline');

// ---------------------------------------------------------------------------
// Paths
// ---------------------------------------------------------------------------

const extensionRoot = path.resolve(__dirname, '..');
const templateDir = path.resolve(extensionRoot, '..', '..', 'paranext-extension-template');
const monorepoRoot = path.resolve(extensionRoot, '..');

// ---------------------------------------------------------------------------
// ANSI colours
// ---------------------------------------------------------------------------

const RESET = '\x1b[0m';
const BOLD = '\x1b[1m';
const DIM = '\x1b[2m';
const GREEN = '\x1b[32m';
const YELLOW = '\x1b[33m';
const RED = '\x1b[31m';
const ORANGE = '\x1b[38;5;208m';

/** Wrap text in an ANSI colour/style code and reset. */
function c(code, text) {
  return `${code}${text}${RESET}`;
}

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

/** Return true if the buffer looks binary (NUL byte in first 8 KB). */
function isBinary(buf) {
  return buf.slice(0, 8192).includes(0);
}

/** Print a coloured git diff between the template file and the extension file. */
function showDiff(templateFile, extensionFile) {
  if (!fs.existsSync(extensionFile)) {
    const tempFile = path.join(os.tmpdir(), `sync-template-empty-${process.pid}`);
    fs.writeFileSync(tempFile, '');
    spawnSync('git', ['diff', '--no-index', '--color', '--', templateFile, tempFile], {
      stdio: 'inherit',
    });
    fs.unlinkSync(tempFile);
  } else {
    spawnSync('git', ['diff', '--no-index', '--color', '--', templateFile, extensionFile], {
      stdio: 'inherit',
    });
  }
}

/** Prompt the user and resolve with the trimmed, lowercased response. */
function ask(rl, question) {
  return new Promise((resolve) => {
    rl.question(question, (ans) => {
      resolve(ans.trim().toLowerCase());
    });
  });
}

// ---------------------------------------------------------------------------
// Build file pairs — enumerate via git ls-files (respects .gitignore)
// ---------------------------------------------------------------------------

/**
 * Top-level directories to skip. Everything gitignored is already excluded by `git ls-files`; this
 * list only covers extension-specific content that isn't gitignored.
 */
const IGNORED_DIRS = new Set([
  '.github', // CI workflow is added explicitly below
  'lib', // used for template CI, but deleted from this extension
]);

/** File basenames to skip — extension-specific, not gitignored. */
const IGNORED_FILES = new Set(['LICENSE', 'CLAUDE.md', 'AGENTS.md', 'package-lock.json']);

/** Exact relative paths (forward-slash) to skip. */
const IGNORED_PATHS = new Set(['src/main.ts']);

/** Relative path prefixes to skip — matches any file under these paths. */
const IGNORED_PATH_PREFIXES = ['src/types/'];

/**
 * Files modified in the extension that still need template updates reviewed. [t] is blocked so
 * changes are never applied without a review pass; [b] is available to apply-then-defer. Keys are
 * forward-slash paths as output by `git ls-files`.
 */
const REVIEW_REQUIRED_NOTE = 'Apply via [b]oth then review — extension has custom modifications';
// prettier-ignore
const REVIEW_REQUIRED_FILES = new Set([
  '.eslintignore',
  '.eslintrc.js',
  '.gitignore',
  '.prettierignore',
  '.stylelintignore',
  'manifest.json',
  'package.json',
  'README.md',
  'tsconfig.json',
  'tsconfig.lint.json',
  'webpack/webpack.config.main.ts',
]);

/** Top-level directories where all files are surfaced but overwrite is blocked. */
const MANUAL_DIRS = {
  assets: { note: 'Adapt changes manually — contributions are extension-specific' },
  contributions: { note: 'Adapt changes manually — contributions are extension-specific' },
};

/** Create the destination directory if needed, then overwrite the extension file with the template. */
function copyFromTemplate(pair) {
  fs.mkdirSync(path.dirname(pair.extensionFile), { recursive: true });
  fs.copyFileSync(pair.templateFile, pair.extensionFile);
}

/** Enumerate template files via git ls-files and return a pair object for each. */
function buildPairs() {
  // git ls-files lists every tracked file, automatically honouring .gitignore
  const templateFiles = execSync('git ls-files', { cwd: templateDir, encoding: 'utf8' })
    .trim()
    .split('\n')
    .filter(Boolean);

  const pairs = templateFiles
    .filter(
      (rel) =>
        !IGNORED_DIRS.has(rel.split('/')[0]) &&
        !IGNORED_FILES.has(rel.split('/').pop()) &&
        !IGNORED_PATHS.has(rel) &&
        !IGNORED_PATH_PREFIXES.some((prefix) => rel.startsWith(prefix)),
    )
    .map((rel) => {
      const reviewRequired = REVIEW_REQUIRED_FILES.has(rel);
      const manualDir = MANUAL_DIRS[rel.split('/')[0]];
      return {
        label: rel,
        templateFile: path.join(templateDir, rel),
        extensionFile: path.join(extensionRoot, rel),
        note: reviewRequired ? REVIEW_REQUIRED_NOTE : manualDir?.note,
        canOverwrite: !reviewRequired && !manualDir,
        deferRequired: reviewRequired,
      };
    });

  // CI workflow: explicitly added because the filename differs on each side
  pairs.push({
    label: 'CI workflow (lint.yml → platform.bible-extension.yaml)',
    templateFile: path.join(templateDir, '.github', 'workflows', 'lint.yml'),
    extensionFile: path.join(monorepoRoot, '.github', 'workflows', 'platform.bible-extension.yaml'),
    note: 'Adapt changes manually — filenames differ and content has monorepo-specific structure',
    canOverwrite: false,
  });

  return pairs;
}

// ---------------------------------------------------------------------------
// Per-pair prompt
// ---------------------------------------------------------------------------

/** Show the diff for a pair and prompt the user to choose how to handle it. */
async function promptChoice(rl, pair) {
  const { templateFile, extensionFile, label, note, canOverwrite, deferRequired } = pair;
  const canApplyDirect = canOverwrite;
  const canApplyWithDefer = canOverwrite || deferRequired;

  if (!fs.existsSync(templateFile)) return 'missing';

  if (fs.existsSync(extensionFile)) {
    const tBuf = fs.readFileSync(templateFile);
    const eBuf = fs.readFileSync(extensionFile);
    if (tBuf.equals(eBuf)) return 'same';
  }

  console.log(`\n${'─'.repeat(72)}`);
  console.log(`${c(BOLD, label)}${note ? `  ${c(DIM, `[${note}]`)}` : ''}`);
  console.log('─'.repeat(72));
  const templateBuf = fs.readFileSync(templateFile);
  if (isBinary(templateBuf)) {
    console.log(c(DIM, '(binary file — diff not shown)'));
  } else {
    showDiff(templateFile, extensionFile);
  }

  const templateOptions = canApplyDirect
    ? `${c(RED, '[t]emplate')}  `
    : `${c(DIM, '[t]emplate (blocked)')}  `;

  const bothOption = canApplyWithDefer
    ? `${c(ORANGE, '[b]oth')} (template+defer)  `
    : `${c(DIM, '[b]oth (blocked)')}  `;

  const prompt = `\n  ${c(GREEN, '[e]xtension')}  ${templateOptions}${c(YELLOW, '[d]efer')} (for review after manual editing)  ${bothOption}> `;

  let choice = '';
  while (!['e', 't', 'b', 'd'].includes(choice)) {
    // eslint-disable-next-line no-await-in-loop
    choice = await ask(rl, prompt);
    if (choice === 't' && !canApplyDirect) {
      console.log(
        c(
          RED,
          '  Blocked — use [b]oth to apply and defer for review, or [d]efer to edit manually.',
        ),
      );
      choice = '';
    } else if (choice === 'b' && !canApplyWithDefer) {
      console.log(c(RED, '  Blocked — adapt manually, then [d]efer or [e]xtension.'));
      choice = '';
    }
  }
  return choice;
}

// ---------------------------------------------------------------------------
// Main
// ---------------------------------------------------------------------------

/** Entry point: pull the template, walk all pairs interactively, and print a summary. */
async function main() {
  if (!fs.existsSync(templateDir)) {
    throw new Error(`Template directory not found: ${templateDir}`);
  }

  console.log(c(BOLD, '\n━━━  Sync template updates  ━━━'));
  console.log(c(DIM, `  Template : ${templateDir}`));
  console.log(c(DIM, `  Extension: ${extensionRoot}\n`));

  console.log('Pulling latest template...');
  execSync('git pull --ff-only', { cwd: templateDir, stdio: 'inherit' });
  console.log();

  const allPairs = buildPairs();

  const rl = readline.createInterface({
    input: process.stdin,
    output: process.stdout,
    terminal: true,
  });
  rl.on('SIGINT', () => {
    console.log(c(RED, '\n\nInterrupted.'));
    rl.close();
    process.exit(1);
  });

  let countSame = 0;
  let countTemplate = 0;
  let countExtension = 0;

  let queue = [...allPairs];
  let deferred = [];

  while (queue.length > 0) {
    let i = 0;
    while (i < queue.length) {
      const pair = queue[i];
      i += 1;
      // eslint-disable-next-line no-await-in-loop
      const choice = await promptChoice(rl, pair);

      if (choice === 'missing') {
        console.log(c(DIM, `  (missing in template — skipping)  ${pair.label}`));
        countExtension += 1;
      } else if (choice === 'same') {
        console.log(`  ${c(DIM, 'same')}    ${pair.label}`);
        countSame += 1;
      } else if (choice === 'e') {
        countExtension += 1;
      } else if (choice === 't') {
        copyFromTemplate(pair);
        console.log(c(GREEN, '  Applied template'));
        countTemplate += 1;
      } else if (choice === 'b') {
        copyFromTemplate(pair);
        console.log(c(GREEN, '  Applied template + deferred'));
        countTemplate += 1;
        deferred.push(pair);
      } else {
        console.log(c(YELLOW, '  Deferred'));
        deferred.push(pair);
      }
    }

    queue = deferred;
    deferred = [];

    if (queue.length > 0) {
      console.log(
        `\n${c(BOLD, '━━━  Deferred files ━━━')}${c(DIM, ' — edit them now, then press Enter to continue')}`,
      );
      // eslint-disable-next-line no-await-in-loop
      await ask(rl, '  Press Enter to continue... ');
    }
  }

  rl.close();

  console.log(`\n${'━'.repeat(72)}`);
  console.log(c(BOLD, 'Summary'));
  console.log('━'.repeat(72));
  console.log(`  ${c(DIM, 'Identical:        ')} ${countSame}`);
  console.log(`  ${c(GREEN, 'Template applied: ')} ${countTemplate}`);
  console.log(`  ${c(DIM, 'Kept extension:   ')} ${countExtension}`);
  console.log('');
}

main().catch((err) => {
  console.error(c(RED, '\nFatal:'), err);
  process.exit(1);
});
