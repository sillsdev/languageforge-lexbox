import { getContext, setContext } from 'svelte';
import { useProjectContext } from '$project/project-context.svelte';
import { tryUsePreferencesService } from '$lib/services/service-provider';
import type { IPreferencesService } from '$lib/dotnet-types/generated-types/FwLiteShared/Services/IPreferencesService';

/**
 * Project-specific storage service
 *
 * This service provides project-scoped storage for user preferences.
 * When running in MAUI, uses MAUI Preferences via the PreferencesService.
 * Otherwise, falls back to localStorage.
 */

const projectStorageContextKey = 'project-storage';

/**
 * Storage backend abstraction
 */
interface StorageBackend {
  get(key: string): Promise<string | null>;
  set(key: string, value: string): Promise<void>;
  remove(key: string): Promise<void>;
}

/**
 * localStorage-based storage backend
 */
class LocalStorageBackend implements StorageBackend {
  async get(key: string): Promise<string | null> {
    return localStorage.getItem(key);
  }

  async set(key: string, value: string): Promise<void> {
    localStorage.setItem(key, value);
  }

  async remove(key: string): Promise<void> {
    localStorage.removeItem(key);
  }
}

/**
 * MAUI Preferences-based storage backend
 */
class PreferencesBackend implements StorageBackend {
  constructor(private preferencesService: IPreferencesService) {}

  async get(key: string): Promise<string | null> {
    return this.preferencesService.get(key);
  }

  async set(key: string, value: string): Promise<void> {
    await this.preferencesService.set(key, value);
  }

  async remove(key: string): Promise<void> {
    await this.preferencesService.remove(key);
  }
}

/**
 * Creates the appropriate storage backend based on environment
 */
function createStorageBackend(): StorageBackend {
  const preferencesService = tryUsePreferencesService();
  if (preferencesService) {
    return new PreferencesBackend(preferencesService);
  }
  return new LocalStorageBackend();
}

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
  #backend: StorageBackend;
  #value = $state<string>('');

  constructor(projectCode: string, key: string, backend: StorageBackend) {
    this.#projectCode = projectCode;
    this.#key = key;
    this.#backend = backend;
    this.load();
  }

  get current(): string {
    return this.#value;
  }

  async set(value: string): Promise<void> {
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

  private load(): void {
    this.#backend.get(this.getStorageKey())
      .then(value => { this.#value = value ?? ''; })
      .catch(e => console.error(`Failed to load preference ${this.#key}:`, e));
  }
}

export class ProjectStorage {
  readonly selectedTaskId: StorageProp;

  constructor(projectCode: string, backend: StorageBackend) {
    this.selectedTaskId = new StorageProp(projectCode, 'selectedTaskId', backend);
  }
}

export function useProjectStorage(): ProjectStorage {
  let storage = getContext<ProjectStorage>(projectStorageContextKey);
  if (!storage) {
    const projectContext = useProjectContext();
    const backend = createStorageBackend();
    storage = new ProjectStorage(projectContext.projectCode, backend);
    setContext(projectStorageContextKey, storage);
  }
  return storage;
}
