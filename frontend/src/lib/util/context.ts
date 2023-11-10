import { getContext, setContext } from 'svelte';

interface ContextConfig<T> {
  key: string | symbol,
  onInit?: (value: T) => void
}

interface ContextDefinition<T, P extends unknown[]> {
  use: () => T;
  init: (...args: P) => T;
}

export function defineContext<T, P extends unknown[] = []>(
  initializer: (...args: P) => T,
  { key = Symbol(), onInit }: Partial<ContextConfig<T>> = {},
): ContextDefinition<T, P> {
  return {
    use(): T {
      return getContext<T>(key);
    },
    init(...args: P): T {
      const value = initializer(...args);
      setContext(key, value);
      onInit?.(value);
      return value;
    }
  }
}
