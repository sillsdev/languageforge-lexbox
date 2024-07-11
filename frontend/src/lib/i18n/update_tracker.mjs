import * as fs from 'fs';
import * as path from 'path';
import json5 from 'json5';

function compileKeys(obj, prefix = '') {
  let keys = [];
  for (const key in obj) {
      const fullKey = prefix ? `${prefix}.${key}` : key;
      keys.push(fullKey);
      const subKey = obj[key];
      if (typeof subKey !== 'string') {
        const subKeys = compileKeys(subKey, fullKey);
        keys = keys.concat(subKeys);
      }
  }
  return keys;
}

function getDiffKeys(stdKeys, lang) {
    const langJson = json5.parse(fs.readFileSync(lang, 'utf-8'));
    const langKeysSet = new Set(compileKeys(langJson, ''));
    const diff = stdKeys.filter(x => !langKeysSet.has(x));
    return diff;
}

function main() {
  const localesPath = path.join(import.meta.dirname, 'locales');
  const files = fs.readdirSync(localesPath);
  const locales = files
      .filter(file => path.extname(file).toLowerCase() === '.json' && file !== 'en.json')
      .map(file => path.join(localesPath, file));

  const enJson = json5.parse(fs.readFileSync(path.join(localesPath, 'en.json'), 'utf-8'));
  const enAllKeys = compileKeys(enJson, '');

  const output = {};
  locales.forEach(lang => {
      const diff = getDiffKeys(enAllKeys, lang);
      output[lang] = diff;
  });
  console.log(output);
}

main();
