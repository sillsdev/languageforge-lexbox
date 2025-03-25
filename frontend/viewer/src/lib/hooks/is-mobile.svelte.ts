import {MOBILE_BREAKPOINT} from '../../css-breakpoints';
import {MediaQuery} from 'svelte/reactivity';

export class IsMobile extends MediaQuery {
  constructor() {
    super(`max-width: ${MOBILE_BREAKPOINT - 1}px`);
  }

  private static isMobile = new IsMobile();

  static get value(): boolean {
    return this.isMobile.current;
  }
}
