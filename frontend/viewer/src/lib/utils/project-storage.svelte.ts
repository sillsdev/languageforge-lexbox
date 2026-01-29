import { getContext, setContext } from 'svelte';
import { watch } from 'runed';

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

export class ProjectStorage {
  #projectCode: string;

  selectedTaskId = $state<string>('');

  constructor(projectCode: string) {
    this.#projectCode = projectCode;

    // Load initial values from storage
    this.selectedTaskId = this.get('selectedTaskId') ?? '';

    // Watch for changes and sync to storage
    watch(() => this.selectedTaskId, (value) => {
      if (value) {
        this.set('selectedTaskId', value);
      } else {
        this.remove('selectedTaskId');
      }
    });
  }

  /**
   * Get a project-specific storage key
   */
  private getStorageKey(key: string): string {
    return `project:${this.#projectCode}:${key}`;
  }

  /**
   * Get a value from project-specific storage
   */
  private get(key: string): string | null {
    const storageKey = this.getStorageKey(key);
    return localStorage.getItem(storageKey);
  }

  /**
   * Set a value in project-specific storage
   */
  private set(key: string, value: string): void {
    const storageKey = this.getStorageKey(key);
    localStorage.setItem(storageKey, value);
  }

  /**
   * Remove a value from project-specific storage
   */
  private remove(key: string): void {
    const storageKey = this.getStorageKey(key);
    localStorage.removeItem(storageKey);
  }

  /**
   * Clear all storage for this project
   */
  public clear(): void {
    const prefix = `project:${this.#projectCode}:`;
    const keysToRemove: string[] = [];

    for (let i = 0; i < localStorage.length; i++) {
      const key = localStorage.key(i);
      if (key && key.startsWith(prefix)) {
        keysToRemove.push(key);
      }
    }

    keysToRemove.forEach(key => localStorage.removeItem(key));
  }
}

export function initProjectStorage(projectCode: string): ProjectStorage {
  const storage = new ProjectStorage(projectCode);
  setContext(projectStorageContextKey, storage);
  return storage;
}

export function useProjectStorage(): ProjectStorage {
  const storage = getContext<ProjectStorage>(projectStorageContextKey);
  if (!storage) throw new Error('ProjectStorage is not initialized. Are you in the context of a project?');
  return storage;
}
