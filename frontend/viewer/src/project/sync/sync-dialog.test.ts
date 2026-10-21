import {afterEach, beforeEach, describe, expect, it, vi} from 'vitest';
import {flushSync, mount, unmount} from 'svelte';
import type {ISyncServiceJsInvokable} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/ISyncServiceJsInvokable';
import {SyncJobStatusEnum} from '$lib/dotnet-types';

vi.stubGlobal('matchMedia', (query: string) => ({
  matches: false, media: query, onchange: null,
  addEventListener: () => {}, removeEventListener: () => {}, addListener: () => {}, removeListener: () => {}, dispatchEvent: () => false,
}));
vi.stubGlobal('ResizeObserver', class { observe() {} unobserve() {} disconnect() {} });
Element.prototype.animate = () => ({cancel() {}, finish() {}, onfinish: null}) as unknown as Animation;

function fakeSyncService() {
  return {
    // The FieldWorks Classic status is a slow Lexbox request: here it never answers
    getSyncStatus: vi.fn(() => new Promise<never>(() => {})),
    triggerFwHeadlessSync: vi.fn(() => Promise.resolve({status: SyncJobStatusEnum.Success})),
    countPendingCrdtCommits: vi.fn(() => Promise.resolve({local: 3, remote: 4})),
    getLatestSyncedCommitDate: vi.fn(() => Promise.resolve(undefined)),
    executeSync: vi.fn(() => Promise.resolve({isSynced: true, missingFromLocal: [], missingFromRemote: []})),
    getCurrentServer: vi.fn(() => Promise.resolve({id: 'test-server', displayName: 'Lexbox', authority: 'https://test-server.com'})),
  } satisfies ISyncServiceJsInvokable;
}

describe('SyncDialog', () => {
  let app: ReturnType<typeof mount> | undefined;
  beforeEach(async () => {
    window.history.replaceState({}, '', '/');
    (await import('$lib/services/service-provider')).setupServiceProvider();
    (await import('$lib/services/browser-app-services')).setupBrowserAppServices();
  });
  afterEach(async () => {
    if (app) await unmount(app);
    app = undefined;
    document.body.innerHTML = '';
  });

  async function openDialog(syncService: ISyncServiceJsInvokable) {
    // Imported after the stubs above: the dialog reads matchMedia when its module loads
    const syncDialogHost = (await import('./test/SyncDialogHost.svelte')).default;
    const target = document.createElement('div');
    document.body.appendChild(target);
    app = mount(syncDialogHost, {target, props: {syncService}});
    flushSync();
    (app as {open: () => void}).open();
    flushSync();
  }

  function getButton(name: RegExp): HTMLButtonElement {
    const button = [...document.querySelectorAll('button')].find(b => name.test(b.textContent ?? ''));
    if (!button) throw new Error(`No button matching ${name}: ${document.body.textContent}`);
    return button;
  }

  it('FW Lite sync finishes without waiting on the FieldWorks Classic status', async () => {
    const syncService = fakeSyncService();
    await openDialog(syncService);
    await vi.waitFor(() => expect(document.body.textContent).toContain('Pending'));

    getButton(/Auto syncing/).click();
    flushSync();
    expect(getButton(/Syncing/)).toBeTruthy();

    await vi.waitFor(() => expect(document.body.textContent).toContain('Auto syncing'));
    expect(syncService.executeSync).toHaveBeenCalledOnce();
    // Only loaded once the Lexbox tab is shown
    expect(syncService.getSyncStatus).not.toHaveBeenCalled();
  }, 60_000); // first import transforms the dialog's dependencies

  it('loads the FieldWorks Classic status when its tab is first shown', async () => {
    const syncService = fakeSyncService();
    await openDialog(syncService);
    const classicTab = [...document.querySelectorAll<HTMLButtonElement>('[role="tab"]')].find(t => t.textContent?.includes('Lexbox'));
    expect(classicTab).toBeTruthy();
    classicTab!.dispatchEvent(new MouseEvent('mousedown', {bubbles: true}));
    classicTab!.click();
    await vi.waitFor(() => expect(syncService.getSyncStatus).toHaveBeenCalledOnce());
  }, 60_000);
});
