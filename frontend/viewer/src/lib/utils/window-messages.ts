/**
 * Whether a message came from a frame inside this document, e.g. a plugin's sandboxed iframe.
 * Window listeners that act on messages (the host app's notification event) must ignore those.
 * `parent` is readable across origins, so this works for an opaque-origin frame too.
 */
export function isFrameInThisDocument(source: MessageEventSource | null): boolean {
  if (!source || source === window || !('parent' in source)) return false;
  for (let ancestor = source.parent; ancestor; ancestor = ancestor.parent) {
    if (ancestor === window) return true;
    if (ancestor.parent === ancestor) return false;
  }
  return false;
}
