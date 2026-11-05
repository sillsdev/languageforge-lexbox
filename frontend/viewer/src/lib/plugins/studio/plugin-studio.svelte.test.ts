import {afterEach, describe, expect, it, vi} from 'vitest';
import type {IMiniLcmJsInvokable, IPlugin} from '$lib/dotnet-types';
import {PluginStudio, type StudioPluginService} from './plugin-studio.svelte';

function html(body: string, options: {title?: string; permissions?: string} = {}) {
  const head = [
    options.title ? `<title>${options.title}</title>` : '',
    options.permissions ? `<meta name="fwlite-plugin-permissions" content="${options.permissions}">` : '',
  ].join('');
  return `<!DOCTYPE html><html><head>${head}</head><body>${body}</body></html>`;
}

/** A plugin service holding one saved plugin, whose run consent starts out granted or not. */
function fakePluginService(saved?: {plugin: IPlugin; html: string; approved: boolean}) {
  let grantedHash: string | undefined;
  const service = {
    current: saved ? [saved.plugin] : [],
    loaded: true,
    getHtml: vi.fn(() => Promise.resolve({result: 'ok' as const, html: saved?.html ?? ''})),
    consentStore: {
      isGranted: vi.fn(() => saved?.approved ?? false),
      grant: vi.fn((_id: string, hash: string) => { grantedHash = hash; }),
    },
    writeTrustStore: {grantedHash: () => undefined},
    create: vi.fn(),
    addExample: vi.fn(),
    update: vi.fn(),
    delete: vi.fn(),
  };
  return {service, grantedHash: () => grantedHash};
}

const studios: PluginStudio[] = [];
afterEach(() => {
  while (studios.length) studios.pop()!.destroy();
});

function createStudio(mode: 'new' | 'edit', service: ReturnType<typeof fakePluginService>['service'], pluginId?: string) {
  const studio = new PluginStudio({
    mode,
    pluginId,
    pluginService: service as unknown as StudioPluginService,
    project: {api: {} as IMiniLcmJsInvokable, projectName: 'Test', projectCode: 'test'},
    goToPlugins: vi.fn(),
    openPlugin: vi.fn(),
  });
  studios.push(studio);
  return studio;
}

/** Edit mode loads the saved plugin and checks its consent asynchronously. */
async function createEditStudio(savedHtml: string, approved: boolean) {
  const plugin = {id: 'p1', name: 'Saved', description: undefined} as unknown as IPlugin;
  const fake = fakePluginService({plugin, html: savedHtml, approved});
  const studio = createStudio('edit', fake.service, plugin.id);
  await vi.waitFor(() => expect(studio.savedApproval).toBeDefined());
  return {studio, fake};
}

describe('PluginStudio', () => {
  it('rejects a paste that is not a complete plugin', async () => {
    const studio = createStudio('new', fakePluginService().service);
    expect(await studio.useHtml('just some chat text')).toBe(false);
    expect(studio.pasteError).toBe('incomplete');
    expect(studio.draft.html).toBe('');
  });

  it('runs an offline version straight away and names a new plugin from its title', async () => {
    const studio = createStudio('new', fakePluginService().service);
    expect(await studio.useHtml(`Here it is:\n${html('v1', {title: 'Word counter'})}\nEnjoy`)).toBe(true);
    expect(studio.internetPrompt).toBeUndefined();
    expect(studio.draft.html).toBe(html('v1', {title: 'Word counter'}));
    expect(studio.draft.name).toBe('Word counter');
    expect(studio.nameFromHtml).toBe(true);

    // The name keeps following the title until the user types one.
    await studio.useHtml(html('v2', {title: 'Word counter 2'}));
    expect(studio.draft.name).toBe('Word counter 2');
    studio.nameFromHtml = false;
    await studio.useHtml(html('v3', {title: 'Word counter 3'}));
    expect(studio.draft.name).toBe('Word counter 2');
  });

  it('asks before a version that newly wants the internet runs', async () => {
    const studio = createStudio('new', fakePluginService().service);
    const online = html('v1', {permissions: 'internet'});

    const declined = studio.useHtml(online);
    await vi.waitFor(() => expect(studio.internetPrompt?.html).toBe(online));
    studio.answerInternetPrompt('cancel');
    expect(await declined).toBe(false);
    expect(studio.draft.html).toBe('');

    const allowed = studio.useHtml(online);
    await vi.waitFor(() => expect(studio.internetPrompt).toBeDefined());
    studio.answerInternetPrompt('allow');
    expect(await allowed).toBe(true);
    expect(studio.draft.html).toBe(online);

    // Already online, so a later version doesn't ask again.
    expect(await studio.useHtml(html('v2', {permissions: 'internet'}))).toBe(true);
    expect(studio.internetPrompt).toBeUndefined();
  });

  it('lists an internet permission allowed at the prompt instead of warning about it', async () => {
    const {studio} = await createEditStudio(html('v1'), true);
    const pasted = studio.useHtml(html('v2', {permissions: 'internet'}));
    await vi.waitFor(() => expect(studio.internetPrompt).toBeDefined());
    studio.answerInternetPrompt('allow');
    await pasted;

    expect(studio.review?.changes.added.permissions).toEqual(['internet']);
    expect(studio.keptPermissions).toEqual(['internet']);
    expect(studio.newPermissions).toEqual([]);
  });

  it('warns about a new edit permission until the user keeps it', async () => {
    const {studio} = await createEditStudio(html('v1'), true);
    await studio.useHtml(html('v2', {permissions: 'edit'}));
    expect(studio.newPermissions).toEqual(['edit']);
    studio.keepPermission('edit');
    expect(studio.newPermissions).toEqual([]);

    studio.undoVersion();
    expect(studio.draft.html).toBe(html('v1'));
    expect(studio.keptPermissions).toEqual([]);
  });

  it('holds back an unapproved saved plugin until the user approves it', async () => {
    const {studio, fake} = await createEditStudio(html('v1'), false);
    expect(studio.needsConsent).toBe(true);

    studio.approveSaved();
    expect(studio.needsConsent).toBe(false);
    expect(fake.service.consentStore.grant).toHaveBeenCalledWith('p1', studio.savedApproval?.hash);
  });

  it('asks about the internet even when the unapproved saved version already had it', async () => {
    const {studio} = await createEditStudio(html('v1', {permissions: 'internet'}), false);
    void studio.useHtml(html('v2', {permissions: 'internet'}));
    await vi.waitFor(() => expect(studio.internetPrompt).toBeDefined());
    studio.answerInternetPrompt('cancel');
  });

  it('only asks before leaving when there is unsaved work', async () => {
    const {studio} = await createEditStudio(html('v1'), true);
    expect(await studio.confirmLeave()).toBe(true);

    studio.draft.name = 'Renamed';
    const leaving = studio.confirmLeave();
    expect(studio.leavePrompt).toBeDefined();
    studio.answerLeavePrompt(false);
    expect(await leaving).toBe(false);
  });
});
