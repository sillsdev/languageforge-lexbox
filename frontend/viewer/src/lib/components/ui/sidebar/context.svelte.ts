import {createContext} from 'svelte';

import {IsMobileOrTablet} from '$lib/hooks/is-mobile-or-tablet.svelte';
import {SIDEBAR_KEYBOARD_SHORTCUT} from './constants';

type Getter<T> = () => T;

export type SidebarStateProps = {
  /**
   * A getter function that returns the current open state of the sidebar.
   * We use a getter function here to support `bind:open` on the `Sidebar.Provider`
   * component.
   */
  open: Getter<boolean>;

  /**
   * A function that sets the open state of the sidebar. To support `bind:open`, we need
   * a source of truth for changing the open state to ensure it will be synced throughout
   * the sub-components and any `bind:` references.
   */
  setOpen: (open: boolean) => void;
};

class SidebarState {
  readonly props: SidebarStateProps;
  open = $derived.by(() => this.props.open());
  openOverlay = $state(false);
  setOpen: SidebarStateProps['setOpen'];
  state = $derived.by(() => (this.open ? 'expanded' : 'collapsed'));

  constructor(props: SidebarStateProps) {
    this.setOpen = props.setOpen;
    this.props = props;
  }

  // Whether the sidebar pops over the content as a sheet (phones and tablets) instead of docking beside it
  // without this, callers would need to know which media query the sidebar uses
  get isOverlay() {
    return IsMobileOrTablet.value;
  }

  // Event handler to apply to the `<svelte:window>`
  handleShortcutKeydown = (e: KeyboardEvent) => {
    if (e.key === SIDEBAR_KEYBOARD_SHORTCUT && (e.metaKey || e.ctrlKey)) {
      e.preventDefault();
      this.toggle();
    }
  };

  setOpenOverlay = (value: boolean) => {
    this.openOverlay = value;
  };

  toggle = () => {
    return this.isOverlay ? (this.openOverlay = !this.openOverlay) : this.setOpen(!this.open);
  };
}

const [useSidebarContext, setSidebarContext] = createContext<SidebarState>();

/**
 * Instantiates a new `SidebarState` instance and sets it in the context.
 *
 * @param props The constructor props for the `SidebarState` class.
 * @returns  The `SidebarState` instance.
 */
export function setSidebar(props: SidebarStateProps): SidebarState {
  return setSidebarContext(new SidebarState(props));
}

/**
 * Retrieves the `SidebarState` instance from the context. This is a class instance,
 * so you cannot destructure it.
 * @returns The `SidebarState` instance.
 */
export function useSidebar(): SidebarState {
  return useSidebarContext();
}
