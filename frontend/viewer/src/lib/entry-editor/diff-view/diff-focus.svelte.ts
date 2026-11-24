import {getContext, setContext} from 'svelte';
import {SvelteMap} from 'svelte/reactivity';

const key = Symbol('diff-focus');

/**
 * A diff primitive's narrowing to its changes, shared with its multi-string fields: they hide their unchanged
 * writing systems while collapsed and report how many values that hides, for the "Show unchanged fields" toggle.
 * Not persisted: each diff starts collapsed.
 */
export class DiffFocus {
  expanded = $state(false);
  readonly #unchanged = new SvelteMap<symbol, number>();
  readonly #isActive: () => boolean;

  /** `isActive`: whether there's anything to narrow to, i.e. 'changes' mode with a visible change */
  constructor(isActive: () => boolean) {
    this.#isActive = isActive;
  }

  get active(): boolean {
    return this.#isActive();
  }

  get collapsed(): boolean {
    return this.active && !this.expanded;
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

export function provideDiffFocus(isActive: () => boolean): DiffFocus {
  return setContext(key, new DiffFocus(isActive));
}

export function useDiffFocus(): DiffFocus | undefined {
  return getContext<DiffFocus | undefined>(key);
}
