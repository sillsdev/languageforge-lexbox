import {useDebounce, useEventListener} from 'runed';

/**
 * The virtual keyboard shrinks the viewport well after a field gets focus (on Android ~700ms later, over
 * several resize events) and the browser doesn't re-scroll the focused field for that, so a field near the
 * bottom ends up under the keyboard. Covers autofocus and manual taps alike.
 * Only a field the resize itself pushed out of view gets scrolled back; one the user scrolled away from stays put.
 */
export function keepFocusedFieldInView() {
  let heightBefore = window.innerHeight;
  useEventListener(window, 'resize', useDebounce(() => {
    const heightAfter = window.innerHeight;
    const field = document.activeElement;
    if (isTextField(field)) {
      // a height change doesn't move the content above the field, so the old height says where the field was
      const {top, bottom} = field.getBoundingClientRect();
      const wasInView = top >= 0 && bottom <= heightBefore;
      if (wasInView && bottom > heightAfter) field.scrollIntoView({block: 'center'});
    }
    heightBefore = heightAfter;
  }, 100));
}

function isTextField(target: EventTarget | null): target is HTMLElement {
  return target instanceof HTMLElement && (target.isContentEditable || target.matches('input, textarea'));
}
