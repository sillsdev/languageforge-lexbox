export async function delay<T>(ms: number): Promise<T> {
  return new Promise<T>(resolve => setTimeout(resolve, ms));
}

export const DEFAULT_DEBOUNCE_TIME = 300;
