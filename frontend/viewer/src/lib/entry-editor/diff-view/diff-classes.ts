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
export const diffRemoved = 'bg-destructive/10 text-destructive line-through decoration-destructive/50 text-sm';
export const diffKept = 'bg-transparent text-foreground border-border text-sm';
// For an item that's still present (kept) but that this change specifically touched — e.g. the reordered
// component whose position changed. Adds a primary accent on top of the neutral kept style.
export const diffTouched = 'bg-transparent text-foreground border-primary/60 ring-2 ring-primary/60 text-sm';
export const diffEmpty = 'text-muted-foreground/40';
