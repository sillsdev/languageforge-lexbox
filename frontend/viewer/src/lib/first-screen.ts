let markReady: () => void;
const ready = new Promise<void>((resolve) => markReady = resolve);

/**
 * Called by whichever page opens first once it has something better than a loading placeholder to show.
 * A native host waits for this (briefly) before swapping its splash screen for the window.
 */
export function markFirstScreenReady(): void {
  // two frames: the first is scheduled before the new content is painted, the second after
  requestAnimationFrame(() => requestAnimationFrame(markReady));
}

export function whenFirstScreenReady(): Promise<void> {
  return ready;
}
