import {MediaQuery} from 'svelte/reactivity';
import {MOBILE_BREAKPOINT} from '../../css-breakpoints';

export class IsMobile extends MediaQuery {
  constructor() {
    super(`max-width: ${MOBILE_BREAKPOINT - 1}px`);
  }
}
