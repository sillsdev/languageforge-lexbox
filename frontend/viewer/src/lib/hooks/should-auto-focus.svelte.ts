import {IsUsingKeyboard} from 'bits-ui';
import {MediaQuery} from 'svelte/reactivity';

const finePointer = new MediaQuery('pointer: fine');
// IsUsingKeyboard registers its listeners in an $effect, so it needs a long-lived root outside of any component
let usingKeyboard: IsUsingKeyboard | undefined;
$effect.root(() => {
  usingKeyboard = new IsUsingKeyboard();
});

/**
 * Whether it's appropriate to move focus into a text field without the user asking for it.
 * On touch devices that would pop up the virtual keyboard, so we only do it when the primary pointer
 * is fine (mouse/trackpad) or the user is currently driving the app with a (presumably physical) keyboard.
 * Screen size is deliberately not a factor: tablets and landscape phones are wide, but still touch-first.
 */
export class ShouldAutoFocus {
  static get value(): boolean {
    return finePointer.current || !!usingKeyboard?.current;
  }
}
