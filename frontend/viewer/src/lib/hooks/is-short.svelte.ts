import {SHORT_BREAKPOINT} from '../../css-breakpoints';
import {MediaQuery} from 'svelte/reactivity';

export class IsShort extends MediaQuery {
  private constructor() {
    super(`max-height: ${SHORT_BREAKPOINT - 1}px`);
  }

  private static isShort = new IsShort();

  static get value(): boolean {
    return this.isShort.current;
  }
}
