import {getContext, setContext} from 'svelte';
import type {IMiniLcmFeatures, IMiniLcmJsInvokable} from '$lib/dotnet-types';
import type {
  IHistoryServiceJsInvokable
} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/IHistoryServiceJsInvokable';
import type {
  ISyncServiceJsInvokable
} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/ISyncServiceJsInvokable';
import {resource, type ResourceReturn} from 'runed';
import {SvelteMap} from 'svelte/reactivity';

const projectContextKey = 'current-project';

type ProjectType = 'crdt' | 'fwdata' | undefined;

interface ProjectContextSetup {
  api: IMiniLcmJsInvokable;
  historyService?: IHistoryServiceJsInvokable;
  syncService?: ISyncServiceJsInvokable;
  projectName: string;
  projectCode: string;
  projectType?: 'crdt' | 'fwdata';
}
export function initProjectContext(args?: ProjectContextSetup) {
  const context = new ProjectContext(args);
  setContext(projectContextKey, context);
  return context;
}
export function useProjectContext() {
  return getContext<ProjectContext>(projectContextKey);
}
export class ProjectContext {
  #stateCache = new SvelteMap<symbol, unknown>();
  #api: IMiniLcmJsInvokable | undefined = $state(undefined);
  #projectName: string | undefined = $state(undefined);
  #projectCode: string | undefined = $state(undefined);
  #projectType: ProjectType = $state(undefined);
  #historyService: IHistoryServiceJsInvokable | undefined = $state(undefined);
  #syncService: ISyncServiceJsInvokable | undefined = $state(undefined);
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
    if (!this.#projectName) throw new Error('projectName not set');
    return this.#projectName;
  }
  public get projectCode(): string {
    if (!this.#projectCode) throw new Error('projectCode not set');
    return this.#projectCode;
  }
  public get projectType(): ProjectType {
    return this.#projectType;
  }
  public get features(): IMiniLcmFeatures {
    return this.#features.current;
  }
  public get historyService(): IHistoryServiceJsInvokable | undefined {
    return this.#historyService;
  }
  public get syncService(): ISyncServiceJsInvokable | undefined {
    return this.#syncService;
  }

  constructor(args?: ProjectContextSetup) {
    this.#api = args?.api;
    this.#historyService = args?.historyService;
    this.#syncService = args?.syncService;
    this.#projectName = args?.projectName;
    this.#projectCode = args?.projectCode;
    this.#projectType = args?.projectType;
  }

  public setup(args: ProjectContextSetup) {
    this.#api = args.api;
    this.#historyService = args.historyService;
    this.#syncService = args.syncService;
    this.#projectName = args.projectName;
    this.#projectCode = args.projectCode;
    this.#projectType = args.projectType;
  }

  public getOrAddAsync<T>(key: symbol, initialValue: T, factory: (api: IMiniLcmJsInvokable) => Promise<T>): ResourceReturn<T, unknown, true> {
    if (this.#stateCache.has(key)) {
      return this.#stateCache.get(key) as ResourceReturn<T, unknown, true>;
    }

    const res = this.apiResource(initialValue, factory);
    this.#stateCache.set(key, res);
    return res;
  }

  public apiResource<T>(initialValue: T, factory: (api: IMiniLcmJsInvokable) => Promise<T>): ResourceReturn<T, unknown, true> {
    const res = resource<IMiniLcmJsInvokable | undefined>(() => this.#api,
      ((api) => {
        if (!api) return Promise.resolve(initialValue);
        return factory(api);
      }), {initialValue});
    return res;
  }

  public getOrAdd<T>(key: symbol, factory: () => T) {
    if (this.#stateCache.has(key)) {
      return this.#stateCache.get(key) as T;
    }
    const result = factory();
    this.#stateCache.set(key, result);
    return result;
  }
}
