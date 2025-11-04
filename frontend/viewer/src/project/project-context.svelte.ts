import {getContext, setContext} from 'svelte';
import type {ILexboxServer, IMiniLcmFeatures, IMiniLcmJsInvokable} from '$lib/dotnet-types';
import type {
  IHistoryServiceJsInvokable
} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/IHistoryServiceJsInvokable';
import type {
  ISyncServiceJsInvokable
} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/ISyncServiceJsInvokable';
import {resource, watchOnce, type ResourceOptions, type ResourceReturn} from 'runed';
import {SvelteMap} from 'svelte/reactivity';
import type {IProjectData} from '$lib/dotnet-types/generated-types/LcmCrdt/IProjectData';

const projectContextKey = 'current-project';

type ProjectType = 'crdt' | 'fwdata' | undefined;

interface ProjectContextSetup {
  api: IMiniLcmJsInvokable;
  historyService?: IHistoryServiceJsInvokable;
  syncService?: ISyncServiceJsInvokable;
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
  #server = $state<ILexboxServer>();
  #projectData = $state<IProjectData>();
  #historyService: IHistoryServiceJsInvokable | undefined = $state(undefined);
  #syncService: ISyncServiceJsInvokable | undefined = $state(undefined);
  #paratext = $state(false);
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
  public get historyService(): IHistoryServiceJsInvokable | undefined {
    return this.#historyService;
  }
  public get syncService(): ISyncServiceJsInvokable | undefined {
    return this.#syncService;
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
    this.#projectName = args.projectName;
    this.#projectCode = args.projectCode;
    this.#projectType = args.projectType;
    this.#server = args.server;
    this.#projectData = args.projectData;
    this.#paratext = args.paratext ?? false;
  }

  public getOrAddAsync<T>(key: symbol, initialValue: T, factory: (api: IMiniLcmJsInvokable) => Promise<T>, options?: GetOrAddAsyncOptions<T>): ResourceReturn<T, unknown, true> {
    if (this.#stateCache.has(key)) {
      return this.#stateCache.get(key) as ResourceReturn<T, unknown, true>;
    }

    const res = this.apiResource(initialValue, factory, options);
    this.#stateCache.set(key, res);
    options?.onAdd?.(res);
    return res;
  }

  public apiResource<T>(initialValue: T, factory: (api: IMiniLcmJsInvokable) => Promise<T>, options?: ApiResourceOptions<T>): ResourceReturn<T, unknown, true> {
    const res = resource<IMiniLcmJsInvokable | undefined>(() => this.#api,
      ((api) => {
        if (!api) return Promise.resolve(initialValue);
        return factory(api);
      }), {initialValue, ...options});

    // If throttling and the api is not yet defined, the resource will likely throttle/swallow the refetch
    // call triggered by the api becoming defined. As a result it will never be loaded. So, we explicitly ensure an initial load.
    if (options?.throttle && !this.#api) {
      function initialLoad(api: IMiniLcmJsInvokable) {
        factory(api).then(res.mutate).catch(console.error);
      }

      if (this.#api) {
        initialLoad(this.#api);
      } else {
        watchOnce(() => this.#api, () => {
          if (this.#api) initialLoad(this.#api);
          else console.warn('apiResource: initialLoad expected api to be defined after watchOnce');
        });
      }
    }
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

type ApiResourceOptions<T> = Partial<Omit<ResourceOptions<T>, 'initialValue'>>;

type GetOrAddAsyncOptions<T> = ApiResourceOptions<T> & {
  onAdd?: (resource: ResourceReturn<T>) => void;
}
