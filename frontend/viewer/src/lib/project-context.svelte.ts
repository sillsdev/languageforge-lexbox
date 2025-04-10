import {getContext, setContext} from 'svelte';
import type {IMiniLcmFeatures, IMiniLcmJsInvokable} from '$lib/dotnet-types';
import type {
  IHistoryServiceJsInvokable
} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/IHistoryServiceJsInvokable';
import {type WritingSystemService} from '$lib/writing-system-service.svelte';
import {resource, type ResourceReturn} from 'runed';
import {SvelteMap} from 'svelte/reactivity';

const projectContextKey = 'current-project';
interface ProjectContextSetup {
  api: IMiniLcmJsInvokable;
  historyService?: IHistoryServiceJsInvokable;
  projectName: string;
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
  #lazy = new SvelteMap<symbol, unknown>();
  #api: IMiniLcmJsInvokable | undefined = $state(undefined);
  #projectName: string | undefined = $state(undefined);
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
  public get features(): IMiniLcmFeatures {
    return this.#features.current;
  }
  public historyService?: IHistoryServiceJsInvokable = $state(undefined);
  //not to be used directly, call useWritingSystemService instead
  public writingSystemService?: WritingSystemService;

  constructor(args?: ProjectContextSetup) {
    this.#api = args?.api;
    this.historyService = args?.historyService;
    this.#projectName = args?.projectName;
  }

  public setup(args: ProjectContextSetup) {
    this.#api = args.api;
    this.historyService = args.historyService;
    this.#projectName = args?.projectName;
  }

  public lazyLoad<T>(key: symbol, initialValue: T, factory: (api: IMiniLcmJsInvokable) => Promise<T>): ResourceReturn<T, unknown, true> {
    if (this.#lazy.has(key)) {
      return this.#lazy.get(key) as ResourceReturn<T, unknown, true>;
    }

    const res = resource<IMiniLcmJsInvokable | undefined>(() => this.#api,
      ((api) => {
        if (!api) return Promise.resolve(initialValue);
        return factory(api);
      }), {initialValue});
    this.#lazy.set(key, res);
    return res;
  }

  public lazyCreate<T>(key: symbol, factory: () => T) {
    if (this.#lazy.has(key)) {
      return this.#lazy.get(key) as T;
    }
    const result = factory();
    this.#lazy.set(key, result);
    return result;
  }
}
