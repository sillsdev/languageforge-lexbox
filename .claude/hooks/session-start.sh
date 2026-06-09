#!/bin/bash
# SessionStart hook for claude/amazing-franklin-BXNI5 and beyond.
#
# Ensures `backend/harmony/` (the CRDT substrate submodule) is checked out so
# that subagents — particularly `harmony-sentinel` — can read its content.
# Without this, plain `git clone` leaves the submodule as a bare gitlink
# pointer and any agent that tries to read inside it gets an empty directory.
#
# Idempotent: `git submodule update --init --recursive` is a no-op when the
# submodule is already at the pinned SHA.
#
# Non-fatal on network failure: a transient github.com outage shouldn't block
# the whole session. We log and continue.

set -uo pipefail

# Only act in the remote (Claude Code on the web) environment. Local devs
# manage their own submodules.
if [ "${CLAUDE_CODE_REMOTE:-}" != "true" ]; then
  exit 0
fi

cd "${CLAUDE_PROJECT_DIR:-$(pwd)}"

echo "[session-start] initializing backend/harmony submodule…"
if git submodule update --init --recursive backend/harmony 2>&1; then
  echo "[session-start] harmony submodule ready at $(cd backend/harmony && git rev-parse --short HEAD 2>/dev/null || echo 'unknown')"
else
  rc=$?
  echo "[session-start] WARN: submodule init failed (exit $rc); harmony-sentinel reviews will report 'submodule not fetched' until resolved." >&2
fi

# Always exit 0 so a failed submodule init doesn't block the session.
exit 0
