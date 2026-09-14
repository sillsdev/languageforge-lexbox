import {expect, test} from 'vitest';

import {entityConfig, helpDocRoot} from './entity-config';

// Opt-in check that every helpId in the entity config points at a real FieldWorks topic page.
// Run with `pnpm test:manual`; it hits the network, so it's excluded from `pnpm test` and CI.
//
// Note we deliberately do NOT check `helpBaseUrl` (`index.htm#t=<helpId>`). That URL is resolved
// client-side by RoboHelp, so the fragment never reaches the server and every request would come
// back 200 regardless of whether the topic exists. The topic pages under `helpDocRoot` 404 properly.

function helpIds(): {path: string; helpId: string}[] {
  return Object.entries(entityConfig).flatMap(([entityName, entity]) =>
    Object.entries(entity)
      .map(([fieldName, fieldData]) => ({
        path: `${entityName}.${fieldName}`,
        helpId: (fieldData as {helpId?: string}).helpId,
      }))
      .filter((field): field is {path: string; helpId: string} => !!field.helpId));
}

test('all help links resolve', {timeout: 120_000}, async () => {
  const results = await Promise.all(helpIds().map(async ({path, helpId}) => {
    const url = `${helpDocRoot}${helpId}`;
    const response = await fetch(url);
    return {path, url, status: response.status, ok: response.ok};
  }));

  expect(results).not.toHaveLength(0);
  const broken = results.filter(result => !result.ok);
  expect(broken, `broken help links:\n${broken.map(b => `  ${b.path}: ${b.status} ${b.url}`).join('\n')}`)
    .toEqual([]);
});
