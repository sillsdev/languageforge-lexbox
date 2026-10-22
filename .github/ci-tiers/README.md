# CI escalation tiers

Each folder here is an nx pseudo-project named `tier-<name>`. It has no code and
runs nothing: it exists so that `nx show projects --affected` (via
`.github/scripts/affected.sh`) lists `tier-<name>` when a pull request should
pull that tier's full-tier jobs into its PR run. The `ci:<name>` label does the
same thing by hand, and `ci:full` sets every tier.

A tier's rule is in its `project.json`, in one of two forms:

- **Path rule (the default):** a `ci-affected` target whose `inputs` are
  `{workspaceRoot}/...` globs. The tier fires when a file matching a glob
  changes, and only then (an edit upstream of those paths does not fire it).
  The target is never run; only its `inputs` are read.
- **Project rule:** `implicitDependencies` on project names. The tier fires when
  any listed project is affected, **transitively**: nx walks the reverse graph,
  so a change upstream of a listed project (e.g. `MiniLcm`, `LexCore`, the
  analyzers) fires the tier too. Use it when that is what you want.

Label-only tiers (`apple`, `android`, `webkit`) have an empty rule and fire only
by label.

Two rules the script adds on top of the graph:

- When a shared file marks everything affected (`nx.json`'s `sharedGlobals`),
  the script reports no tiers: the whole PR tier runs, still Linux-only. Only the
  PR workflow file itself escalates, to `full`.
- Editing a tier's `project.json` fires that tier (its own folder changed), so a
  rule change proves itself on the PR that makes it.

A missed escalation during the shadow phase becomes a one-line change to the
tier's `project.json`, reviewed like any other PR.
