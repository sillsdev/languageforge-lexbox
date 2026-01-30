import { getContext, setContext } from 'svelte';
import { useProjectContext } from '$project/project-context.svelte';

/**
 * Project-specific storage service
 *
 * This service provides project-scoped storage for user preferences.
 * Currently uses localStorage with project-prefixed keys.
 *
 * TODO: Enhance to use MAUI Preferences when running in MAUI app
 * (detect platform via useFwLiteConfig().os and use a preferences service)
 */

const projectStorageContextKey = 'project-storage';

/**
 * Reactive storage property that automatically syncs to localStorage
 */
class StorageProp {
  #projectCode: string;
  #key: string;
  #value = $state<string>('');

  constructor(projectCode: string, key: string) {
    this.#projectCode = projectCode;
    this.#key = key;
    // Load initial value
    this.#value = this.load();
  }

  private getStorageKey(): string {
    return `project:${this.#projectCode}:${this.#key}`;
  }

  private load(): string {
    return localStorage.getItem(this.getStorageKey()) ?? '';
  }

  private persist(value: string): void {
    const storageKey = this.getStorageKey();
    if (value) {
      localStorage.setItem(storageKey, value);
    } else {
      localStorage.removeItem(storageKey);
    }
  }

  get current(): string {
    return this.#value;
  }

  set current(value: string) {
    this.#value = value;
    this.persist(value);
  }
}

export class ProjectStorage {
  #projectCode: string;

  readonly selectedTaskId: StorageProp;

  constructor(projectCode: string) {
    this.#projectCode = projectCode;
    this.selectedTaskId = new StorageProp(projectCode, 'selectedTaskId');
  }
}

export function useProjectStorage(): ProjectStorage {
  let storage = getContext<ProjectStorage>(projectStorageContextKey);
  if (!storage) {
    const projectContext = useProjectContext();
    storage = new ProjectStorage(projectContext.projectCode);
    setContext(projectStorageContextKey, storage);
  }
  return storage;
}
