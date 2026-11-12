import {IsMobile} from '$lib/hooks/is-mobile.svelte';

/**
 * Whether a keydown in a form dialog should submit it: a plain Enter typed into a text field, on desktop.
 * Enter on a button or picker keeps its own action (e.g. Cancel), Shift+Enter is a line break in rich text,
 * and on mobile Enter moves to the next field.
 */
export function isSubmitEnter(event: KeyboardEvent): boolean {
  if (event.key !== 'Enter' || event.shiftKey || IsMobile.value) return false;
  // Enter that confirms an IME/Keyman composition; older Safari reports it as keyCode 229 without isComposing
  if (event.isComposing || event.keyCode === 229) return false;
  const target = event.target;
  return target instanceof HTMLInputElement || (target instanceof HTMLElement && target.isContentEditable);
}
