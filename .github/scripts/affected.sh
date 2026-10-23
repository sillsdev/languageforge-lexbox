#!/usr/bin/env bash
# Prints ONE JSON object describing what the current branch affects, for the PR
# workflow's detect job and for `task ci:affected` (same answer in both places).
#
#   {
#     "projects":   [".NET project names affected, transitively, per nx's graph"],
#     "viewer":     true|false,   // nx project "viewer" (frontend/viewer) affected
#     "extension":  true|false,   // nx project "extension" (platform.bible-extension) affected
#     "everything": true|false,   // every real project is affected (a shared file changed):
#                                 // run the whole PR tier
#     "tiers":      ["slow-tests", ...],  // escalation tiers whose tier-<name> pseudo-project
#                                         // is affected (.github/ci-tiers/<name>/project.json)
#     "fallback":   null | "reason"  // set when detection failed; everything is then true
#   }
#
# Diff range: merge-base of ${AFFECTED_BASE:-origin/develop} and ${AFFECTED_HEAD:-HEAD},
# to that head. Only committed changes count, like GitHub's three-dot PR diff.
# Needs a full-depth checkout (fetch-depth: 0) and no token: everything is local git.
#
# Every rule lives in the nx graph (nx.json sharedGlobals, project.json files for
# the viewer, the extension and the tiers); this script only runs
# `nx show projects --affected` and shapes its answer. No globs here.
#
# Any failure still prints valid JSON with "everything": true and a "fallback"
# reason, and exits 0, so a broken detector can only over-run the PR tier, never
# skip it. Escalation tiers then come from labels only.

set -u

cd "$(git rev-parse --show-toplevel 2>/dev/null || pwd)" || exit 0

base_ref="${AFFECTED_BASE:-origin/develop}"
head_ref="${AFFECTED_HEAD:-HEAD}"

fallback() {
  # $1: reason (kept to printable ASCII, quotes stripped, so it is safe inside JSON)
  local reason
  reason=$(printf '%s' "$1" | tr -cd '[:print:]' | tr -d '"\\' | cut -c1-300)
  printf '{"projects":[],"viewer":true,"extension":true,"everything":true,"tiers":[],"fallback":"%s"}\n' "$reason"
  exit 0
}

# nx prints its JSON array as the last stdout line; banners and errors go to stderr,
# which is left visible so a fallback can be diagnosed from the log.
nx_json() {
  local out
  out=$("$nx" "$@" --json) || return 1
  out=$(printf '%s\n' "$out" | grep '^\[' | tail -n 1)
  [ -n "$out" ] || return 1
  printf '%s' "$out"
}

merge_base=$(git merge-base "$base_ref" "$head_ref" 2>&1) \
  || fallback "git merge-base $base_ref $head_ref failed: $merge_base"

nx=./node_modules/.bin/nx
[ -x "$nx" ] || fallback "nx is not installed: run 'pnpm install' at the repo root"

all=$(nx_json show projects) \
  || fallback "nx show projects failed"
affected=$(nx_json show projects --affected --base="$merge_base" --head="$head_ref") \
  || fallback "nx show projects --affected failed"

# nx marks every project, tiers included, when nx.json, the root package.json or the root
# lockfile changes, so tiers are decided on the diff without those files: each tier fires by
# its own rule only. sharedGlobals never reach a tier (no `default` input), so they don't escalate.
tier_files=$(git diff --name-only --no-renames "$merge_base" "$head_ref" -- \
  ':!nx.json' ':!package.json' ':!pnpm-lock.yaml' ':!pnpm-workspace.yaml') \
  || fallback "git diff $merge_base $head_ref failed"
tier_affected=$(nx_json show projects --affected --files="$(printf '%s' "$tier_files" | paste -sd, -)") \
  || fallback "nx show projects --affected --files failed"

# Assemble the result with node (nx already requires it), so names are escaped properly.
ALL="$all" AFFECTED="$affected" TIER_AFFECTED="$tier_affected" node -e '
  const isTier = (n) => n.startsWith("tier-");
  const isSpecial = (n) => n === "viewer" || n === "extension";
  const all = JSON.parse(process.env.ALL);
  const affected = JSON.parse(process.env.AFFECTED);
  const real = (names) => names.filter((n) => !isTier(n));
  const projects = affected.filter((n) => !isTier(n) && !isSpecial(n)).sort();
  const everything = real(all).every((n) => affected.includes(n));
  const tiers = JSON.parse(process.env.TIER_AFFECTED).filter(isTier)
    .map((n) => n.slice("tier-".length)).sort();
  process.stdout.write(JSON.stringify({
    projects,
    viewer: affected.includes("viewer"),
    extension: affected.includes("extension"),
    everything,
    tiers,
    fallback: null,
  }) + "\n");
' || fallback "could not assemble JSON from nx output"
