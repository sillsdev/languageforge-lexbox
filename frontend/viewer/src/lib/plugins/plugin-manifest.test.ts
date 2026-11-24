import {describe, expect, it} from 'vitest';
import {compareManifests, manifestFromStored, parsePluginManifest} from './plugin-manifest';

function html(head: string): string {
  return `<html><head>${head}</head><body></body></html>`;
}

describe('parsePluginManifest', () => {
  it('reads declared permissions, contexts, and requirements', () => {
    const manifest = parsePluginManifest(html(
      '<meta name="fwlite-plugin-permissions" content="internet edit">' +
      '<meta name="fwlite-plugin-contexts" content="entry">' +
      '<meta name="fwlite-plugin-requires" content="comments history">'));
    expect(manifest.permissions).toEqual(['internet', 'edit']);
    expect(manifest.contexts).toEqual(['entry']);
    expect(manifest.requires).toEqual(['comments', 'history']);
  });

  it('accepts comma-separated tokens', () => {
    expect(parsePluginManifest(html('<meta name="fwlite-plugin-permissions" content="internet,edit">')).permissions)
      .toEqual(['internet', 'edit']);
  });

  it('ignores unknown tokens and missing metas', () => {
    const manifest = parsePluginManifest(html(
      '<meta name="fwlite-plugin-permissions" content="teleport internet">' +
      '<meta name="fwlite-plugin-contexts" content="entry galaxy">'));
    expect(manifest.permissions).toEqual(['internet']);
    expect(manifest.contexts).toEqual(['entry']);
    expect(manifest.requires).toEqual([]);
    expect(parsePluginManifest(html(''))).toEqual({permissions: [], contexts: [], requires: []});
  });
});

describe('manifestFromStored', () => {
  it('drops tokens this client does not know (written by a newer client)', () => {
    const manifest = manifestFromStored({
      permissions: ['edit', 'time-travel'],
      contexts: ['entry', 'sense'],
      requires: ['history', 'blockchain'],
    });
    expect(manifest.permissions).toEqual(['edit']);
    expect(manifest.contexts).toEqual(['entry']);
    expect(manifest.requires).toEqual(['history']);
  });
});

describe('compareManifests', () => {
  it('flags what a new version adds and drops', () => {
    const before = parsePluginManifest(html('<meta name="fwlite-plugin-permissions" content="edit"><meta name="fwlite-plugin-contexts" content="entry">'));
    const after = parsePluginManifest(html('<meta name="fwlite-plugin-permissions" content="edit internet"><meta name="fwlite-plugin-requires" content="history">'));
    expect(compareManifests(before, after)).toEqual({
      added: {permissions: ['internet'], contexts: [], requires: ['history']},
      removed: {permissions: [], contexts: ['entry'], requires: []},
    });
  });

  it('reports nothing for the same declarations', () => {
    const manifest = parsePluginManifest(html('<meta name="fwlite-plugin-permissions" content="edit">'));
    const none = {permissions: [], contexts: [], requires: []};
    expect(compareManifests(manifest, manifest)).toEqual({added: none, removed: none});
  });
});
