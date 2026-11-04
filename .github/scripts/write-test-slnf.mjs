// Writes FwLiteAffectedTests.slnf at the repo root: the FW Lite test projects named in $TEST_PROJECTS
// (a JSON array of project names from pr.yaml's detect job), taken from FwLiteCore.slnf so the paths
// and the solution reference stay in one place. `dotnet test FwLiteAffectedTests.slnf --no-build`
// then runs exactly those projects, concurrently, after a whole-FwLiteCore.slnf build.
import {readFileSync, writeFileSync} from 'node:fs';

const wanted = new Set(JSON.parse(process.env.TEST_PROJECTS ?? '[]'));
const core = JSON.parse(readFileSync('FwLiteCore.slnf', 'utf8'));
const name = (p) => p.split('\\').pop().replace(/\.csproj$/, '');
const projects = core.solution.projects.filter((p) => wanted.has(name(p)));
const missing = [...wanted].filter((w) => !projects.some((p) => name(p) === w));
if (missing.length) throw new Error(`not in FwLiteCore.slnf: ${missing.join(', ')}`);
if (projects.length === 0) throw new Error('TEST_PROJECTS selected no test project');

writeFileSync('FwLiteAffectedTests.slnf', JSON.stringify({solution: {path: core.solution.path, projects}}, null, 2) + '\n');
console.log(`FwLiteAffectedTests.slnf: ${projects.map(name).join(', ')}`);
