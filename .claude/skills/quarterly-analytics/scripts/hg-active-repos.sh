#!/bin/sh
# Runs inside the hg pod (piped in by prod-collect.ps1). Usage: sh -s -- START LAST_DAY MTIME_CUTOFF
# Prints the code of every repo with at least one commit dated START..LAST_DAY (whole days), one
# per line, then a final "# repos=.. checked=.. active=.. failed=.." line. Progress goes to stderr.
#
# Load matters more than speed here: a parallel scan has taken a node down before. So this runs
# one hg process at a time at the lowest CPU priority, refuses to start while another scan is still
# running (e.g. one orphaned by a dropped kubectl exec), and skips repos whose changelog hasn't been
# written since MTIME_CUTOFF (a month before START): those can't hold commits from the period.
# If find can't do -newermt, every repo is checked, which is slower but no heavier per repo.

main() {
  START=$1
  LAST_DAY=$2
  MTIME_CUTOFF=$3
  for d in "$START" "$LAST_DAY" "$MTIME_CUTOFF"; do
    case "$d" in
      [0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9]) ;;
      *) echo "bad date argument: '$d'" >&2; return 2 ;;
    esac
  done
  command -v hg >/dev/null 2>&1 || { echo "hg not found on PATH" >&2; return 2; }

  lock=/tmp/hg-active-repos.lock
  if ! mkdir "$lock" 2>/dev/null; then
    other=$(cat "$lock/pid" 2>/dev/null)
    case "$other" in ''|*[!0-9]*) other='' ;; esac
    if [ -n "$other" ] && kill -0 "$other" 2>/dev/null; then
      echo "another scan (pid $other) is still running; wait for it to finish" >&2
      return 3
    fi
    echo "taking over a stale lock (pid ${other:-unknown} is gone)" >&2
  fi
  echo $$ > "$lock/pid"
  trap 'rm -rf "$lock"' EXIT
  trap 'rm -rf "$lock"; exit 129' HUP INT TERM PIPE

  nice_cmd=""
  command -v nice >/dev/null 2>&1 && nice_cmd="nice -n 19"

  fast=1
  find / -maxdepth 0 -newermt "$MTIME_CUTOFF" >/dev/null 2>&1 || fast=0
  echo "range $START..$LAST_DAY, changelog mtime prefilter: $fast" >&2

  total=0
  checked=0
  active=0
  failed=0
  for group in "${HG_REPOS_ROOT:-/var/hg/repos}"/*; do
    [ -d "$group" ] || continue
    for repo in "$group"/*; do
      [ -d "$repo/.hg" ] || continue
      total=$((total + 1))
      if [ "$fast" = 1 ] && [ -z "$(find "$repo/.hg/store" -maxdepth 1 -name '00changelog.*' -newermt "$MTIME_CUTOFF" 2>/dev/null)" ]; then
        continue
      fi
      checked=$((checked + 1))
      # HGPLAIN ignores aliases and [defaults] from the repo's own hgrc.
      if ! out=$(HGPLAIN=1 $nice_cmd hg -R "$repo" log -d "$START to $LAST_DAY" -l 1 --template '{rev}\n' 2>/dev/null); then
        failed=$((failed + 1))
        echo "hg log failed: $(basename "$repo")" >&2
      fi
      if [ -n "$out" ]; then
        active=$((active + 1))
        basename "$repo"
      fi
    done
    echo "$group done: repos=$total checked=$checked active=$active failed=$failed" >&2
  done
  echo "# repos=$total checked=$checked active=$active failed=$failed"
  [ "$total" -gt 0 ] || { echo "no repos found" >&2; return 4; }
}

# Exit here so stray bytes a shell pipe appends after the script (e.g. a CRLF) never run.
main "$@"
exit $?
