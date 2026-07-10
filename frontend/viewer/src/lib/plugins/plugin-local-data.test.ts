import {beforeEach, describe, expect, it} from 'vitest';
import {computePluginHash, PluginConsentStore, PluginStorage, PluginWriteTrustStore} from './plugin-local-data';

beforeEach(() => localStorage.clear());

describe('computePluginHash', () => {
  it('changes when the html, the name, or the description changes', async () => {
    const base = await computePluginHash({name: 'A', description: 'd', html: '<html></html>'});
    expect(await computePluginHash({name: 'A', description: 'd', html: '<html>x</html>'})).not.toBe(base);
    // A renamed plugin must re-consent: the name is the identity users recognize in dialogs.
    expect(await computePluginHash({name: 'B', description: 'd', html: '<html></html>'})).not.toBe(base);
    expect(await computePluginHash({name: 'A', description: 'e', html: '<html></html>'})).not.toBe(base);
    expect(await computePluginHash({name: 'A', description: 'd', html: '<html></html>'})).toBe(base);
  });
});

describe('hash-pinned grant stores', () => {
  it('grants apply only to the exact content hash', () => {
    const store = new PluginConsentStore('proj');
    store.grant('plugin-1', 'hash-a');
    expect(store.isGranted('plugin-1', 'hash-a')).toBe(true);
    expect(store.isGranted('plugin-1', 'hash-b')).toBe(false);
    expect(store.isGranted('plugin-2', 'hash-a')).toBe(false);
  });

  it('is scoped per project', () => {
    new PluginConsentStore('proj-1').grant('plugin-1', 'hash-a');
    expect(new PluginConsentStore('proj-2').isGranted('plugin-1', 'hash-a')).toBe(false);
  });

  it('revoke removes the grant', () => {
    const store = new PluginWriteTrustStore('proj');
    store.grant('plugin-1', 'hash-a');
    store.revoke('plugin-1');
    expect(store.isGranted('plugin-1', 'hash-a')).toBe(false);
    expect(store.grantedHash('plugin-1')).toBeUndefined();
  });

  it('run consent and write trust are independent stores', () => {
    new PluginConsentStore('proj').grant('plugin-1', 'hash-a');
    expect(new PluginWriteTrustStore('proj').isGranted('plugin-1', 'hash-a')).toBe(false);
  });
});

describe('PluginStorage', () => {
  it('round-trips values per project + plugin', () => {
    const storage = new PluginStorage('proj', 'plugin-1');
    storage.set('progress', {level: 3});
    expect(storage.get('progress')).toEqual({level: 3});
    expect(new PluginStorage('proj', 'plugin-2').get('progress')).toBeNull();
    storage.remove('progress');
    expect(storage.get('progress')).toBeNull();
  });

  it('treats __proto__ as plain data (no prototype pollution, nothing leaked back)', () => {
    const storage = new PluginStorage('proj', 'plugin-1');
    storage.set('__proto__', {polluted: true});
    expect(({} as Record<string, unknown>).polluted).toBeUndefined();
    expect(storage.get('safe-key')).toBeNull();
    expect(storage.get('__proto__')).toEqual({polluted: true});
  });

  it('rejects writes beyond the storage budget with storage-full', () => {
    const storage = new PluginStorage('proj', 'plugin-1');
    expect(() => storage.set('big', 'x'.repeat(300 * 1024))).toThrowError(/storage limit/i);
  });
});
