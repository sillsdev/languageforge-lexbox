# Alert

Inline callout for status / errors. Svelte: `import * as Alert from '$lib/components/ui/alert';`

**Props**: `variant` = `default | destructive`.

**Class recipe**: `relative grid grid-cols-[0_1fr] items-start rounded-lg border px-4 py-3 text-sm`; default = `bg-card text-card-foreground`, destructive = `bg-destructive text-destructive-foreground`. An optional leading `i-mdi-*` icon shifts the grid to `[calc(var(--spacing)*4)_1fr]`.
