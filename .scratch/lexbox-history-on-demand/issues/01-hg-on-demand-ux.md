# Make Mercurial history load on demand

Type: prototype
Status: open

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
