# Master-detail

FW Lite's core workspace: a **list pane** (left) beside a **detail pane** (right), with a resizable divider (`paneforge`). Selecting an item in the list opens it in the detail pane.

**List pane** (~300px, `@container/list`): a sticky filter/search bar (`border-b border-border`) over a scrolling list of entry rows. A row = `rounded-md p-3 hover:bg-accent` with `flex justify-between items-end` inside — headword (`font-semibold`), gloss (`text-sm text-muted-foreground`), and a part-of-speech `Badge`. The **selected** row uses `bg-accent text-accent-foreground`.

**Detail pane** (flexes to fill): entry header + actions, then the multi-writing-system field editor.

**Responsive**: side-by-side above `lg-view` (≥801px). Below it, only one pane shows at a time — the list, and tapping an entry pushes the detail over it (back-navigation returns to the list).
