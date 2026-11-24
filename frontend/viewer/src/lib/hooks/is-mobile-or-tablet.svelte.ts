import {TABLET_BREAKPOINT} from '../../css-breakpoints';
import {MediaQuery} from 'svelte/reactivity';

/**
 * True on phones and tablets (below the Tailwind `lg` breakpoint).
 * Use this for chrome that should pop over the content on touch-sized screens,
 * while `IsMobile` stays the switch for single-pane layouts.
 */
export class IsMobileOrTablet extends MediaQuery {
  private constructor() {
    super(`max-width: ${TABLET_BREAKPOINT - 1}px`);
  }

  private static isMobileOrTablet = new IsMobileOrTablet();

  static get value(): boolean {
    return this.isMobileOrTablet.current;
  }
}
