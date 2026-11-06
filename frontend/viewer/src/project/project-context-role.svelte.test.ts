import {afterEach, describe, expect, it, vi} from 'vitest';
import {ProjectContext} from './project-context.svelte';
import type {IMiniLcmJsInvokable} from '$lib/dotnet-types';
import type {IProjectData} from '$lib/dotnet-types/generated-types/LcmCrdt/IProjectData';
import {UserProjectRole} from '$lib/dotnet-types/generated-types/LcmCrdt/UserProjectRole';
import type {IProjectRoleChangedEvent} from '$lib/dotnet-types/generated-types/FwLiteShared/Events/IProjectRoleChangedEvent';
import {FwEventType} from '$lib/dotnet-types/generated-types/FwLiteShared/Events/FwEventType';
import type {EventBus} from '$lib/services/event-bus';

const projectCode = 'test-project';

function fakeEventBus() {
  const callbacks = new Set<(event: IProjectRoleChangedEvent) => void>();
  const eventBus = {
    onProjectRoleChanged(code: string, callback: (event: IProjectRoleChangedEvent) => void) {
      expect(code).toBe(projectCode);
      callbacks.add(callback);
      return () => callbacks.delete(callback);
    },
  } as unknown as EventBus;
  function publish(role: UserProjectRole) {
    callbacks.forEach(cb => cb({role, type: FwEventType.ProjectRoleChanged, isGlobal: false}));
  }
  return {eventBus, publish, subscriberCount: () => callbacks.size};
}

function projectData(role: UserProjectRole): IProjectData {
  return {role} as IProjectData;
}

describe('ProjectContext.watchProjectRole', () => {
  let cleanupRoot: (() => void) | undefined;
  let context: ProjectContext;
  afterEach(() => {
    context?.destroy();
    cleanupRoot?.();
  });

  function setupContext() {
    cleanupRoot = $effect.root(() => {
      context = new ProjectContext();
      context.projectCode = projectCode;
    });
    return context;
  }

  function fakeApi(write: () => boolean) {
    return {supportedFeatures: vi.fn(() => Promise.resolve({write: write()}))} as unknown as IMiniLcmJsInvokable;
  }

  it('applies a role change that arrives before setup', () => {
    const {eventBus, publish} = fakeEventBus();
    const context = setupContext();
    context.watchProjectRole(eventBus);

    // e.g. the sync triggered by opening the project finishes before openCrdtProject returns to JS
    publish(UserProjectRole.Observer);
    context.setup({api: fakeApi(() => false), projectName: projectCode, projectCode, projectData: projectData(UserProjectRole.Editor)});

    expect(context.projectData?.role).toBe(UserProjectRole.Observer);
  });

  it('updates the role and refetches features on a role change after setup', async () => {
    const {eventBus, publish} = fakeEventBus();
    const context = setupContext();
    context.watchProjectRole(eventBus);
    let canWrite = true;
    const api = fakeApi(() => canWrite);
    context.setup({api, projectName: projectCode, projectCode, projectData: projectData(UserProjectRole.Editor)});
    await vi.waitFor(() => expect(context.features.write).toBe(true));

    canWrite = false;
    publish(UserProjectRole.Observer);

    expect(context.projectData?.role).toBe(UserProjectRole.Observer);
    await vi.waitFor(() => expect(context.features.write).toBe(false));
  });

  it('unsubscribes on destroy', () => {
    const {eventBus, subscriberCount} = fakeEventBus();
    const context = setupContext();
    context.watchProjectRole(eventBus);
    expect(subscriberCount()).toBe(1);

    context.destroy();

    expect(subscriberCount()).toBe(0);
  });
});
