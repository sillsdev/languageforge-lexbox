/** True on macOS / iOS (for Cmd vs Ctrl shortcut labels and matching). */
export function isApplePlatform(): boolean {
  if (typeof navigator === 'undefined') return false;
  const uaDataPlatform = (navigator as Navigator & {userAgentData?: {platform?: string}}).userAgentData
    ?.platform;
  if (uaDataPlatform) return /mac|iphone|ipad|ipod/i.test(uaDataPlatform);
  // Fallback for browsers without userAgentData (Firefox, older Safari).
  return /Mac|iPhone|iPad|iPod/i.test(navigator.platform);
}

/** Primary modifier for app shortcuts: Cmd on Apple, Ctrl elsewhere. */
export function hasPrimaryModifier(e: KeyboardEvent): boolean {
  if (e.altKey || e.shiftKey) return false;
  return isApplePlatform() ? e.metaKey : e.ctrlKey;
}
