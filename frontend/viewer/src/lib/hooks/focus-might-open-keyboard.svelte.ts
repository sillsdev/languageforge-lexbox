import {IsUsingKeyboard} from 'bits-ui';
import {MediaQuery} from 'svelte/reactivity';

const finePointer = new MediaQuery('pointer: fine');
// IsUsingKeyboard registers its listeners in an $effect, so it needs a long-lived root outside of any component
let usingKeyboard: IsUsingKeyboard | undefined;
$effect.root(() => {
  usingKeyboard = new IsUsingKeyboard();
});

/**
 * Whether moving focus into a text field would probably pop up the virtual keyboard: no fine pointer
 * (mouse/trackpad) and the user isn't currently driving the app with a keyboard. It's a guess; a touch
 * device with a hardware keyboard attached still reports true.
 *
 * Gate focus the user didn't ask for on this. The new entry dialog doesn't: typing is the point of it.
 * Screen size is deliberately not a factor: tablets and landscape phones are wide, but still touch-first.
 */
export class FocusMightOpenKeyboard {
  static get value(): boolean {
    return !finePointer.current && !usingKeyboard?.current;
  }
}
