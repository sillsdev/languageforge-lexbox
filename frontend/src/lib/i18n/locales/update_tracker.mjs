import * as fs from 'fs';
import * as path from 'path';
import json5 from 'json5';

const dirname = import.meta.dirname;

const files = fs.readdirSync(dirname);
const locales = files
    .filter(file => path.extname(file).toLowerCase() === '.json')
    .map(file => path.basename(file, '.json'))
    .filter(file => file !== 'en');

const enJson = json5.parse(fs.readFileSync('en.json', 'utf-8'));
const enAllKeys = [];
compileKeys(enJson, '', enAllKeys);

function compileKeys(obj, prefix = '', keys = []) {
    for (const key in obj) {
        const fullKey = prefix ? `${prefix}.${key}` : key;
        keys.push(fullKey);
        const subKey = obj[key];
        if (typeof subKey !== 'string') {
            compileKeys(subKey, fullKey, keys);
        }
    }
}

function getDiffKeys(stdKeys, lang) {
    const langJson = json5.parse(fs.readFileSync(`${lang}.json`, 'utf-8'));
    const langKeys = [];
    compileKeys(langJson, '', langKeys);
    const diff = stdKeys.filter(x => !langKeys.includes(x));
    return diff;
}

const output = {};
locales.forEach(lang => {
    const diff = getDiffKeys(enAllKeys, lang);
    output[lang] = diff;
});
console.log(output);
