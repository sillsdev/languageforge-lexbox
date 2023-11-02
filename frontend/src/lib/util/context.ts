import { getContext, setContext } from 'svelte';

interface ContextDefinition<T> {
  use: () => T;
  init: (value: T) => T;
}

export function defineContext<T>(key: string | symbol = Symbol(), onInit?: (value: T) => void): ContextDefinition<T> {
  return {
    use(): T {
      return getContext<T>(key);
    },
    init(value: T): T {
      setContext(key, value);
      onInit?.(value);
      return value;
    }
  }
}
