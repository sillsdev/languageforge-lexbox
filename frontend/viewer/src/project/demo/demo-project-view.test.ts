import {afterEach, describe, expect, it, vi} from 'vitest';
import {flushSync, mount, unmount} from 'svelte';

// Smoke test: the in-memory demo project (what `task test:ui-standalone` and /testing/project-view use)
// mounts without throwing. Catches an always-mounted component resolving a service the demo never registers.
// jsdom lacks these layout APIs; stub them before the components load.
vi.stubGlobal('matchMedia', (query: string) => ({
  matches: false, media: query, onchange: null,
  addEventListener: () => {}, removeEventListener: () => {}, addListener: () => {}, removeListener: () => {}, dispatchEvent: () => false,
}));
vi.stubGlobal('ResizeObserver', class { observe() {} unobserve() {} disconnect() {} });
vi.stubGlobal('scrollTo', () => {});
Element.prototype.animate = () => ({cancel() {}, finish() {}, onfinish: null}) as unknown as Animation;

describe('demo project view', () => {
  let app: ReturnType<typeof mount> | undefined;
  afterEach(async () => {
    if (app) await unmount(app);
    document.body.innerHTML = '';
  });

  it('mounts with the in-memory demo API', async () => {
    window.history.replaceState({}, '', '/testing/project-view');
    // Same bootstrap as main.ts for a browser (not .NET hosted) session.
    (await import('$lib/services/service-provider')).setupServiceProvider();
    (await import('$lib/services/browser-app-services')).setupBrowserAppServices();
    (await import('$lib/services/event-bus')).useEventBus();
    const appComponent = (await import('../../App.svelte')).default;
    const target = document.createElement('div');
    document.body.appendChild(target);
    app = mount(appComponent, {target});
    flushSync();
    expect(target.querySelector('.app')?.childElementCount).toBeGreaterThan(0);
  }, 60_000); // first import transforms the whole app
});
