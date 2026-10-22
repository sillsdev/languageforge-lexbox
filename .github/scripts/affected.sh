#!/usr/bin/env bash
# Prints ONE JSON object describing what the current branch affects, for the PR
# workflow's detect job and for `task ci:affected` (same answer in both places).
#
#   {
#     "projects":   [".NET project names affected, transitively, per nx's graph"],
#     "viewer":     true|false,   // frontend/viewer affected (nx project "viewer")
#     "extension":  true|false,   // platform.bible-extension/** changed
#     "everything": true|false,   // a shared file changed: run the whole PR tier
#     "tiers":      [],           // escalation tiers from path rules (not yet)
#     "fallback":   null | "reason"  // set when detection failed; everything is then true
#   }
#
# Diff range: merge-base of ${AFFECTED_BASE:-origin/develop} and ${AFFECTED_HEAD:-HEAD},
# to that head. Only committed changes count, like GitHub's three-dot PR diff.
# Needs a full-depth checkout (fetch-depth: 0) and no token: everything is local git.
#
# .NET names and the viewer boolean come from nx (`nx show projects --affected`,
# @nx/dotnet plugin, graph-only). This script keeps only the glob rules nx does
# not express: the shared files that mark everything affected, and the extension.
#
# Any failure still prints valid JSON with "everything": true and a "fallback"
# reason, and exits 0, so a broken detector can only over-run CI, never skip it.

set -u

cd "$(git rev-parse --show-toplevel 2>/dev/null || pwd)" || exit 0

base_ref="${AFFECTED_BASE:-origin/develop}"
head_ref="${AFFECTED_HEAD:-HEAD}"

# Shared files: a change to any of these marks everything affected (map decision).
# Matched as bash globs against repo-relative paths.
everything_globs=(
  'global.json'
  'LexBox.slnx'
  '*.slnf'
  'backend/Directory.Build.props'
  'backend/Directory.Build.targets'
  'backend/Directory.Packages.props'
  'backend/Harmony*.props'
  'frontend/pnpm-lock.yaml'
  'frontend/pnpm-workspace.yaml'
  # The detector's own inputs: nx config and this script.
  'nx.json'
  'package.json'
  'pnpm-lock.yaml'
  'pnpm-workspace.yaml'
  '.github/scripts/affected.sh'
)
extension_globs=('platform.bible-extension/*')

fallback() {
  # $1: reason (kept to printable ASCII, quotes stripped, so it is safe inside JSON)
  local reason
  reason=$(printf '%s' "$1" | tr -cd '[:print:]' | tr -d '"\\' | cut -c1-300)
  printf '{"projects":[],"viewer":true,"extension":true,"everything":true,"tiers":[],"fallback":"%s"}\n' "$reason"
  exit 0
}

matches_any() {
  # $1: path, rest: globs. `*` in a glob also crosses `/` here (pattern matching
  # in [[ ]] does not treat `/` specially), so 'dir/*' means 'dir/**'.
  local path=$1; shift
  local g
  for g in "$@"; do
    # shellcheck disable=SC2053
    [[ $path == $g ]] && return 0
  done
  return 1
}

merge_base=$(git merge-base "$base_ref" "$head_ref" 2>&1) \
  || fallback "git merge-base $base_ref $head_ref failed: $merge_base"

changed=$(git diff --name-only "$merge_base" "$head_ref" 2>&1) \
  || fallback "git diff $merge_base..$head_ref failed: $changed"

everything=false
extension=false
while IFS= read -r f; do
  [ -z "$f" ] && continue
  matches_any "$f" "${everything_globs[@]}" && everything=true
  matches_any "$f" "${extension_globs[@]}" && extension=true
done <<<"$changed"

nx=./node_modules/.bin/nx
[ -x "$nx" ] || fallback "nx is not installed: run 'pnpm install' at the repo root"

# nx prints the JSON array as the last stdout line; banners go to stderr.
nx_out=$("$nx" show projects --affected --base="$merge_base" --head="$head_ref" --json 2>/dev/null) \
  || fallback "nx show projects --affected failed (exit $?)"
nx_json=$(printf '%s\n' "$nx_out" | grep '^\[' | tail -n 1)
[ -n "$nx_json" ] || fallback "nx printed no JSON array"

# Assemble the result with node (nx already requires it), so names are escaped properly.
NX_JSON="$nx_json" EVERYTHING="$everything" EXTENSION="$extension" node -e '
  const names = JSON.parse(process.env.NX_JSON);
  const viewer = names.includes("viewer");
  const projects = names.filter((n) => n !== "viewer").sort();
  process.stdout.write(JSON.stringify({
    projects,
    viewer,
    extension: process.env.EXTENSION === "true",
    everything: process.env.EVERYTHING === "true",
    tiers: [],
    fallback: null,
  }) + "\n");
' || fallback "could not assemble JSON from nx output"
