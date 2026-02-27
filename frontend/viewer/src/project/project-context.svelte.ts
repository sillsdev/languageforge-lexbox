import {getContext, setContext} from 'svelte';
import type {ILexboxServer, IMiniLcmFeatures, IMiniLcmJsInvokable} from '$lib/dotnet-types';
import type {
  IHistoryServiceJsInvokable
} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/IHistoryServiceJsInvokable';
import type {
  ISyncServiceJsInvokable
} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/ISyncServiceJsInvokable';
import {resource, type ResourceOptions, type ResourceReturn} from 'runed';
import {SvelteMap} from 'svelte/reactivity';
import type {IProjectData} from '$lib/dotnet-types/generated-types/LcmCrdt/IProjectData';
import {randomId} from '$lib/utils';

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
  #queuedResources: Record<string, (api: IMiniLcmJsInvokable) => void> = {};
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

    Object.values<(api: IMiniLcmJsInvokable) => void>(this.#queuedResources)
      .forEach(factory => factory(args.api));
    this.#queuedResources = {};
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
    const resourceId = randomId();

    const res = resource<IMiniLcmJsInvokable | undefined>(() => this.#api,
      ((api) => {
        if (!api) return Promise.resolve(initialValue);
        delete this.#queuedResources[resourceId];
        return factory(api);
      }), {initialValue, ...options});


    // If the api is not yet defined, a couple things could go wrong:
    // 1) Throttling could throttle/swallow the refetch when the api becomes defined
    // 2) The component that triggered the load could get destroyed before the load is complete, which would cancel the load/resource-$effect.
    // (the fact that the loading is scoped to the component lifecycle is a non-trivial architecture problem)
    //
    // both cases can prevent the resource from ever being initialized.
    // So, we queue it up to be explicitly initialized when the api is set up.
    if (!this.#api) {
      this.#queuedResources[resourceId] = (api) => {
        void factory(api).then(res.mutate);
      };
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
