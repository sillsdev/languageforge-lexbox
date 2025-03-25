import {MediaQuery} from 'svelte/reactivity';
import { MOBILE_BREAKPOINT } from '../../../tailwind.config';

export class IsMobile extends MediaQuery {
  constructor() {
    super(`max-width: ${MOBILE_BREAKPOINT - 1}px`);
  }
}
