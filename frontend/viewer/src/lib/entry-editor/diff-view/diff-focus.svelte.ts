import {getContext, setContext} from 'svelte';
import {SvelteMap} from 'svelte/reactivity';

const key = Symbol('diff-focus');

/**
 * Set by a diff primitive in 'changes' mode, so its multi-string fields hide their unchanged writing systems
 * while collapsed and report how many values that hides: the "Show unchanged fields" toggle counts them too.
 */
export class DiffFocus {
  readonly #hidden = new SvelteMap<symbol, number>();
  readonly #isCollapsed: () => boolean;

  constructor(isCollapsed: () => boolean) {
    this.#isCollapsed = isCollapsed;
  }

  get collapsed(): boolean {
    return this.#isCollapsed();
  }

  get hiddenValues(): number {
    let total = 0;
    for (const count of this.#hidden.values()) total += count;
    return total;
  }

  report(field: symbol, hiddenValues: number): void {
    if (hiddenValues) this.#hidden.set(field, hiddenValues);
    else this.#hidden.delete(field);
  }
}

export function provideDiffFocus(isCollapsed: () => boolean): DiffFocus {
  return setContext(key, new DiffFocus(isCollapsed));
}

export function useDiffFocus(): DiffFocus | undefined {
  return getContext<DiffFocus | undefined>(key);
}
