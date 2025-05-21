import {isTabbable} from 'tabbable';

export function findFirstTabbable(container: HTMLElement | null | undefined): HTMLElement | undefined {
  if (!container) {
    return undefined;
  }
  const walker = document.createTreeWalker(
    container,
    NodeFilter.SHOW_ELEMENT,
    {
      acceptNode: node => {
        console.log(node);
        return isTabbable(node as HTMLElement, {displayCheck: 'full'})
          ? NodeFilter.FILTER_ACCEPT
          : NodeFilter.FILTER_SKIP;
      }
    }
  );
  return walker.nextNode() as HTMLElement;
}
