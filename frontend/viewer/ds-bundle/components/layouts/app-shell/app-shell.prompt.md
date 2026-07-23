# App shell

The outer chrome every FW Lite screen lives in: a **sidebar** (project switcher, a primary `New Word` action, the Dictionary nav group with an active/badge state, and footer utilities like Synchronize/Settings) beside the **primary view**.

**Structure**: `bg-sidebar text-sidebar-foreground` sidebar with `border-e border-sidebar-border`, ~190px wide; nav items = `flex w-full items-center gap-2 rounded-md p-2 text-sm`, hover `hover:bg-sidebar-accent hover:text-sidebar-accent-foreground`, **active** `bg-sidebar-accent text-sidebar-accent-foreground font-medium`. Main region = `bg-background flex flex-col`, fills remaining width. Built on the ShadCN **Sidebar** component (`$lib/components/ui/sidebar`).

**Responsive**: on narrow screens (`sm-view`, ≤800px) the sidebar collapses to an off-canvas drawer toggled by a hamburger; the primary view takes the full width.
