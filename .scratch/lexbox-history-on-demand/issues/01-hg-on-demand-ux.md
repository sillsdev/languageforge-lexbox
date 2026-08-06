# Make Mercurial history load on demand

Type: prototype
Status: resolved

## Question

How should hg history stop loading on every project-page visit and instead load
only when the user asks for it? Decide the UX and the trigger.

Candidates:
- **Collapsed section + "Show history" button** on the same page — smallest change;
  gate the existing non-awaited `projectChangesets` query behind a click.
- **Separate history tab / sub-route** — history only fetches when navigated to.
- **Lazy-on-scroll** — inline, fetches when scrolled into view (still implicit).

Sub-questions to settle here:
- Does "on demand" also mean **bounding** the fetch (today it pulls the *entire*
  changelog — the real slowness)? Or same unbounded fetch, just deferred? If bounded,
  what's the initial count + how does "load more" work?
- Where does the trigger live relative to today's `HgLogView` + the loading spinner?

Context: `frontend/src/routes/(authenticated)/project/[project_code]/+page.ts`
(`projectChangesets` query, non-awaited), `HgLogView.svelte`, `+page.svelte:620-635`.
Backend `HgService.GetChangesets` sends `log?style=json-lex` with no rev/revcount.

Output: a decided UX (with a rough `/prototype` if it clarifies) and whether
pagination is in scope for the first cut.

## Answer

**Trigger: collapsed section + "Show history" button.** History stays on the project
page (`+page.svelte`, the `project_page.history` block ~lines 618-635) but renders
collapsed by default. A button reveals the section *and* fires the fetch. No separate
tab, no scroll trigger.

**Fetch is gated on the click** — the existing non-awaited `projectChangesets`
`queryStore` must NOT fire on page load anymore; it fires only when the button is
clicked. (Today it's already client-side lazy but auto-fires; the change is to defer
that firing to the trigger.) Reuse `HgLogView` as-is inside the revealed section.

**Bounding: out for v1 — just defer, keep the unbounded whole-changelog fetch.** On
click, fetch the full log exactly as today. This fixes the *page-load* slowness (the
stated problem) with minimal change. Large projects remain a big fetch once opened;
the existing "Open in hgweb" link is the escape hatch. Pagination/`revcount` stays in
the map's fog, not pulled into this ticket.

No prototype needed — decision made from concrete options grounded in the real page.

Implications for later tickets:
- Layout ticket must decide whether the FwLite section gets its own independent
  collapse/button or shares one trigger with hg (both default-collapsed).
- Spec: the button lives where the section title + "open in hgweb" link are now.
