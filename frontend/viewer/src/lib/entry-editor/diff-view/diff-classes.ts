// The one palette every Diff* leaf uses, so the diff's visual vocabulary changes in one place.
// No sizes here: the tokens also color inline text segments, which must keep the surrounding size.

export const diffAdded = 'bg-emerald-500/15 text-emerald-700 dark:text-emerald-300';
// Full-strength decoration: a faded strikethrough disappears into the tinted background.
export const diffRemoved = 'bg-destructive/10 text-destructive line-through decoration-destructive decoration-2';
// Outline only, so kept badges don't compete with the colored ones. /20 stays visible in dark mode, where border-border vanishes.
export const diffKept = 'bg-transparent text-foreground border-muted-foreground/20';
// A kept item that this change references, e.g. the reordered component.
export const diffTouched = 'bg-transparent text-foreground border-primary/60 ring-2 ring-primary/60';
export const diffEmpty = 'text-muted-foreground/40';

// Tints for wrapping non-text content (audio, pictures), where line-through means nothing.
export const diffRemovedSurface = 'bg-destructive/10';
export const diffAddedSurface = 'bg-emerald-500/15';
