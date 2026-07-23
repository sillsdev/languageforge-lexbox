# Colors

Semantic, theme-aware color tokens (ShadCN convention, OKLCH). Every color is a CSS variable resolved through Tailwind utilities — **never hardcode hex**.

**Core pairs**: `background/foreground`, `card/card-foreground`, `popover/popover-foreground`, `primary/primary-foreground`, `secondary/secondary-foreground`, `muted/muted-foreground`, `accent/accent-foreground`, `destructive/destructive-foreground`, plus `border`, `input`, `ring`, and `sidebar*`.

**Usage**: `bg-primary text-primary-foreground`, `text-muted-foreground`, `border-border`, `bg-card`.

**Theming**: light is `:root`/`.light`, dark is `.dark`. Six accent themes are opt-in via `data-theme="green|rose|orange|violet|stone"` (default/blue = no attribute). All designs must work in both light and dark.
