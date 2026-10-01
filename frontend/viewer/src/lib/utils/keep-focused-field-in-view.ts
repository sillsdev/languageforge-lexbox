/**
 * The virtual keyboard shrinks the viewport well after a field gets focus (on Android ~700ms later, over
 * several resize events) and the browser doesn't re-scroll the focused field for that, so a field near the
 * bottom ends up under the keyboard. This covers autofocus and manual taps alike.
 */
export function keepFocusedFieldInView() {
  window.addEventListener('resize', () => {
    const field = document.activeElement;
    if (!isTextField(field)) return;
    const {top, bottom} = field.getBoundingClientRect();
    if (top >= 0 && bottom <= window.innerHeight) return;
    field.scrollIntoView({block: 'center'});
  });
}

export function isTextField(target: EventTarget | null): target is HTMLElement {
  return target instanceof HTMLElement && (target.isContentEditable || target.matches('input, textarea'));
}
