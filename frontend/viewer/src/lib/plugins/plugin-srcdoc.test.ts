import {describe, expect, it} from 'vitest';
import {buildPluginSrcdoc, parsePluginContexts, parsePluginPermissions} from './plugin-srcdoc';

function parse(html: string): Document {
  return new DOMParser().parseFromString(html, 'text/html');
}

function cspMeta(doc: Document): Element | null {
  return doc.head.querySelector('meta[http-equiv="Content-Security-Policy"]');
}

function sdkScript(doc: Document): Element | undefined {
  return [...doc.head.querySelectorAll('script')].find(script => script.textContent?.includes('fwlite'));
}

describe('buildPluginSrcdoc', () => {
  it('injects the CSP before the SDK at the top of the head', () => {
    const doc = parse(buildPluginSrcdoc('<!DOCTYPE html><html><head><title>x</title></head><body>hi</body></html>', []));
    expect(doc.head.firstElementChild).toBe(cspMeta(doc));
    expect(sdkScript(doc)).toBeDefined();
    expect(doc.title).toBe('x');
    expect(doc.body.textContent).toBe('hi');
  });

  it('omits the CSP when the internet permission is granted', () => {
    const doc = parse(buildPluginSrcdoc('<html><head></head><body></body></html>', ['internet']));
    expect(cspMeta(doc)).toBeNull();
    expect(sdkScript(doc)).toBeDefined();
  });

  it('handles bare fragments without html/head tags', () => {
    const doc = parse(buildPluginSrcdoc('<h1>minimal</h1>', []));
    expect(doc.head.firstElementChild).toBe(cspMeta(doc));
    expect(sdkScript(doc)).toBeDefined();
    expect(doc.body.querySelector('h1')?.textContent).toBe('minimal');
  });

  // A string-splicing implementation can be tricked into injecting the CSP/SDK into inert
  // positions; these documents must still end up guarded by a real CSP meta in the head.
  it('is not evaded by a fake <head> inside an attribute value', () => {
    const doc = parse(buildPluginSrcdoc('<html><head data-x="<head>"><script>window.x = 1;</script></head><body></body></html>', []));
    expect(doc.head.firstElementChild).toBe(cspMeta(doc));
    expect(sdkScript(doc)).toBeDefined();
  });

  it('is not evaded by a fake <head> inside an HTML comment', () => {
    const doc = parse(buildPluginSrcdoc('<!-- <head> --><html><head><script>window.x = 1;</script></head><body></body></html>', []));
    expect(doc.head.firstElementChild).toBe(cspMeta(doc));
    expect(sdkScript(doc)).toBeDefined();
  });

  it('keeps the doctype when present and omits it when absent', () => {
    expect(buildPluginSrcdoc('<!DOCTYPE html><html></html>', []).startsWith('<!DOCTYPE html>')).toBe(true);
    expect(buildPluginSrcdoc('<html></html>', []).startsWith('<html')).toBe(true);
  });
});

describe('parsePluginPermissions', () => {
  it('reads declared permissions from the meta tag', () => {
    expect(parsePluginPermissions('<html><head><meta name="fwlite-plugin-permissions" content="internet"></head></html>'))
      .toEqual(['internet']);
  });

  it('ignores unknown permissions and missing meta tags', () => {
    expect(parsePluginPermissions('<html><head><meta name="fwlite-plugin-permissions" content="teleport internet"></head></html>'))
      .toEqual(['internet']);
    expect(parsePluginPermissions('<html><head></head></html>')).toEqual([]);
  });
});

describe('parsePluginContexts', () => {
  it('reads declared contexts from the meta tag', () => {
    expect(parsePluginContexts('<html><head><meta name="fwlite-plugin-contexts" content="entry"></head></html>'))
      .toEqual(['entry']);
  });

  it('ignores unknown contexts and missing meta tags', () => {
    expect(parsePluginContexts('<html><head><meta name="fwlite-plugin-contexts" content="entry galaxy"></head></html>'))
      .toEqual(['entry']);
    expect(parsePluginContexts('<html><head></head></html>')).toEqual([]);
  });
});
