import {getContext, setContext} from 'svelte';
import type {IMiniLcmJsInvokable} from '$lib/dotnet-types';
import type {
  IHistoryServiceJsInvokable
} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/IHistoryServiceJsInvokable';
import {type WritingSystemService} from '$lib/writing-system-service.svelte';

const projectContextKey = 'current-project';
interface ProjectContextSetup {
  api: IMiniLcmJsInvokable;
  historyService?: IHistoryServiceJsInvokable;
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
  #api: IMiniLcmJsInvokable | undefined = $state(undefined);
  public get api() {
    if (!this.#api) throw new Error('api not setup yet');
    return this.#api;
  }
  public get maybeApi() {
    return this.#api;
  }
  public historyService?: IHistoryServiceJsInvokable;
  //not to be used directly, call useWritingSystemService instead
  public writingSystemService?: WritingSystemService;

  constructor(args?: ProjectContextSetup) {
    this.#api = args?.api;
    this.historyService = args?.historyService;
  }

  public setup(args: ProjectContextSetup) {
    this.#api = args.api;
    this.historyService = args.historyService;
  }
}
