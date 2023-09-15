import type { Action } from 'svelte/action';

export const bubbleFocusOnDestroy: Action = (node) => {
  const focusableAncestor = node.closest('[tabindex]') as { focus?: HTMLOrSVGElement['focus'] } | null;

  function getHasFocus(): boolean {
    return node.contains(document.activeElement) || node === document.activeElement;
  }

  /**
   * the destroy callback gets called after the element has already lost focus/been removed,
   * so we need to keep track of wether it had focus before/when it was removed
   */
  let hadFocus = getHasFocus();

  document.addEventListener('focusin', () => {
    // focus is moved to the body when the element is removed
    if (hadFocus && document.activeElement === document.body) {
      setTimeout(() => hadFocus = false);
    } else {
      hadFocus = getHasFocus();
    }
  });

  return {
    destroy: () => {
      if (hadFocus) {
        focusableAncestor?.focus?.();
      }
    }
  };
}
