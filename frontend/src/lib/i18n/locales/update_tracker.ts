// CL to run the script:
// pnpm exec tsc src/lib/i18n/locales/update_tracker.ts && mv update_tracker.js update_tracker.cjs && node update_tracker.cjs
// Adding --json argument will spit out a json
import { readFileSync } from 'fs'
import { parse } from 'json5'

const enJson = parse(readFileSync('en.json', 'utf-8')) as object;
const locales = ['fr', 'es'];
const enAllKeys: string[] = [];
compileKeys(enJson, '', enAllKeys);

// eslint-disable-next-line @typescript-eslint/explicit-function-return-type
function compileKeys(obj: object, prefix: string = '', keys: string[] = []) {
  for (const key in obj) {
    const fullKey = prefix ? `${prefix}.${key}` : key;
    keys.push(fullKey);
    if (typeof obj[key] === 'object') {
      compileKeys(obj[key], fullKey, keys);
    }
  }
}

function getDiffKeys(stdKeys: string[], lang: string): string[] {
  const langJson = parse(readFileSync(`${lang}.json`, 'utf-8')) as object;
  const langKeys: string[] = [];
  compileKeys(langJson, '', langKeys);
  const diff = stdKeys.filter(x => !langKeys.includes(x));
  return diff;
}

if (process.argv.includes('--json')) {
  locales.forEach(lang => {
    const diff = getDiffKeys(enAllKeys, lang);
    console.log(JSON.stringify(diff));
  });
} else {
  locales.forEach(lang => {
    const diff = getDiffKeys(enAllKeys, lang);
    console.log(diff);
  });
}
