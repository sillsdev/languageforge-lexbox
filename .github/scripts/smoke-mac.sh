#!/usr/bin/env bash
# Smoke test: launch a FW Lite Mac Catalyst .app and wait for it to log "Viewer loaded".
# Usage: smoke-mac.sh <path to .app>
# Used by fw-lite.yaml (launch-mac, the notarized DMG) and pr.yaml (tier mac, an unsigned build).
set -euo pipefail
APP="$1"
TIMEOUT="${SMOKE_TIMEOUT:-120}"
echo "Runner CPU: $(uname -m)"
BUNDLE_ID="$(/usr/libexec/PlistBuddy -c 'Print :CFBundleIdentifier' "$APP/Contents/Info.plist")"
EXE="$APP/Contents/MacOS/$(/usr/libexec/PlistBuddy -c 'Print :CFBundleExecutable' "$APP/Contents/Info.plist")"
OUT="$RUNNER_TEMP/mac-stdout.log"
# app.log lands in the sandbox container when the app is signed with its entitlements, else in Application Support.
LOG_DIRS=("$HOME/Library/Containers/$BUNDLE_ID" "$HOME/Library/Application Support/$BUNDLE_ID")

# -D skip: CoreCLR's debugger transport leaves FIFOs (clr-debug-pipe-*) in the app's tmp dir, which is inside the
# folders searched here; reading one blocks forever.
viewer_loaded() { grep -rqs -D skip 'Viewer loaded' "$@"; }
"$EXE" > "$OUT" 2>&1 &
PID=$!
for _ in $(seq "$TIMEOUT"); do
  viewer_loaded "$OUT" "${LOG_DIRS[@]}" && break
  kill -0 "$PID" 2>/dev/null || break
  sleep 1
done
if ! viewer_loaded "$OUT" "${LOG_DIRS[@]}" && kill -0 "$PID" 2>/dev/null; then
  echo "::group::The app is still running without 'Viewer loaded': native stacks"
  sample "$PID" 3 2>&1 || true
  echo "::endgroup::"
fi
# .NET treats SIGTERM as a graceful shutdown, which never finishes if the app is hung, so fall back to SIGKILL.
kill "$PID" 2>/dev/null || true
for _ in $(seq 10); do kill -0 "$PID" 2>/dev/null || break; sleep 1; done
kill -9 "$PID" 2>/dev/null || true
wait "$PID" 2>/dev/null || true
cat "$OUT"
find "${LOG_DIRS[@]}" -name 'app*.log' -print -exec cat {} \; 2>/dev/null || true
viewer_loaded "$OUT" "${LOG_DIRS[@]}" || { echo "::error::The Mac app never logged 'Viewer loaded'"; exit 1; }
echo "The Mac app logged 'Viewer loaded'"
