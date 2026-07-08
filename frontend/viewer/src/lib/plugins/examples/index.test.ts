import {describe, expect, it} from 'vitest';
import {parsePluginContexts, parsePluginPermissions} from '../plugin-srcdoc';
import {examplePlugins, examplePluginsByPrimaryFunction} from './index';

describe('example plugin registry', () => {
  it('has a unique key, a non-empty description and at least one function per example', () => {
    const keys = new Set<string>();
    for (const example of examplePlugins) {
      expect(example.description.trim(), example.key).not.toBe('');
      expect(example.functions.length, `${example.key}: needs a function`).toBeGreaterThan(0);
      expect(keys.has(example.key), `duplicate key: ${example.key}`).toBe(false);
      keys.add(example.key);
    }
  });

  it('shows every example exactly once in the default (primary-function) grouping', () => {
    const grouped = examplePluginsByPrimaryFunction().flatMap((group) => group.plugins);
    expect(grouped).toHaveLength(examplePlugins.length);
  });

  // Badges are read from the registry before the HTML is loaded, so the registry must not lie about
  // what the plugin declares. `microphone`/`camera` can't be derived from stored HTML, so aren't checked.
  it('declares internet/entry-menu capabilities that agree with each example’s HTML meta', async () => {
    for (const example of examplePlugins) {
      const html = await example.loadHtml();
      const capabilities = new Set(example.capabilities ?? []);
      expect(parsePluginPermissions(html).includes('internet'), `${example.key}: internet`).toBe(capabilities.has('internet'));
      expect(parsePluginContexts(html).includes('entry'), `${example.key}: entry-menu`).toBe(capabilities.has('entry-menu'));
    }
  });
});
