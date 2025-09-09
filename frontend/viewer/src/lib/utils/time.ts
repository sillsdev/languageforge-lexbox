export async function delay(ms: number): Promise<void> {
  return new Promise(resolve => setTimeout(resolve, ms));
}

export const DEFAULT_DEBOUNCE_TIME = 300;
