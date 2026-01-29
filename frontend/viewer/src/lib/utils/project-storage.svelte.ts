/**
 * Project-specific storage utility
 *
 * This utility provides project-scoped storage for user preferences.
 * Currently uses localStorage with project-prefixed keys.
 *
 * TODO: Enhance to use MAUI Preferences when running in MAUI app
 * (detect platform via useFwLiteConfig().os and use a preferences service)
 */

/**
 * Get a project-specific storage key
 */
export function getProjectStorageKey(projectCode: string, key: string): string {
  return `project:${projectCode}:${key}`;
}

/**
 * Get a value from project-specific storage
 */
export function getProjectStorage(projectCode: string, key: string): string | null {
  const storageKey = getProjectStorageKey(projectCode, key);
  return localStorage.getItem(storageKey);
}

/**
 * Set a value in project-specific storage
 */
export function setProjectStorage(projectCode: string, key: string, value: string): void {
  const storageKey = getProjectStorageKey(projectCode, key);
  localStorage.setItem(storageKey, value);
}

/**
 * Remove a value from project-specific storage
 */
export function removeProjectStorage(projectCode: string, key: string): void {
  const storageKey = getProjectStorageKey(projectCode, key);
  localStorage.removeItem(storageKey);
}

/**
 * Clear all storage for a specific project
 */
export function clearProjectStorage(projectCode: string): void {
  const prefix = `project:${projectCode}:`;
  const keysToRemove: string[] = [];

  for (let i = 0; i < localStorage.length; i++) {
    const key = localStorage.key(i);
    if (key && key.startsWith(prefix)) {
      keysToRemove.push(key);
    }
  }

  keysToRemove.forEach(key => localStorage.removeItem(key));
}
