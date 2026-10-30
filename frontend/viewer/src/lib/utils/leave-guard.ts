/**
 * Lets a page with unsaved work ask before the app navigates away. svelte-routing has no
 * navigation guards, so in-app navigation that can leave such a page (the sidebar) awaits
 * {@link confirmLeave} first. Only one page registers at a time.
 */
type LeaveGuard = () => Promise<boolean>;

let currentGuard: LeaveGuard | undefined;

/** Registers the guard; returns its unregister function (use it as an effect teardown). */
export function setLeaveGuard(guard: LeaveGuard): () => void {
  currentGuard = guard;
  return () => {
    if (currentGuard === guard) currentGuard = undefined;
  };
}

/** Resolves true when there's nothing to lose or the user agreed to leave. */
export function confirmLeave(): Promise<boolean> {
  return currentGuard?.() ?? Promise.resolve(true);
}
