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
const CYAN = '\x1b[36m';
const GREEN = '\x1b[32m';
const YELLOW = '\x1b[33m';
const RED = '\x1b[31m';

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

function showDiff(templateFile, extensionFile) {
  if (!fs.existsSync(extensionFile)) {
    const tempFile = path.join(os.tmpdir(), `sync-template-empty-${process.pid}`);
    fs.writeFileSync(tempFile, '');
    spawnSync('git', ['diff', '--no-index', '--color', '--', tempFile, templateFile], {
      stdio: 'inherit',
    });
    fs.unlinkSync(tempFile);
  } else {
    spawnSync('git', ['diff', '--no-index', '--color', '--', templateFile, extensionFile], {
      stdio: 'inherit',
    });
  }
}

function ask(rl, question) {
  return new Promise((resolve) => {
    rl.question(question, (ans) => {
      resolve(ans.trim().toLowerCase());
    });
  });
}

// ---------------------------------------------------------------------------
// Build file pairs
// ---------------------------------------------------------------------------

function buildPairs() {
  const staticRels = [
    'tsconfig.json',
    'tsconfig.lint.json',
    'postcss.config.ts',
    'tailwind.config.ts',
    '.eslintrc.js',
    '.eslintignore',
    '.stylelintrc.js',
    '.stylelintignore',
    '.prettierrc.js',
    '.prettierignore',
    '.editorconfig',
    '.gitattributes',
    'cspell.json',
    'webpack.config.ts',
  ];

  const pairs = staticRels.map((rel) => ({
    label: rel,
    templateFile: path.join(templateDir, rel),
    extensionFile: path.join(extensionRoot, rel),
    canOverwrite: true,
  }));

  pairs.push({
    label: 'package.json',
    templateFile: path.join(templateDir, 'package.json'),
    extensionFile: path.join(extensionRoot, 'package.json'),
    note: 'Review scripts & devDeps only — restoring name/version/description/dependencies will be needed after overwrite',
    canOverwrite: true,
  });

  // Dynamic webpack entries
  const webpackTemplateDir = path.join(templateDir, 'webpack');
  if (fs.existsSync(webpackTemplateDir)) {
    fs.readdirSync(webpackTemplateDir).forEach((filename) => {
      pairs.push({
        label: `webpack/${filename}`,
        templateFile: path.join(webpackTemplateDir, filename),
        extensionFile: path.join(extensionRoot, 'webpack', filename),
        canOverwrite: true,
      });
    });
  }

  // CI workflow — canOverwrite: false, filenames differ, content has monorepo-specific structure
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

async function promptChoice(rl, pair) {
  const { templateFile, extensionFile, label, note, canOverwrite } = pair;

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

  const templateOptions = canOverwrite
    ? `${c(GREEN, '[t]emplate')}  `
    : `${c(DIM, '[t]emplate (blocked)')}  `;

  const bothOption = canOverwrite
    ? `${c(CYAN, '[b]oth')} (template+defer)  `
    : `${c(DIM, '[b]oth (blocked)')}  `;

  const prompt = `\n  ${c(DIM, '[e]xtension')}  ${templateOptions}${c(YELLOW, '[d]efer')} (for review after manual editing)  ${bothOption}> `;

  let choice = '';
  while (!['e', 't', 'b', 'd'].includes(choice)) {
    // eslint-disable-next-line no-await-in-loop
    choice = await ask(rl, prompt);
    if ((choice === 't' || choice === 'b') && !canOverwrite) {
      console.log(c(RED, '  Blocked — adapt manually, then [d]efer or [e]xtension.'));
      choice = '';
    }
  }
  return choice;
}

// ---------------------------------------------------------------------------
// Main
// ---------------------------------------------------------------------------

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
        fs.copyFileSync(pair.templateFile, pair.extensionFile);
        console.log(c(GREEN, '  Applied template'));
        countTemplate += 1;
      } else if (choice === 'b') {
        fs.copyFileSync(pair.templateFile, pair.extensionFile);
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
