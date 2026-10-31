import {getContext, setContext} from 'svelte';
import {SvelteMap} from 'svelte/reactivity';

const key = Symbol('diff-focus');

/**
 * Set by a diff primitive in 'changes' mode, so its multi-string fields hide their unchanged writing systems
 * while collapsed and report how many values collapsing hides: the "Show unchanged fields" toggle counts them too.
 */
export class DiffFocus {
  readonly #unchanged = new SvelteMap<symbol, number>();
  readonly #isCollapsed: () => boolean;

  constructor(isCollapsed: () => boolean) {
    this.#isCollapsed = isCollapsed;
  }

  get collapsed(): boolean {
    return this.#isCollapsed();
  }

  get unchangedValues(): number {
    let total = 0;
    for (const count of this.#unchanged.values()) total += count;
    return total;
  }

  report(field: symbol, unchangedValues: number): void {
    if (unchangedValues) this.#unchanged.set(field, unchangedValues);
    else this.#unchanged.delete(field);
  }
}

export function provideDiffFocus(isCollapsed: () => boolean): DiffFocus {
  return setContext(key, new DiffFocus(isCollapsed));
}

export function useDiffFocus(): DiffFocus | undefined {
  return getContext<DiffFocus | undefined>(key);
}
