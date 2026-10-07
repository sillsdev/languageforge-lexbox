#!/usr/bin/env bash
# Summarise a GitHub Actions run of sillsdev/languageforge-lexbox for an agent.
#
# Usage: .github/scripts/ci-logs.sh <run-id|run-or-job-url> [job-name-substring]
#                                   [--lines N] [--artifacts <dir>]
#
# Lists every job with its status. For each failed job: the failed step names, every
# "##[error]" line, and the N lines (default 30) leading up to the first error, from the
# raw job log with ANSI escapes and timestamps stripped. The full log is saved to a temp
# file whose path is printed. Works while the run is still in progress (finished jobs
# already have logs; `gh run view --log-failed` refuses until the whole run completes).
#
# --artifacts <dir> also downloads the run's *-k8s-logs artifacts (integration tests) into
# <dir> and counts pod-log lines that usually explain the failure.
set -euo pipefail

REPO="sillsdev/languageforge-lexbox"
LINES=30
ARTIFACT_DIR=""
FILTER=""
RUN=""

usage() { sed -n '4,5p' "$0" | sed 's/^# \{0,1\}//' >&2; exit 2; }

while [ $# -gt 0 ]; do
  case "$1" in
    --lines) LINES="${2:?}"; shift 2 ;;
    --artifacts) ARTIFACT_DIR="${2:?}"; shift 2 ;;
    -h|--help) usage ;;
    *)
      if [ -z "$RUN" ]; then RUN="$1"; elif [ -z "$FILTER" ]; then FILTER="$1"; else usage; fi
      shift ;;
  esac
done
[ -n "$RUN" ] || usage

JOB_ID=""
if [[ "$RUN" == *"/runs/"* ]]; then
  [[ "$RUN" =~ /job/([0-9]+) ]] && JOB_ID="${BASH_REMATCH[1]}"
  [[ "$RUN" =~ /runs/([0-9]+) ]] && RUN="${BASH_REMATCH[1]}"
fi
[[ "$RUN" =~ ^[0-9]+$ ]] || { echo "ci-logs: cannot read a run id from '$RUN'" >&2; exit 2; }

# $TMPDIR is unset or unusable in some Windows shells; fall back to $TEMP, then /tmp.
OUT_DIR=""
for base in "${TMPDIR:-}" "${TEMP:-}" /tmp; do
  [ -n "$base" ] && mkdir -p "$base/ci-logs/$RUN" 2>/dev/null && { OUT_DIR="$base/ci-logs/$RUN"; break; }
done
[ -n "$OUT_DIR" ] || { echo "ci-logs: no writable temp dir" >&2; exit 1; }

gh run view "$RUN" -R "$REPO" --json name,displayTitle,headBranch,event,status,conclusion,url \
  --jq '"\(.name) | \(.displayTitle) | \(.headBranch) (\(.event))\nstatus: \(.status) \(.conclusion)  \(.url)"'

conclusion=$(gh run view "$RUN" -R "$REPO" --json conclusion --jq .conclusion)
if [ "$conclusion" = "startup_failure" ]; then
  echo "startup_failure: the workflow YAML was rejected before any job ran (no logs, no annotations)."
  echo "Run actionlint on .github/workflows/; the exact message is only on the run page in the web UI."
  exit 1
fi

# id <TAB> status <TAB> conclusion <TAB> name
jobs=$(gh run view "$RUN" -R "$REPO" --json jobs \
  --jq '.jobs[] | [.databaseId, .status, (if .conclusion == "" then "-" else .conclusion end), .name] | @tsv')
echo
echo "jobs:"
if [ -z "$jobs" ]; then echo "  (none scheduled yet)"; fi
printf '%s\n' "$jobs" | awk -F'\t' 'NF >= 4 { printf "  %-11s %-10s %s  [%s]\n", $2, $3, $4, $1 }'

failed=0
while IFS=$'\t' read -r id status concl name; do
  [ -n "$id" ] || continue
  case "$concl" in failure|timed_out|startup_failure) ;; *) continue ;; esac
  if [ -n "$JOB_ID" ] && [ "$id" != "$JOB_ID" ]; then continue; fi
  if [ -n "$FILTER" ] && [[ "${name,,}" != *"${FILTER,,}"* ]]; then continue; fi
  failed=$((failed + 1))
  log="$OUT_DIR/job-$id.log"
  echo
  echo "=== FAILED: $name ($concl)"
  gh run view "$RUN" -R "$REPO" --json jobs \
    --jq ".jobs[] | select(.databaseId == $id) | .steps[] | select(.conclusion == \"failure\") | \"  failed step: \" + .name"
  if ! gh api "repos/$REPO/actions/jobs/$id/logs" --allow-escape-sequences 2>/dev/null \
      | sed -E $'s/\x1b\\[[0-9;]*[A-Za-z]//g; s/^[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9:.]+Z //' >"$log" || [ ! -s "$log" ]; then
    echo "  (log not available yet)"
    continue
  fi
  echo "  full log: $log ($(wc -l <"$log") lines)"
  first=$(grep -n -m1 '##\[error\]' "$log" | cut -d: -f1 || true)
  if [ -z "$first" ]; then
    echo "  no ##[error] line; last $LINES lines:"
    tail -n "$LINES" "$log" | sed 's/^/  | /'
    continue
  fi
  echo "  errors:"
  grep '##\[error\]' "$log" | sort -u | head -n 10 | sed 's/^/  ! /'
  echo "  context before first error (line $first):"
  start=$((first > LINES ? first - LINES : 1))
  sed -n "${start},${first}p" "$log" | grep -v '^##\[endgroup\]' | sed 's/^##\[group\]/> /; s/^/  | /'
done <<<"$jobs"

if [ "$failed" -eq 0 ]; then
  echo
  echo "no failed jobs${FILTER:+ matching '$FILTER'}."
fi

if [ -n "$ARTIFACT_DIR" ]; then
  echo
  mkdir -p "$ARTIFACT_DIR"
  # -R matters: without it gh needs a git checkout and fails outside one ("not a git repository").
  if gh run download "$RUN" -R "$REPO" -p '*-k8s-logs' -D "$ARTIFACT_DIR" 2>/dev/null; then
    echo "k8s log artifacts in $ARTIFACT_DIR:"
    find "$ARTIFACT_DIR" -type f | sed "s|^$ARTIFACT_DIR/|  |" | head -n 40
    # ErrImagePull "not found" on a fork PR = images are only published for branches on origin.
    echo "pod-log hits (image pull, OOMKilled, CrashLoopBackOff, probe failures, exceptions):"
    grep -rE 'Failed to pull image|ErrImagePull|ImagePullBackOff|OOMKilled|CrashLoopBackOff|probe failed|Unhandled exception|Exception:' "$ARTIFACT_DIR" \
      | sed "s|^$ARTIFACT_DIR/||; s/  */ /g; s/^/  /" | cut -c1-240 | sort -u | head -n 15 || true
  else
    echo "no *-k8s-logs artifacts on run $RUN (they exist only on integration-test runs that reached log upload)."
  fi
fi
