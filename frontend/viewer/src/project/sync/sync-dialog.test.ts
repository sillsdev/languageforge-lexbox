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
    // a folder sync test's platform features would otherwise add the folder method to later tests
    window.lexbox?.ServiceProvider?.removeService(DotnetService.PlatformFeaturesService);
    (await import('$lib/services/platform-features-service')).refreshPlatformFeatures();
    (await import('$lib/feature-flags/feature-flags.svelte')).featureFlags.channel = '';
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
    await chooseMethod('Other FieldWorks Lite users');
    await vi.waitFor(() => expect(document.body.textContent).toContain('Pending'));

    const button = getButton(/Auto syncing/);
    button.click();
    flushSync();
    // Both labels stay in the button so it keeps its size; only the current one is visible
    expect(visibleLabel(button)).toBe('Syncing...');

    await vi.waitFor(() => expect(visibleLabel(button)).toBe('Auto syncing'));
    expect(syncService.executeSync).toHaveBeenCalledOnce();
    // Only loaded once FieldWorks Send/Receive is chosen
    expect(syncService.getSyncStatus).not.toHaveBeenCalled();
    expect([...document.querySelectorAll('button')].some(b => b.textContent?.includes('Sync your changes'))).toBe(false);
  }, 60_000); // first import transforms the dialog's dependencies

  type SyncMethod = 'Other FieldWorks Lite users' | 'FieldWorks Send/Receive (on Lexbox)' | 'USB Drive / Folder';

  function methodButtons(): string[] {
    return [...document.querySelectorAll('button')].map(b => b.textContent?.trim() ?? '').filter(text => /^(Other FieldWorks Lite users|FieldWorks Send\/Receive|USB Drive)/.test(text));
  }

  // The methods show once the project's features have loaded
  async function chooseMethod(name: SyncMethod) {
    const button = await vi.waitFor(() => {
      const button = [...document.querySelectorAll('button')].find(b => b.textContent?.trim().startsWith(name));
      expect(button).toBeTruthy();
      return button!;
    });
    button.click();
    flushSync();
    expect(document.querySelector('h3')?.textContent).toBe(name);
  }

  function back() {
    document.querySelector<HTMLButtonElement>('button[title="Back"]')!.click();
    flushSync();
  }

  async function systemBack() {
    history.back();
    await new Promise(resolve => window.addEventListener('popstate', resolve, {once: true}));
    await new Promise(resolve => setTimeout(resolve));
    flushSync();
  }

  function dialogOpen() {
    return new URL(location.href).searchParams.get('syncDialogOpen') === 'true';
  }

  it('goes back to the method list on system back, then closes', async () => {
    await openDialog(fakeSyncService());
    await vi.waitFor(() => expect(dialogOpen()).toBe(true));
    await chooseMethod('FieldWorks Send/Receive (on Lexbox)');
    await new Promise(resolve => setTimeout(resolve, 50));

    await systemBack();
    expect(dialogOpen()).toBe(true);
    await vi.waitFor(() => expect(methodButtons()).toHaveLength(2));

    await systemBack();
    await vi.waitFor(() => expect(dialogOpen()).toBe(false));
  }, 60_000);

  it('leaves no history entry behind after the back arrow', async () => {
    await openDialog(fakeSyncService());
    await vi.waitFor(() => expect(dialogOpen()).toBe(true));
    await chooseMethod('FieldWorks Send/Receive (on Lexbox)');
    await new Promise(resolve => setTimeout(resolve, 50));
    back();
    await vi.waitFor(() => expect(methodButtons()).toHaveLength(2));
    await new Promise(resolve => setTimeout(resolve, 50));

    // the next system back closes the dialog instead of reopening the method
    await systemBack();
    await vi.waitFor(() => expect(dialogOpen()).toBe(false));
  }, 60_000);

  it('closing from a method view leaves no history entries behind', async () => {
    await openDialog(fakeSyncService());
    await vi.waitFor(() => expect(dialogOpen()).toBe(true));
    const startLength = history.length;
    await chooseMethod('FieldWorks Send/Receive (on Lexbox)');
    await vi.waitFor(() => expect(history.length).toBe(startLength + 1));

    document.querySelector<HTMLButtonElement>('[data-dialog-close]')!.click();
    await vi.waitFor(() => expect(dialogOpen()).toBe(false));
    await new Promise(resolve => setTimeout(resolve, 100));
    // back on the entry from before the dialog opened
    expect(history.state).toEqual({});
  }, 60_000);

  it('focuses the back button on opening a method, and that method on going back', async () => {
    await openDialog(fakeSyncService());
    await chooseMethod('FieldWorks Send/Receive (on Lexbox)');
    expect(document.activeElement?.getAttribute('title')).toBe('Back');
    back();
    await vi.waitFor(() => expect(document.activeElement?.textContent?.trim()).toMatch(/^FieldWorks Send\/Receive/));
  }, 60_000);

  function visibleLabel(button: HTMLButtonElement) {
    return [...button.querySelectorAll('.grid > span')].find(s => !s.classList.contains('invisible'))?.textContent;
  }

  it('loads the FieldWorks Classic status only once that method is chosen', async () => {
    const syncService = fakeSyncService();
    await openDialog(syncService);
    await vi.waitFor(() => expect(methodButtons()).toHaveLength(2));
    expect(syncService.getSyncStatus).not.toHaveBeenCalled();
    await chooseMethod('FieldWorks Send/Receive (on Lexbox)');
    await vi.waitFor(() => expect(syncService.getSyncStatus).toHaveBeenCalledOnce());
  }, 60_000);

  it('shows a failed FieldWorks Classic status inline', async () => {
    const syncService = fakeSyncService();
    syncService.getSyncStatus.mockImplementation(() => Promise.reject(new Error('Lexbox unreachable')));
    await openDialog(syncService);
    await chooseMethod('FieldWorks Send/Receive (on Lexbox)');
    await vi.waitFor(() => expect(document.body.textContent).toContain(`Couldn't get the FieldWorks Classic status from Lexbox.`));
  }, 60_000);

  it('keeps the FieldWorks Classic status when going back and forth', async () => {
    const syncService = fakeSyncService();
    syncService.getSyncStatus.mockImplementation(() => Promise.resolve({
      status: ProjectSyncStatusEnum.ReadyToSync,
      pendingCrdtChanges: 0,
      pendingMercurialChanges: 5,
    }));
    await openDialog(syncService);
    await chooseMethod('FieldWorks Send/Receive (on Lexbox)');
    await vi.waitFor(() => expect(document.body.textContent).toContain('New data'));
    back();
    await chooseMethod('Other FieldWorks Lite users');
    back();
    await chooseMethod('FieldWorks Send/Receive (on Lexbox)');
    expect(document.body.textContent).toContain('New data');
    expect(syncService.getSyncStatus).toHaveBeenCalledOnce();
  }, 60_000);

  // Folder sync is a beta feature, and asks for a name before the first sync in a project without a Lexbox user
  async function enableFolderSync(authorName = 'Test User') {
    (await import('$lib/feature-flags/feature-flags.svelte')).featureFlags.channel = 'beta';
    await (await import('$lib/author/local-author-name.svelte')).useLocalAuthorName().set(authorName);
  }

  async function setFolderSyncSupport(pickFolder: () => Promise<string | undefined>) {
    await enableFolderSync();
    const {fallbackPlatformFeaturesService, refreshPlatformFeatures} = await import('$lib/services/platform-features-service');
    window.lexbox.ServiceProvider.setService(DotnetService.PlatformFeaturesService, {
      ...fallbackPlatformFeaturesService,
      supportsFolderSync: () => Promise.resolve(true),
      hasFolderPicker: () => Promise.resolve(true),
      pickFolder,
    });
    refreshPlatformFeatures();
  }

  it('asks for a typed path where the host has no folder picker', async () => {
    await enableFolderSync();
    const {fallbackPlatformFeaturesService, refreshPlatformFeatures} = await import('$lib/services/platform-features-service');
    const folderExists = vi.fn((path: string) => Promise.resolve(path === '/media/usb'));
    window.lexbox.ServiceProvider.setService(DotnetService.PlatformFeaturesService, {
      ...fallbackPlatformFeaturesService,
      supportsFolderSync: () => Promise.resolve(true),
      folderExists,
    });
    refreshPlatformFeatures();
    const syncService = fakeSyncService();
    await openDialog(syncService);
    await chooseMethod('USB Drive / Folder');

    await vi.waitFor(() => getButton(/Sync with a folder\.\.\./).click());
    const input = await vi.waitFor(() => {
      const input = document.querySelector<HTMLInputElement>('#folder-picker-path');
      expect(input).toBeTruthy();
      return input!;
    });
    function typePath(path: string) {
      input.value = path;
      input.dispatchEvent(new Event('input', {bubbles: true}));
      flushSync();
      getButton(/Use this folder/).click();
    }

    typePath('/media/typo');
    await vi.waitFor(() => expect(document.body.textContent).toContain(`This folder doesn't exist.`));
    expect(syncService.syncWithFolder).not.toHaveBeenCalled();

    typePath('/media/usb');
    await vi.waitFor(() => expect(syncService.syncWithFolder).toHaveBeenCalledWith('/media/usb'));
  }, 60_000);

  it('offers folder sync only where the host supports it', async () => {
    const {refreshPlatformFeatures} = await import('$lib/services/platform-features-service');
    window.lexbox.ServiceProvider.removeService(DotnetService.PlatformFeaturesService);
    refreshPlatformFeatures();
    await openDialog(fakeSyncService());
    await vi.waitFor(() => expect(methodButtons()).toHaveLength(2));
    expect(methodButtons()).not.toContainEqual(expect.stringContaining('USB Drive'));
  }, 60_000);

  it('syncs with the picked folder, then offers to sync with it again', async () => {
    await setFolderSyncSupport(() => Promise.resolve(String.raw`E:\test-project`));
    const syncService = fakeSyncService();
    syncService.getRememberedSyncFolder
      .mockResolvedValueOnce(undefined)
      .mockResolvedValue(String.raw`E:\test-project`);
    await openDialog(syncService);
    await chooseMethod('USB Drive / Folder');

    await vi.waitFor(() => getButton(/Sync with a folder\.\.\./).click());
    await vi.waitFor(() => expect(syncService.syncWithFolder).toHaveBeenCalledWith(String.raw`E:\test-project`));
    await vi.waitFor(() => expect(getButton(/Sync with E:/).title).toBe(String.raw`E:\test-project`));
    expect(document.body.textContent).toContain('Sync with another folder...');
    expect(document.body.textContent).not.toContain('Sync with a folder...');

    // The method list shows the new folder too
    back();
    await vi.waitFor(() => expect(methodButtons()).toContainEqual(expect.stringContaining(String.raw`Sync with E:`)));
  }, 60_000);

  it('hides folder sync outside the beta channel', async () => {
    await setFolderSyncSupport(() => Promise.resolve(undefined));
    (await import('$lib/feature-flags/feature-flags.svelte')).featureFlags.channel = '';
    await openDialog(fakeSyncService());
    await vi.waitFor(() => expect(methodButtons()).toHaveLength(2));
    expect(methodButtons()).not.toContainEqual(expect.stringContaining('USB Drive'));
  }, 60_000);

  it('asks for a name before the first folder sync, then uses it', async () => {
    await setFolderSyncSupport(() => Promise.resolve(String.raw`E:\test-project`));
    const authorName = (await import('$lib/author/local-author-name.svelte')).useLocalAuthorName();
    await authorName.set('');
    const syncService = fakeSyncService();
    await openDialog(syncService);
    await chooseMethod('USB Drive / Folder');

    await vi.waitFor(() => getButton(/Sync with a folder\.\.\./).click());
    const input = await vi.waitFor(() => {
      const input = document.querySelector<HTMLInputElement>('#author-name');
      expect(input).toBeTruthy();
      return input!;
    });
    getButton(/Continue/).click();
    await vi.waitFor(() => expect(document.body.textContent).toContain('Enter your name'));

    input.value = 'Ana';
    input.dispatchEvent(new Event('input', {bubbles: true}));
    flushSync();
    getButton(/Continue/).click();
    await vi.waitFor(() => expect(syncService.syncWithFolder).toHaveBeenCalledWith(String.raw`E:\test-project`));
    expect(authorName.current).toBe('Ana');
    await vi.waitFor(() => expect(document.body.textContent).toContain('Your changes will show the name Ana.'));
  }, 60_000);

  it('does nothing when the folder picker is cancelled', async () => {
    await setFolderSyncSupport(() => Promise.resolve(undefined));
    const syncService = fakeSyncService();
    await openDialog(syncService);
    await chooseMethod('USB Drive / Folder');

    await vi.waitFor(() => getButton(/Sync with a folder\.\.\./).click());
    await vi.waitFor(() => expect(getButton(/Sync with a folder\.\.\./).disabled).toBe(false));
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
    expect(methodButtons()).toHaveLength(0);
    expect(syncService.getSyncStatus).not.toHaveBeenCalled();

    getButton(/Sync your changes with other FieldWorks Lite users/).click();
    await vi.waitFor(() => expect(document.body.textContent).toContain('when someone with permission syncs them'));
  }, 60_000);
});
