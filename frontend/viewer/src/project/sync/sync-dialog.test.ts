import {afterEach, beforeEach, describe, expect, it, vi} from 'vitest';
import {flushSync, mount, unmount} from 'svelte';
import type {ISyncServiceJsInvokable} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/ISyncServiceJsInvokable';
import {DotnetService, SyncJobStatusEnum} from '$lib/dotnet-types';
import type {IProjectSyncStatus} from '$lib/dotnet-types/generated-types/LexCore/Sync/IProjectSyncStatus';
import {ProjectSyncStatusEnum} from '$lib/dotnet-types/generated-types/LexCore/Sync/ProjectSyncStatusEnum';
import {FolderSyncStatus} from '$lib/dotnet-types/generated-types/FwLiteShared/LocalSync/FolderSyncStatus';

vi.stubGlobal('matchMedia', (query: string) => ({
  matches: false, media: query, onchange: null,
  addEventListener: () => {}, removeEventListener: () => {}, addListener: () => {}, removeListener: () => {}, dispatchEvent: () => false,
}));
vi.stubGlobal('ResizeObserver', class { observe() {} unobserve() {} disconnect() {} });
Element.prototype.animate = () => ({cancel() {}, finish() {}, onfinish: null}) as unknown as Animation;

function fakeSyncService() {
  return {
    // The FieldWorks Classic status is a slow Lexbox request: here it never answers
    getSyncStatus: vi.fn((): Promise<IProjectSyncStatus> => new Promise(() => {})),
    triggerFwHeadlessSync: vi.fn(() => Promise.resolve({status: SyncJobStatusEnum.Success})),
    countPendingCrdtCommits: vi.fn(() => Promise.resolve({local: 3, remote: 4})),
    getLatestSyncedCommitDate: vi.fn(() => Promise.resolve(undefined)),
    executeSync: vi.fn(() => Promise.resolve({isSynced: true, missingFromLocal: [], missingFromRemote: []})),
    getCurrentServer: vi.fn(() => Promise.resolve({id: 'test-server', displayName: 'Lexbox', authority: 'https://test-server.com'})),
    syncWithFolder: vi.fn(() => Promise.resolve({status: FolderSyncStatus.Synced, mediaSent: 0, mediaReceived: 0, mediaFailed: 0})),
    getRememberedSyncFolder: vi.fn(() => Promise.resolve<string | undefined>(undefined)),
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
    // bits-ui restores the body style 24ms after the dialog unmounts; let it run before jsdom is torn down
    await new Promise(resolve => setTimeout(resolve, 50));
    document.body.innerHTML = '';
  });

  async function openDialog(syncService: ISyncServiceJsInvokable, fieldWorksSync = true) {
    const supportedFeatures = vi.fn(() => Promise.resolve({sync: true, write: true, fieldWorksSync}));
    // Imported after the stubs above: the dialog reads matchMedia when its module loads
    const syncDialogHost = (await import('./test/SyncDialogHost.svelte')).default;
    const target = document.createElement('div');
    document.body.appendChild(target);
    app = mount(syncDialogHost, {target, props: {syncService, supportedFeatures}});
    flushSync();
    (app as {open: () => void}).open();
    flushSync();
    return supportedFeatures;
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
    expect([...document.querySelectorAll('button')].some(b => b.textContent?.includes('Sync your changes'))).toBe(false);
  }, 60_000); // first import transforms the dialog's dependencies

  // The tabs show once the project's features have loaded
  async function showTab(name: 'FieldWorks Lite' | 'Lexbox') {
    const tab = await vi.waitFor(() => {
      const tab = [...document.querySelectorAll<HTMLButtonElement>('[role="tab"]')].find(t => t.textContent?.includes(name));
      expect(tab).toBeTruthy();
      return tab!;
    });
    tab.dispatchEvent(new MouseEvent('mousedown', {bubbles: true}));
    tab.click();
    flushSync();
    expect(tab.getAttribute('aria-selected')).toBe('true');
  }

  it('loads the FieldWorks Classic status when its tab is first shown', async () => {
    const syncService = fakeSyncService();
    await openDialog(syncService);
    await showTab('Lexbox');
    await vi.waitFor(() => expect(syncService.getSyncStatus).toHaveBeenCalledOnce());
  }, 60_000);

  it('shows a failed FieldWorks Classic status inline', async () => {
    const syncService = fakeSyncService();
    syncService.getSyncStatus.mockImplementation(() => Promise.reject(new Error('Lexbox unreachable')));
    await openDialog(syncService);
    await showTab('Lexbox');
    await vi.waitFor(() => expect(document.body.textContent).toContain(`Couldn't get the FieldWorks Classic status from Lexbox.`));
  }, 60_000);

  it('keeps the FieldWorks Classic status when switching tabs', async () => {
    const syncService = fakeSyncService();
    syncService.getSyncStatus.mockImplementation(() => Promise.resolve({
      status: ProjectSyncStatusEnum.ReadyToSync,
      pendingCrdtChanges: 0,
      pendingMercurialChanges: 5,
    }));
    await openDialog(syncService);
    await showTab('Lexbox');
    await vi.waitFor(() => expect(document.body.textContent).toContain('New data'));
    await showTab('FieldWorks Lite');
    await showTab('Lexbox');
    expect(document.body.textContent).toContain('New data');
    expect(syncService.getSyncStatus).toHaveBeenCalledOnce();
  }, 60_000);

  async function setFolderSyncSupport(pickFolder: () => Promise<string | undefined>) {
    const {fallbackPlatformFeaturesService, refreshPlatformFeatures} = await import('$lib/services/platform-features-service');
    window.lexbox.ServiceProvider.setService(DotnetService.PlatformFeaturesService, {
      ...fallbackPlatformFeaturesService,
      supportsFolderSync: () => Promise.resolve(true),
      pickFolder,
    });
    refreshPlatformFeatures();
  }

  it('offers folder sync only where the host can pick a folder', async () => {
    const {refreshPlatformFeatures} = await import('$lib/services/platform-features-service');
    refreshPlatformFeatures();
    await openDialog(fakeSyncService());
    await vi.waitFor(() => expect(document.body.textContent).toContain('Pending'));
    expect(document.body.textContent).not.toContain('Sync with folder...');
  }, 60_000);

  it('syncs with the picked folder, then offers to sync with it again', async () => {
    await setFolderSyncSupport(() => Promise.resolve(String.raw`E:\test-project`));
    const syncService = fakeSyncService();
    syncService.getRememberedSyncFolder
      .mockResolvedValueOnce(undefined)
      .mockResolvedValue(String.raw`E:\test-project`);
    await openDialog(syncService);

    await vi.waitFor(() => getButton(/Sync with folder\.\.\./).click());
    await vi.waitFor(() => expect(syncService.syncWithFolder).toHaveBeenCalledWith(String.raw`E:\test-project`));
    await vi.waitFor(() => expect(getButton(/again/).title).toBe(String.raw`E:\test-project`));
  }, 60_000);

  it('does nothing when the folder picker is cancelled', async () => {
    await setFolderSyncSupport(() => Promise.resolve(undefined));
    const syncService = fakeSyncService();
    await openDialog(syncService);

    await vi.waitFor(() => getButton(/Sync with folder\.\.\./).click());
    await vi.waitFor(() => expect(getButton(/Sync with folder\.\.\./).disabled).toBe(false));
    expect(syncService.syncWithFolder).not.toHaveBeenCalled();
  }, 60_000);

  it('shows only the FW Lite sync without FieldWorks sync', async () => {
    const syncService = fakeSyncService();
    const supportedFeatures = await openDialog(syncService, false);
    await vi.waitFor(() => expect(supportedFeatures).toHaveBeenCalled());
    await supportedFeatures.mock.results[0].value;
    await new Promise(resolve => setTimeout(resolve));
    flushSync();
    expect(document.body.textContent).toContain('Pending');
    expect(document.querySelectorAll('[role="tab"]')).toHaveLength(0);
    expect(syncService.getSyncStatus).not.toHaveBeenCalled();

    getButton(/Sync your changes with other FieldWorks Lite users/).click();
    await vi.waitFor(() => expect(document.body.textContent).toContain('when someone with permission syncs them'));
  }, 60_000);
});
