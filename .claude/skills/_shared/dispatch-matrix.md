# Dispatch matrix

Both `/polish` and `/implement` use this matrix to choose which
subagents to spawn against a diff (or a plan, for implement's plan-
validation phase).

Dispatch via `Agent` with `subagent_type=<name>` (the value in the
"Agent" column, without the `.md` extension).

## Always on

| Agent | Purpose |
|---|---|
| `diff-hygiene` | Debug prints, scratch files, secrets, lonely TODOs. |
| `rename-detector` | Stragglers after symbol renames; blast-radius count. |
| `ripple-detector` | Signature changes, behavior shifts with stable signatures, payload contract drift, deleted symbols with surviving references. |
| `test-auditor` | Assertion quality, regression coverage, BeSubsetOf vs BeEquivalentTo. |
| `pr-narrative` | Title/body audit; drafts when no PR exists. |
| `intent-check` | Linked issue vs work — addresses the claim? scope drift? |

## Conditional on touched paths

| Touched | Agent |
|---|---|
| `backend/harmony/**` (submodule pointer bump or in-place edits) | `harmony-sentinel` |
| `backend/FwLite/**` | `fwlite-sentinel` |
| `backend/FwHeadless/**` | `fwheadless-sentinel` |
| `backend/LexBoxApi/GraphQL/**` or any `.cs` with `[GraphQLType]` / `[QueryType]` / `[MutationType]` / `[ObjectType]` | `graphql` (additive — `dotnet-stylist` still runs on these too) |
| `frontend/src/**` (main LexBox SvelteKit app, not `frontend/viewer/`) | `lexbox-frontend` |
| `**/*.cs` (general .NET) | `dotnet-stylist` |
| `frontend/viewer/**` | `viewer-watcher` |
| `frontend/viewer/**` AND new `$t` / `msg` strings | `i18n-completeness` |
| `**/Migrations/**` | `migration-detective` |
| `**/*.sh`, `**/Dockerfile*` | `bash-discipline` |
| `.github/workflows/**`, `.github/actions/**` | `ci-workflow` |
| `deployment/**` | `deployment-infra` |

## Invocation contract

Each agent receives via your invocation prompt:

1. The diff scope — relevant paths, or the full diff if small, or (for
   implement's plan-validation phase) the proposed plan text.
2. The mode signal — *"reviewing a diff"* (default) or *"validating a
   plan, not yet executed; report missing steps and sequencing risks"*.
3. Severity ladder reminder: 🚫 blocking · ⚠️ important · 💭 nit · ✨ praise.
4. Finding format: `<emoji> <severity> · path:line\n  why-it-matters with citation`.

In **plan-validation** mode (item 2 above) an agent walks its
standards/checklist against the plan text and skips diff-only steps
(grep-the-diff, fanout-table greps). Dispatch only agents whose
checklist is meaningful against a plan; the mechanical / narrative
always-on agents (`diff-hygiene`, `rename-detector`, `pr-narrative`,
`intent-check`) are diff-only — don't dispatch them in plan mode.

Agents are self-contained — they don't see your conversation. Keep
invocation prompts under ~150 words.

Spawn all relevant agents **in one message** (parallel) to keep
wallclock low.
