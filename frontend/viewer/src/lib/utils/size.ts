export function getAvailableHeightForElement(elem: HTMLElement): number {
  const elementIsBiggerThanScreenSpace = (elem.scrollHeight + elem.offsetTop) > document.body.clientHeight;
  // Only grow if we need to, otherwise it looks weird when a footer gets pushed down
  const growWithScroll = elementIsBiggerThanScreenSpace ? window.scrollY : 0;
  return window.innerHeight - Math.max(0, elem.offsetTop - growWithScroll);
}
