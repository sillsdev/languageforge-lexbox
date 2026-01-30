import { getContext, setContext } from 'svelte';
import { watch } from 'runed';
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
export class StorageProp {
  #key: string;
  #storage: ProjectStorage;
  #value = $state<string>('');

  constructor(storage: ProjectStorage, key: string) {
    this.#key = key;
    this.#storage = storage;

    // Load initial value
    this.#value = this.#storage.get(this.#key) ?? '';

    // Watch for changes and sync to storage
    watch(() => this.#value, (value) => {
      if (value) {
        this.#storage.set(this.#key, value);
      } else {
        this.#storage.remove(this.#key);
      }
    });
  }

  get current(): string {
    return this.#value;
  }

  set current(value: string) {
    this.#value = value;
  }
}

export class ProjectStorage {
  #projectCode: string;

  readonly selectedTaskId: StorageProp;

  constructor(projectCode: string) {
    this.#projectCode = projectCode;
    this.selectedTaskId = new StorageProp(this, 'selectedTaskId');
  }

  /**
   * Get a project-specific storage key
   */
  getStorageKey(key: string): string {
    return `project:${this.#projectCode}:${key}`;
  }

  /**
   * Get a value from project-specific storage
   */
  get(key: string): string | null {
    const storageKey = this.getStorageKey(key);
    return localStorage.getItem(storageKey);
  }

  /**
   * Set a value in project-specific storage
   */
  set(key: string, value: string): void {
    const storageKey = this.getStorageKey(key);
    localStorage.setItem(storageKey, value);
  }

  /**
   * Remove a value from project-specific storage
   */
  remove(key: string): void {
    const storageKey = this.getStorageKey(key);
    localStorage.removeItem(storageKey);
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
