#!/bin/bash
# Smoke-test: does chorusmerge actually engage the FieldWorks merge handler, or silently
# fall back to keeping one whole file? See sillsdev/languageforge-lexbox#2508 / #2509.
#
# PartsOfSpeech.base.list is the FLExBridge split (nested) form of a possibility list — what
# hg actually versions and merges. Regenerate from a .fwdata with the FLExBridge split CLI.
#
# Plugins are discovered from AppContext.BaseDirectory (sillsdev/chorus#393), but hg still runs
# the merge tool with cwd = the repo (not /app). Run from a non-/app cwd so we keep exercising
# that path — ChorusRequirePlugins=1 (FwHeadless Dockerfile ENV) fails if none load.
set -euo pipefail

APP=${APP:-/app}
SMOKE="$(dirname "$(readlink -f "$0")")"
BASE="$SMOKE/PartsOfSpeech.base.list"
WORK="$(mktemp -d)"
trap 'rm -rf "$WORK"' EXIT

# Two edits to DIFFERENT possibility items (different guids) => they must auto-merge cleanly.
# A real 3-way merge keeps both; the whole-file "keep ours" fallback drops the theirs edit.
sed 's#>Adverb</AUni>#>Adverb_OURS</AUni>#' "$BASE" > "$WORK/ours.list"
sed 's#>Verb</AUni>#>Verb_THEIRS</AUni>#'   "$BASE" > "$WORK/theirs.list"
cp "$WORK/ours.list" "$WORK/merged.list"   # ChorusMerge writes the result back to arg 0

cd "$WORK"   # a non-/app cwd, exactly as hg runs the merge tool inside the repo
rc=0
ChorusPathToRepository="$WORK" "$APP/chorusmerge" "$WORK/merged.list" "$BASE" "$WORK/theirs.list" || rc=$?

if grep -q Adverb_OURS "$WORK/merged.list" && grep -q Verb_THEIRS "$WORK/merged.list"; then
  echo "chorusmerge smoke OK: FieldWorks 3-way merge engaged; both edits preserved"
else
  echo "chorusmerge smoke FAILED: merge dropped a side (FieldWorks handler not engaged; chorusmerge exit=$rc)"
  echo "  Adverb_OURS=$(grep -c Adverb_OURS "$WORK/merged.list" || true)  Verb_THEIRS=$(grep -c Verb_THEIRS "$WORK/merged.list" || true)"
  exit 1
fi
