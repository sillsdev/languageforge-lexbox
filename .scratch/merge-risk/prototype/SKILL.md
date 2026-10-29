# merge-risk (prototype, run by hand)

Decide whether a PR can merge without a line-by-line read. Only care about changes whose worst plausible outcome is painful to repair:
- data corruption or loss (sync, FwData writes, CRDT history, hg repos, DB)
- breaking app updates (old clients, serialized changes, sync/API contracts, migrations)
- long-term pain a new release can't undo

Everything else is fixable by a follow-up release: mention it only if it truly helps.

## Steps
1. Ground: diff vs base, commits, PR body, CI on the current head, open bot findings (Devin, CodeRabbit, Claude) on the current head.
2. Read the diff yourself and list candidate scary changes (what could go wrong, how likely, how hard to repair). Paths are a hint, not a rule.
3. Dispatch 2-4 agents chosen by what the change does: test-auditor and ripple-detector by default; fwlite-sentinel / fwheadless-sentinel / harmony-sentinel / migration-detective only if a candidate is in their area. Brief them with the candidates and the "painful to repair" bar.
4. For each scary change: would a test fail if it went wrong? Run only tests the PR adds or changes.
5. Verdict, one of:
   - **Merge**
   - **Do X, then merge** (X is small: a fast test, a guard, a one-line fix; may do it per `_shared/apply-rules.md`)
   - **Spot-check these hunks, then merge** (name the hunks and what to look for)
   - **Needs a real review** (say why)

## Output
1. **Verdict** and X (if any) in one or two lines.
2. **Worst things that could happen**: painful outcomes only, worst first. Each: outcome · the change that could cause it · likelihood · repair cost · what would catch it (test name, CI, or "nothing"). Say "nothing painful" if empty.
3. **Also noted**: one line per non-scary item that truly helps. Often empty.
