// The Dockerfile prunes devDependencies from node_modules and adapter-node only leaves "dependencies"
// external, so "dependencies" must be exactly what the server output imports: anything missing fails at
// request time, anything extra ships in the image for nothing (and drags in its peers). Run after `vite build`.
import {init, parse} from 'es-module-lexer';
import {readFileSync, readdirSync} from 'fs';
import {builtinModules} from 'module';
import {join} from 'path';

await init;
const {dependencies} = JSON.parse(readFileSync('package.json', 'utf8'));
const files = readdirSync('build', {recursive: true, withFileTypes: true})
  .filter(e => e.isFile() && e.name.endsWith('.js') && !e.parentPath.split(/[\\/]/).includes('client'))
  .map(e => join(e.parentPath, e.name));
const packageOf = spec => spec.split('/').slice(0, spec.startsWith('@') ? 2 : 1).join('/');

const imported = new Map();
for (const file of files) {
  const [imports] = parse(readFileSync(file, 'utf8'), file);
  for (const {n} of imports) {
    if (!n || n.startsWith('.') || n.startsWith('/') || n.startsWith('node:') || builtinModules.includes(n)) continue;
    imported.set(packageOf(n), file);
  }
}

const leaked = [...imported].filter(([pkg]) => !(pkg in dependencies));
for (const [pkg, file] of leaked) console.error(`${pkg} is imported at runtime by ${file} but is not in "dependencies"`);
const unused = Object.keys(dependencies).filter(pkg => !imported.has(pkg));
for (const pkg of unused) console.error(`${pkg} is in "dependencies" but the server never imports it; move it to devDependencies`);
if (leaked.length || unused.length) process.exit(1);
console.log(`${files.length} server files import exactly "dependencies"`);
