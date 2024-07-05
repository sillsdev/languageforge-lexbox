export function getAvailableHeightForElement(elem: HTMLElement): number {
  const occupiedSpaceTop = Math.max(0, elem.offsetTop - window.scrollY);
  const footerSize = document.body.scrollHeight - (elem.scrollHeight + elem.offsetTop);
  const currScrollFromBottom = document.body.scrollHeight - window.scrollY - window.innerHeight;
  const occupiedSpaceBottom = Math.max(0, footerSize - currScrollFromBottom);
  const occupiedSpace = occupiedSpaceTop + occupiedSpaceBottom;
  return window.innerHeight - occupiedSpace;
}
