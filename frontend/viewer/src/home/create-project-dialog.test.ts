import {afterEach, beforeEach, describe, expect, it, vi} from 'vitest';
import {flushSync, mount, unmount} from 'svelte';
import {DotnetService} from '$lib/dotnet-types';
import type {ICombinedProjectsService} from '$lib/dotnet-types';
import {PreferenceKey} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/PreferenceKey';

vi.stubGlobal('matchMedia', (query: string) => ({
  matches: false, media: query, onchange: null,
  addEventListener: () => {}, removeEventListener: () => {}, addListener: () => {}, removeListener: () => {}, dispatchEvent: () => false,
}));
vi.stubGlobal('ResizeObserver', class { observe() {} unobserve() {} disconnect() {} });
Element.prototype.animate = () => ({cancel() {}, finish() {}, onfinish: null}) as unknown as Animation;

describe('CreateProjectDialog', () => {
  let app: ReturnType<typeof mount> | undefined;
  let createProject: ReturnType<typeof vi.fn>;

  beforeEach(async () => {
    localStorage.clear();
    (await import('$lib/services/service-provider')).setupServiceProvider();
    (await import('$lib/services/browser-app-services')).setupBrowserAppServices();
    createProject = vi.fn(() => Promise.resolve());
    window.lexbox.ServiceProvider.setService(DotnetService.CombinedProjectsService, {createProject} as unknown as ICombinedProjectsService);
  });
  afterEach(async () => {
    if (app) await unmount(app);
    app = undefined;
    await new Promise(resolve => setTimeout(resolve, 50));
    document.body.innerHTML = '';
  });

  async function openDialog() {
    const dialog = (await import('./CreateProjectDialog.svelte')).default;
    const target = document.createElement('div');
    document.body.appendChild(target);
    app = mount(dialog, {target, props: {refreshProjects: () => Promise.resolve()}});
    flushSync();
    (app as {openDialog: () => void}).openDialog();
    flushSync();
    await vi.waitFor(() => expect(document.body.textContent).toContain('Your name'));
  }

  function input(label: string): HTMLInputElement {
    const labelElement = [...document.querySelectorAll('label')].find(l => l.textContent?.trim().startsWith(label));
    const field = labelElement?.querySelector('input');
    if (!field) throw new Error(`No input labelled ${label}`);
    return field;
  }

  function type(field: HTMLInputElement, value: string) {
    field.value = value;
    field.dispatchEvent(new Event('input', {bubbles: true}));
  }

  function clickCreate() {
    const button = [...document.querySelectorAll('button')].find(b => /Create/.test(b.textContent ?? ''));
    if (!button) throw new Error('No Create button');
    button.click();
  }

  async function storedName(): Promise<string | undefined> {
    const {usePreferencesService} = await import('$lib/services/service-provider');
    return usePreferencesService().get(PreferenceKey.LocalAuthorName);
  }

  it('requires the author name and saves it on the device before creating the project', async () => {
    await openDialog();
    type(input('Name'), 'Sena');
    type(input('Vernacular writing system code'), 'seh');

    clickCreate();
    await vi.waitFor(() => expect(document.body.textContent).toContain('Your name is required'));
    expect(createProject).not.toHaveBeenCalled();

    type(input('Your name'), ' Jane ');
    clickCreate();
    await vi.waitFor(() => expect(createProject).toHaveBeenCalledWith('Sena', 'sena-seh', 'seh', undefined));
    expect(await storedName()).toBe('Jane');
  }, 60_000);

  it('prefills the name this device already has, and lets the user change it', async () => {
    const {usePreferencesService} = await import('$lib/services/service-provider');
    await usePreferencesService().set(PreferenceKey.LocalAuthorName, 'Jane');
    await openDialog();
    await vi.waitFor(() => expect(input('Your name').value).toBe('Jane'));

    type(input('Name'), 'Sena');
    type(input('Vernacular writing system code'), 'seh');
    type(input('Your name'), 'Joan');
    clickCreate();

    await vi.waitFor(() => expect(createProject).toHaveBeenCalled());
    expect(await storedName()).toBe('Joan');
  }, 60_000);
});
