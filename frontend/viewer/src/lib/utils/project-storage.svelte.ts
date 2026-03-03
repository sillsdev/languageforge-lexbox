import {getContext, setContext} from 'svelte';

import type {IPreferencesService} from '$lib/dotnet-types/generated-types/FwLiteShared/Services';
import {usePreferencesService} from '$lib/services/service-provider';
import {useProjectContext} from '$project/project-context.svelte';

const projectStorageContextKey = 'project-storage';

/**
 * Reactive storage property with async persistence.
 *
 * - `current` getter is reactive (Svelte 5 runes) and read-only
 * - `set()` is async - callers can await or fire-and-forget
 * - Initial value loads asynchronously; early subscribers get updates when ready
 */
class StorageProp {
  #projectCode: string;
  #key: string;
  #backend: IPreferencesService;
  #value = $state<string>('');
  #hasBeenSet = $state(false);

  constructor(projectCode: string, key: string, backend: IPreferencesService) {
    this.#projectCode = projectCode;
    this.#key = key;
    this.#backend = backend;
    void this.load();
  }

  get current(): string {
    return this.#value;
  }

  get loading(): boolean {
    return !this.#hasBeenSet;
  }

  async set(value: string): Promise<void> {
    this.#hasBeenSet = true;
    this.#value = value;
    const storageKey = this.getStorageKey();
    if (value) {
      await this.#backend.set(storageKey, value);
    } else {
      await this.#backend.remove(storageKey);
    }
  }

  private getStorageKey(): string {
    return `project:${this.#projectCode}:${this.#key}`;
  }

  private async load(): Promise<void> {
    const value = await this.#backend.get(this.getStorageKey());
    if (!this.#hasBeenSet) {
      this.#value = value ?? '';
      this.#hasBeenSet = true;
    }
  }
}

export class ProjectStorage {
  readonly selectedTaskId: StorageProp;

  constructor(projectCode: string, backend: IPreferencesService) {
    this.selectedTaskId = new StorageProp(projectCode, 'selectedTaskId', backend);
  }
}

export function useProjectStorage(): ProjectStorage {
  let storage = getContext<ProjectStorage>(projectStorageContextKey);
  if (!storage) {
    const projectContext = useProjectContext();
    const backend = usePreferencesService();
    storage = new ProjectStorage(projectContext.projectCode, backend);
    setContext(projectStorageContextKey, storage);
  }
  return storage;
}
