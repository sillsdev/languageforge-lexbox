/** From this many options a select gets a filter box and, on phones, a drawer instead of a popover */
export const LONG_LIST = 10;

/** Row height shared by every select and multi-select option */
export const OPTION_ROW = 'min-h-12 md:min-h-9';

/**
 * Short lists have no filter input, so focus goes to the listbox itself, and screen readers need
 * aria-activedescendant there to announce the option bits-ui highlights (it only sets it on the input).
 */
export function trackActiveDescendant(list: HTMLElement): () => void {
  function update() {
    const id = list.querySelector('[role="option"][aria-selected="true"]')?.id;
    if (id) list.setAttribute('aria-activedescendant', id);
    else list.removeAttribute('aria-activedescendant');
  }
  const observer = new MutationObserver(update);
  observer.observe(list, {subtree: true, childList: true, attributes: true, attributeFilter: ['aria-selected']});
  update();
  return () => observer.disconnect();
}

/** Whether Tab moves between the filter input and its clear button rather than out of the list */
export function tabStaysInFilter(e: KeyboardEvent): boolean {
  const wrapper = (e.target as Element).closest('[data-slot="command-input-wrapper"]');
  if (!wrapper) return false;
  const focusables = [...wrapper.querySelectorAll<HTMLElement>('input, button')];
  const i = focusables.indexOf(e.target as HTMLElement);
  return e.shiftKey ? i > 0 : i < focusables.length - 1;
}
