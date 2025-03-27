import {MOBILE_BREAKPOINT} from '../../css-breakpoints';
import {MediaQuery} from 'svelte/reactivity';

const isMobileMediaQuery = new MediaQuery(`max-width: ${MOBILE_BREAKPOINT - 1}px`);

export class IsMobile extends MediaQuery {
  private constructor() {
    super(`max-width: ${MOBILE_BREAKPOINT - 1}px`);
  }

  private static isMobile = new IsMobile();

  static get value(): boolean {
    return this.isMobile.current;
  }
}
