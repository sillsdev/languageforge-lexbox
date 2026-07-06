import type {IconClass} from '$lib/icon-class';
import type {FactCategory} from './change-summary';

// Change-kind glyph vocabulary shared by the activity list and the preview pane: the icon SHAPE
// classifies the change kind (added / removed / changed / reordered); adds and removes carry colour
// so they pop while routine edits recede.
export const FACT_GLYPH: Record<FactCategory, {icon: IconClass; class: string} | undefined> = {
  added: {icon: 'i-mdi-plus', class: 'text-emerald-600 dark:text-emerald-400'},
  removed: {icon: 'i-mdi-minus', class: 'text-destructive'},
  changed: {icon: 'i-mdi-pencil-outline', class: 'text-muted-foreground'},
  reordered: {icon: 'i-mdi-swap-vertical', class: 'text-muted-foreground'},
  other: undefined,
};
