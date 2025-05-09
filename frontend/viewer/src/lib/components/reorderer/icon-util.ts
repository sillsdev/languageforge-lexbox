import type {IconClass} from '$lib/icon-class';

export function pickIcon(direction: 'horizontal' | 'vertical', first = false, last = false): IconClass {
  if (direction === 'horizontal') {
    return first ? 'i-mdi-arrow-right-bold' : last ? 'i-mdi-arrow-left-bold' : 'i-mdi-arrow-left-right-bold';
  } else {
    return first ? 'i-mdi-arrow-down-bold' : last ? 'i-mdi-arrow-up-bold' : 'i-mdi-arrow-up-down-bold';
  }
}
