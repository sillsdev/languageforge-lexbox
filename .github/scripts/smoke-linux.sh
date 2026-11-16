#!/usr/bin/env bash
# Smoke test: launch a published linux-x64 FwLiteWeb, load it in headless Chrome and wait for the
# server to log "Viewer loaded", the line the MAUI smoke tests wait for. Leaves the server and
# Chrome logs in $SMOKE_OUT (default $RUNNER_TEMP/smoke).
# Usage: smoke-linux.sh <path to the FwLiteWeb binary>
# Used by fw-lite.yaml (publish-linux) and pr.yaml (tier web-linux).
set -euo pipefail
BIN="$(realpath "$1")"
TIMEOUT="${SMOKE_TIMEOUT:-120}"
OUT_DIR="${SMOKE_OUT:-$RUNNER_TEMP/smoke}"
URL=http://127.0.0.1:5137
SERVER_LOG="$OUT_DIR/fwliteweb.log"
CHROME_LOG="$OUT_DIR/chrome.log"
mkdir -p "$OUT_DIR"

chmod +x "$BIN"
cd "$(dirname "$BIN")"
# File logging off, or fw-lite-web.log lands in this folder and ships in the release artifact.
"$BIN" --urls "$URL" --FwLiteWeb:OpenBrowser=false --FwLiteWeb:EnableFileLogging=false --FwLite:UpdateCheckCondition=Never > "$SERVER_LOG" 2>&1 &
SERVER=$!
# Wait for the server to answer, giving up early if it died.
for _ in $(seq 60); do
  curl -fsS -o /dev/null "$URL/" && break
  kill -0 "$SERVER" 2>/dev/null || break
  sleep 1
done

google-chrome --headless=new --disable-gpu --no-sandbox --enable-logging=stderr --user-data-dir="$RUNNER_TEMP/chrome" "$URL/" > "$CHROME_LOG" 2>&1 &
CHROME=$!
# Wait for the page to finish loading in Chrome, again giving up early if the server died.
for _ in $(seq "$TIMEOUT"); do
  grep -q 'Viewer loaded' "$SERVER_LOG" && break
  kill -0 "$SERVER" 2>/dev/null || break
  sleep 1
done
if kill -0 "$CHROME" 2>/dev/null; then CHROME_STATE="still running"; else CHROME_STATE="already exited"; fi
kill "$CHROME" "$SERVER" 2>/dev/null || true

cat "$SERVER_LOG"
if ! grep -q 'Viewer loaded' "$SERVER_LOG"; then
  # Chrome is the half of this check the server log can't explain.
  echo "::group::Chrome output (Chrome had $CHROME_STATE)"
  cat "$CHROME_LOG"
  echo "::endgroup::"
  echo "::error::FwLiteWeb never logged 'Viewer loaded'"
  exit 1
fi
echo "FwLiteWeb logged 'Viewer loaded'"
