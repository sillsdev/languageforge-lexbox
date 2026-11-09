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

"$EXE" > "$OUT" 2>&1 &
PID=$!
for _ in $(seq "$TIMEOUT"); do
  grep -rqs 'Viewer loaded' "$OUT" "${LOG_DIRS[@]}" && break
  kill -0 "$PID" 2>/dev/null || break
  sleep 1
done
kill "$PID" 2>/dev/null || true
wait "$PID" 2>/dev/null || true
cat "$OUT"
find "${LOG_DIRS[@]}" -name 'app*.log' -print -exec cat {} \; 2>/dev/null || true
grep -rqs 'Viewer loaded' "$OUT" "${LOG_DIRS[@]}" || { echo "::error::The Mac app never logged 'Viewer loaded'"; exit 1; }
echo "The Mac app logged 'Viewer loaded'"
