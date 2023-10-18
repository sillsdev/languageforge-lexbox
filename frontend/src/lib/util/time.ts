export const enum Duration {
  Default = 5000,
  Medium = 10000,
  Long = 15000,
}

export async function delay<T>(ms = Duration.Default): Promise<T> {
  return new Promise<T>(resolve => setTimeout(resolve, ms));
}

// eslint-disable-next-line @typescript-eslint/no-explicit-any
export function debounce<P extends any[]>(fn: (...args: P) => void, debounceTime: number): (...args: P) => void {
  let timeout: ReturnType<typeof setTimeout>;
  return (...args: P) => {
    clearTimeout(timeout);
    timeout = setTimeout(() => fn(...args), debounceTime);
  };
}
