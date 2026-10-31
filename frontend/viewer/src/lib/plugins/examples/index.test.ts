import {describe, expect, it} from 'vitest';
import {parsePluginManifest} from '../plugin-manifest';
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
  // Loads all 27 example files; generous timeout for slow shared-CI runs.
  it('declares internet/entry-menu capabilities that agree with each example’s HTML meta', {timeout: 30_000}, async () => {
    for (const example of examplePlugins) {
      const manifest = parsePluginManifest(await example.loadHtml());
      const capabilities = new Set(example.capabilities ?? []);
      expect(manifest.permissions.includes('internet'), `${example.key}: internet`).toBe(capabilities.has('internet'));
      expect(manifest.contexts.includes('entry'), `${example.key}: entry-menu`).toBe(capabilities.has('entry-menu'));
    }
  });

  // The examples are the de-facto tutorial, so they must model correct manifest declarations.
  it('declares the edit permission exactly on the examples that write, and requires on the ones that need optional features', {timeout: 30_000}, async () => {
    for (const example of examplePlugins) {
      const html = await example.loadHtml();
      const manifest = parsePluginManifest(html);
      // Examples may alias window.fwlite, so match the (distinctive) method names directly.
      const writes = /\.\s*(createEntry|updateEntry|applyChanges|saveFile)\s*\(/.test(html);
      expect(manifest.permissions.includes('edit'), `${example.key}: edit permission`).toBe(writes);
      const usesComments = /\.\s*(getCommentThreads?|getUserComments|getUnreadComments|getUnreadCommentsForSubject|countUnreadComments)\s*\(/.test(html);
      const usesHistory = /\.\s*(getActivity|getEntityHistory|getChangeContext|getObjectAtCommit|listActivityAuthors|listActivityChangeTypes)\s*\(/.test(html);
      expect(manifest.requires.includes('comments'), `${example.key}: requires comments`).toBe(usesComments);
      expect(manifest.requires.includes('history'), `${example.key}: requires history`).toBe(usesHistory);
    }
  });
});
