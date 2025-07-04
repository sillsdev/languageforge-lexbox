import {isTabbable, tabbable} from 'tabbable';

// Use 'none' for tests (jsdom) and 'full' for production
const displayCheck: 'none' | 'full' = typeof process !== 'undefined' && process.env?.NODE_ENV === 'test' ? 'none' : 'full';

export function findFirstTabbable(container: Element | null | undefined): HTMLElement | undefined {
  if (!container) {
    return undefined;
  }
  const walker = document.createTreeWalker(
    container,
    NodeFilter.SHOW_ELEMENT,
    {
      acceptNode: node => isTabbable(node as HTMLElement, {displayCheck})
        ? NodeFilter.FILTER_ACCEPT
        : NodeFilter.FILTER_SKIP
    }
  );
  return walker.nextNode() as HTMLElement;
}

export function findNextTabbable(
  current: HTMLElement | SVGElement | null | undefined
): HTMLElement | SVGElement | undefined {
  if (!current?.parentElement) {
    return current ?? undefined;
  }

  if (!isTabbable(current, {displayCheck})) {
    throw new Error('Current element is not tabbable, so can\'t find relative tabbable element');
  }

  let container = current.parentElement;
  let tabbables = tabbable(container, {displayCheck});
  while (tabbables.length === 0 // should never happen, but whatever
    || tabbables.at(-1) === current // we're still last, so haven't found a "next" yet
  ) {
    if (!container.parentElement) return tabbables[0]; // loop back to first
    container = container.parentElement;
    tabbables = tabbable(container, {displayCheck});
  }

  const currentIndex = tabbables.indexOf(current);
  if (currentIndex === -1) {
    throw new Error('Current tabbable element should always be in an ancestor\'s list of tabbables');
  }

  return tabbables[currentIndex + 1] ??
    tabbables[0]; // loop back to first
}
