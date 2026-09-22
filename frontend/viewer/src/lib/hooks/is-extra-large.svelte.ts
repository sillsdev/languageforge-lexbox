import {EXTRA_LARGE_BREAKPOINT} from '../../css-breakpoints';
import {MediaQuery} from 'svelte/reactivity';

export class IsExtraLarge extends MediaQuery {
  private constructor() {
    super(`min-width: ${EXTRA_LARGE_BREAKPOINT}px`);
  }

  private static isExtraLarge = new IsExtraLarge();

  static get value(): boolean {
    return this.isExtraLarge.current;
  }
}
