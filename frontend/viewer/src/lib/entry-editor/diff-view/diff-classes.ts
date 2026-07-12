// Shared palette for the diff-view system. Every Diff* component uses these tokens so a change to the
// visual vocabulary (add saturation, swap red for orange, etc.) happens in exactly one place.
//
// Semantic tokens:
//   diffAdded    — text/value that was added by the change (emerald)
//   diffRemoved  — text/value that was removed (destructive red, strikethrough)
//   diffKept     — an item that survived the change unchanged, when rendered as a badge/token in a list
//   diffEmpty    — layout-preserving placeholder when neither before nor after has any value

// text-sm on badges (Badge's own default is text-xs) so values read as prominent data rather than
// muted metadata inside the DiffShell frame. Kept-items get an outline instead of a muted fill so
// they stand out cleanly against the shell background.
export const diffAdded = 'bg-emerald-500/15 text-emerald-700 dark:text-emerald-300 text-sm';
// Full-strength strikethrough so "removed" reads as struck-out, not just darker-red text (a /50 decoration
// is nearly invisible over the tinted background at this size).
export const diffRemoved = 'bg-destructive/10 text-destructive line-through decoration-destructive decoration-2 text-sm';
// Outline-only (no fill) so kept items read as data without competing with the colored add/remove badges.
// border-muted-foreground/20 keeps the outline visible in dark mode, where border-border nearly vanishes.
export const diffKept = 'bg-transparent text-foreground border-muted-foreground/20 text-sm';
// For an item that's still present (kept) but that this change specifically touched — e.g. the reordered
// component whose position changed. Adds a primary accent on top of the neutral kept style.
export const diffTouched = 'bg-transparent text-foreground border-primary/60 ring-2 ring-primary/60 text-sm';
export const diffEmpty = 'text-muted-foreground/40';

// Surface-only variants (background tint + no text styling) for wrapping non-text diff content — an audio
// player, an image later — where a text token like line-through would be nonsensical. Pair with a gutter
// border (border-destructive / border-emerald-500) to read as removed / added.
export const diffRemovedSurface = 'bg-destructive/10';
export const diffAddedSurface = 'bg-emerald-500/15';
