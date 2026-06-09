# Apply rules

Shared rules for `/polish` and `/implement` when acting on agent
findings — what to apply directly, what to ask first, what's
off-limits, and how to re-validate after fixes.

## Apply directly (no asking)

Anything unambiguous and `AGENTS.md`-backed:

- Remove debug prints (tracing `Console.WriteLine` / `console.log`),
  unused imports, commented-out code blocks. NOT console output in CLI
  entry points / `Program.cs` / tooling — ask there.
- Remove a `try/catch` in `frontend/viewer/**` that just logs / swallows
  (see `viewer-watcher`, PR #2215).
- Remove a `DeletedAt is null` filter on a projected DbSet (PR #2286).
- Replace `.Result` / `.Wait()` / `.GetAwaiter().GetResult()` with
  `await` in async-reachable paths.
- Fix missed renames in generated TS / snapshot fixtures (prefer
  rebuild — `dotnet build backend/FwLite/FwLiteShared/FwLiteShared.csproj`
  — over hand-editing generated files when possible).
- Replace `BeSubsetOf` with `BeEquivalentTo` where set-equality is
  clearly intended (PR #2219).

## Format runs are strictly scoped to touched files

Never the whole solution / filter. Skip entirely if the diff touched
`.editorconfig` or any prettier config — config-change + format-run is
the unrelated-changes anti-pattern.

`.cs`:

```bash
dotnet format whitespace --verify-no-changes --include <touched .cs files>
# if non-zero:
dotnet format whitespace --include <touched .cs files>
```

Viewer / frontend:

```bash
cd frontend && pnpm --filter viewer exec prettier --check <touched files>
# if non-zero:
cd frontend && pnpm --filter viewer exec prettier --write <touched files>
```

## Ask before applying

Anything a reasonable reviewer might disagree about:

- Architecture moves (e.g. promoting a validation check into the
  Validation wrapper).
- New tests beyond a one-liner — show the proposed test, ask.
- Comment additions — show the proposed comment, ask.
- Public API renames.
- Edits to an already-open PR body.
- Anything touching the sync flow in
  `backend/FwLite/FwLiteProjectSync/CrdtFwdataProjectSyncService.cs` or
  `MiniLcm/SyncHelpers/`.

Use `AskUserQuestion` for binary / short-list decisions; plain text
otherwise. One decision at a time.

## Don't, without explicit user approval

- Commit, push, open a PR.
- Run integration tests (root `AGENTS.md`).
- Touch `backend/harmony` submodule contents.
- Modify `.git/`, `deployment/` PVCs, or any infrastructure state.

## Re-validate after fixes (loop, hard cap 2)

After applying fixes, dispatch a narrow re-validation:

- Only the agents whose domain you touched in the fixes.
- Only on the files you modified.
- Hard cap: 2 cycles. If cycle 2 still finds new findings, surface
  them but **don't auto-fix** — that's how feedback loops happen.

Common regressions to watch for:

- Removed `try/catch` → exposed a different error path. Re-run
  `viewer-watcher` on the touched file.
- Completed a rename → a previously-shadowed straggler now visible.
  Re-run `rename-detector` on the touched scope.
- Applied `dotnet format` → a comment moved off the line it referenced.
  Re-run `diff-hygiene` on the touched files.

Phase surfaces nothing → proceed to verdict.
