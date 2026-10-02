import {DotnetService} from '$lib/dotnet-types';

/**
 * Android and iOS report whether a hardware keyboard is attached (iOS once at app start, Android on every
 * activity recreation, which attaching or detaching one triggers); desktop and the browser host always have
 * one. Without one, focusing a field pops up the virtual keyboard, so focus the user didn't ask for is gated
 * on this, as are keyboard-shortcut hints. The new entry dialog isn't gated: typing is the point of it.
 * Screen size is deliberately not a factor: tablets and landscape phones are wide, but still touch-first.
 */
export class HasHardwareKeyboard {
  static get value(): boolean {
    return window.lexbox.ServiceProvider.tryGetService(DotnetService.FwLiteConfig)?.hasHardwareKeyboard ?? true;
  }
}
