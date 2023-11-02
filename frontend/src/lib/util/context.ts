import { getContext, setContext } from 'svelte';

interface ContextDefinition<T> {
  use: () => T;
  init: (value: T) => T;
}

export function defineContext<T>(key: string | symbol = Symbol()): ContextDefinition<T> {
  return {
    use(): T {
      return getContext<T>(key);
    },
    init(value: T): T {
      setContext(key, value);
      return value;
    }
  }
}
