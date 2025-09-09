import fs from 'node:fs'
import {getIconCollections} from '@egoist/tailwindcss-icons'

const collections = getIconCollections([
  'mdi',
]);

let result = 'export type IconClass =\n';

for (const collection of Object.values(collections)) {
  for (const name of Object.keys(collection.icons)) {
    result += `    | 'i-${collection.prefix}-${name}'\n`
  }
}

result += ';\n';

fs.writeFileSync('./src/lib/icon-class.ts', result)

console.log('icon-class.ts has been successfully generated.')
