// Command line to run the script:
// pnpm exec tsc src/lib/i18n/locales/update_tracker.ts && mv update_tracker.js update_tracker.cjs && node update_tracker.cjs
import * as fs from 'fs'
import * as path from 'path'
import { parse } from 'json5'

interface Langdef {
  [key: string]: string | Langdef
}

const files = fs.readdirSync(__dirname);
const locales = files
    .filter(file => path.extname(file).toLowerCase() === '.json')
    .map(file => path.basename(file, '.json'))
    .filter(file => file !== 'en');

const enJson: Langdef = parse(fs.readFileSync('en.json', 'utf-8'));
const enAllKeys: string[] = [];
compileKeys(enJson, '', enAllKeys);

function compileKeys(obj: Langdef, prefix: string = '', keys: string[] = []): void {
  for (const key in obj) {
    const fullKey = prefix ? `${prefix}.${key}` : key;
    keys.push(fullKey);
    const subKey = obj[key]
    if (typeof subKey !== 'string') {
      compileKeys(subKey, fullKey, keys);
    }
  }
}

function getDiffKeys(stdKeys: string[], lang: string): string[] {
  const langJson: Langdef = parse(fs.readFileSync(`${lang}.json`, 'utf-8'));
  const langKeys: string[] = [];
  compileKeys(langJson, '', langKeys);
  const diff = stdKeys.filter(x => !langKeys.includes(x));
  return diff;
}

const output: Record<string, string[]> = {};
locales.forEach(lang => {
  const diff = getDiffKeys(enAllKeys, lang);
  output[lang] = diff;
});

console.log(output);
