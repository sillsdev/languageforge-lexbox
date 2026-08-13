import { createContext } from 'svelte';

interface ContextConfig<T> {
  onInit?: (value: T) => void
}

interface ContextDefinition<T, P extends unknown[]> {
  use: () => T;
  init: (...args: P) => T;
}

export function defineContext<T, P extends unknown[] = []>(
  initializer: (...args: P) => T,
  { onInit }: Partial<ContextConfig<T>> = {},
): ContextDefinition<T, P> {
  const [use, set] = createContext<T>();
  return {
    use,
    init(...args: P): T {
      const value = initializer(...args);
      set(value);
      onInit?.(value);
      return value;
    }
  }
}
