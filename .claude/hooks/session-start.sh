#!/bin/bash
# SessionStart hook: make a fresh worktree's frontend runnable (svelte-check, eslint, vitest,
# the pre-commit prettier hook). Prints one line; fast no-op when deps are current.
# Harmony is consumed via NuGet; nothing to initialise for it.

cd "${CLAUDE_PROJECT_DIR:-$(git rev-parse --show-toplevel 2>/dev/null)}" 2>/dev/null || exit 0
[ -f frontend/pnpm-lock.yaml ] || exit 0

if [ -d frontend/viewer/node_modules/.bin ] && [ ! frontend/pnpm-lock.yaml -nt frontend/node_modules/.modules.yaml ]; then
  echo "session-start: frontend deps up to date"
  exit 0
fi

if ! command -v pnpm >/dev/null 2>&1; then
  echo "session-start: frontend deps missing or stale and pnpm is not on PATH; run: cd frontend && pnpm install --frozen-lockfile"
  exit 0
fi

log="${TMPDIR:-${TEMP:-/tmp}}/lexbox-session-start-install.log"
# cd, not pnpm -C: only cwd picks up the packageManager pin in frontend/package.json (same as .husky/pre-commit).
if (cd frontend && pnpm install --frozen-lockfile --prefer-offline) >"$log" 2>&1; then
  echo "session-start: installed frontend deps (log: $log)"
else
  echo "session-start: frontend install FAILED, see $log; retry: cd frontend && pnpm install --frozen-lockfile"
fi
exit 0
