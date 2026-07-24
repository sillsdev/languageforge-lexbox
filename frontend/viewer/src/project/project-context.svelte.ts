import {getContext, onDestroy, setContext, untrack} from 'svelte';
import type {ILexboxServer, IMiniLcmFeatures, IMiniLcmJsInvokable} from '$lib/dotnet-types';
import type {
  IHistoryServiceJsInvokable
} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/IHistoryServiceJsInvokable';
import type {
  ISyncServiceJsInvokable
} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/ISyncServiceJsInvokable';
import {resource, type ResourceReturn} from 'runed';
import {DetachedResource, type DetachedResourceReturn} from './detached-resource';
import {SvelteMap, SvelteSet} from 'svelte/reactivity';
import type {IProjectData} from '$lib/dotnet-types/generated-types/LcmCrdt/IProjectData';
import type {IMediaFilesServiceJsInvokable} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/IMediaFilesServiceJsInvokable';

const projectContextKey = 'current-project';

type ProjectType = 'crdt' | 'fwdata' | undefined;

interface ProjectContextSetup {
  api: IMiniLcmJsInvokable;
  historyService?: IHistoryServiceJsInvokable;
  syncService?: ISyncServiceJsInvokable;
  mediaFilesService?: IMediaFilesServiceJsInvokable;
  projectName: string;
  projectCode: string;
  projectType?: 'crdt' | 'fwdata';
  server?: ILexboxServer;
  projectData?: IProjectData;
  paratext?: boolean;
}
export function initProjectContext(args?: ProjectContextSetup) {
  const context = new ProjectContext(args);
  setContext(projectContextKey, context);
  onDestroy(() => context.destroy());
  return context;
}
export function useProjectContext() {
  return getContext<ProjectContext>(projectContextKey);
}
export class ProjectContext {
  #stateCache = new SvelteMap<symbol, unknown>();
  // Cleanups for the per-service $effect.root instances; see #ownAndCache.
  #rootCleanups: Array<() => void> = [];
  #api: IMiniLcmJsInvokable | undefined = $state(undefined);
  #projectName: string | undefined = $state(undefined);
  #projectCode: string | undefined = $state(undefined);
  #projectType: ProjectType = $state(undefined);
  #server = $state<ILexboxServer>();
  #projectData = $state<IProjectData>();
  #historyService: IHistoryServiceJsInvokable | undefined = $state(undefined);
  #syncService: ISyncServiceJsInvokable | undefined = $state(undefined);
  #mediaFilesService: IMediaFilesServiceJsInvokable | undefined = $state(undefined);
  #paratext = $state(false);
  #detachedResources = new SvelteSet<DetachedResource<unknown>>();
  #features = resource(() => this.#api, (api) => {
    if (!api) return Promise.resolve({} satisfies IMiniLcmFeatures);
    return api.supportedFeatures();
  }, {initialValue: {}});
  public get api() {
    if (!this.#api) throw new Error('api not setup yet');
    return this.#api;
  }
  public get maybeApi() {
    return this.#api;
  }
  public get projectName(): string {
    if (!this.#projectName) return this.projectCode;
    return this.#projectName;
  }
  public get projectCode(): string {
    if (!this.#projectCode) throw new Error('projectCode not set');
    return this.#projectCode;
  }
  public set projectCode(value: string) {
    if (this.#projectCode === value) return;
    if (this.#projectCode) {
      if (import.meta.env.DEV) {
        throw new Error('Cannot set projectCode after it is already set');
      } else {
        console.error('Cannot set projectCode after it is already set');
        return;
      }
    }
    this.#projectCode = value;
  }
  public get projectType(): ProjectType {
    return this.#projectType;
  }
  public set projectType(value: ProjectType) {
    this.#projectType = value;
  }
  public get server(): ILexboxServer | undefined {
    return this.#server;
  }
  public get projectData(): IProjectData | undefined {
    return this.#projectData;
  }
  public get features(): IMiniLcmFeatures {
    return this.#features.current;
  }

  /** Re-fetch {@link features} from the API (e.g. after a test toggles demo write). */
  public refetchFeatures(): Promise<IMiniLcmFeatures | undefined> {
    return this.#features.refetch();
  }
  public get historyService(): IHistoryServiceJsInvokable | undefined {
    return this.#historyService;
  }
  public get syncService(): ISyncServiceJsInvokable | undefined {
    return this.#syncService;
  }
  public get mediaFilesService(): IMediaFilesServiceJsInvokable | undefined {
    return this.#mediaFilesService;
  }
  public get inParatext(): boolean {
    return this.#paratext;
  }

  constructor(args?: ProjectContextSetup) {
    if (args) this.setup(args);
  }

  public setup(args: ProjectContextSetup) {
    this.#api = args.api;
    this.#historyService = args.historyService;
    this.#syncService = args.syncService;
    this.#mediaFilesService = args.mediaFilesService;
    this.#projectName = args.projectName;
    this.#projectCode = args.projectCode;
    this.#projectType = args.projectType;
    this.#server = args.server;
    this.#projectData = args.projectData;
    this.#paratext = args.paratext ?? false;

    for (const res of this.#detachedResources) {
      res.onApiChange(args.api);
    }
  }

  public getOrAddAsync<T>(key: symbol, initialValue: T, factory: (api: IMiniLcmJsInvokable) => Promise<T>, options?: GetOrAddAsyncOptions<T>): DetachedResourceReturn<T> {
    if (this.#stateCache.has(key)) {
      return this.#stateCache.get(key) as DetachedResourceReturn<T>;
    }

    return this.#ownAndCache(key, () => {
      const res = this.apiResource(initialValue, factory, options);
      options?.onAdd?.(res);
      return res;
    });
  }

  /**
   * Creates a `resource` whose lifecycle is manged by the project context.
   * Note: we aren't using resource from runed, because it's based on $effect,
   * which ties the resource lifecycle to the calling component, rather than the project.
   */
  public apiResource<T>(initialValue: T, factory: (api: IMiniLcmJsInvokable) => Promise<T>, options?: { eager?: boolean }): DetachedResourceReturn<T> {
    const res = new DetachedResource(initialValue, factory, () => this.#api, options);
    this.#detachedResources.add(res as DetachedResource<unknown>);
    return res;
  }

  public getOrAdd<T>(key: symbol, factory: () => T): T {
    if (this.#stateCache.has(key)) {
      return this.#stateCache.get(key) as T;
    }
    return this.#ownAndCache(key, factory);
  }

  /**
   * Runs `factory` inside an `$effect.root` so a cached service's `$derived`/
   * `$state` is owned by the long-lived project context, not by whichever
   * component first asked for the service. Without this, a `$derived` owned by a
   * short-lived component (e.g. a virtualized row) stops updating for later
   * readers once that component unmounts — see the regression tests in
   * detached-resource/. `untrack` keeps a caller that's mid-derivation from
   * subscribing to our caches.
   */
  #ownAndCache<T>(key: symbol, factory: () => T): T {
    let result!: T;
    const cleanup = $effect.root(() => {
      result = untrack(factory);
    });
    this.#rootCleanups.push(cleanup);
    this.#stateCache.set(key, result);
    return result;
  }

  /**
   * Tear down all cached service roots so deriveds don't leak. initProjectContext
   * registers this on the initializing component's onDestroy.
   */
  public destroy(): void {
    for (const cleanup of this.#rootCleanups) cleanup();
    this.#rootCleanups = [];
    this.#stateCache.clear();
    // DetachedResource has no explicit dispose; just drop our references.
    this.#detachedResources.clear();
  }
}

type GetOrAddAsyncOptions<T> = {
  eager?: boolean;
  onAdd?: (resource: ResourceReturn<T>) => void;
}
