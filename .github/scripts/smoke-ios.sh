#!/usr/bin/env bash
# Smoke test: boot an iOS simulator, install a FW Lite simulator .app, launch it and wait for it
# to log "Viewer loaded". Leaves a screenshot in $SMOKE_OUT (default $RUNNER_TEMP/smoke).
# Usage: smoke-ios.sh <path to iossimulator .app>
# Used by fw-lite.yaml (launch-ios) and pr.yaml (tier ios).
set -euo pipefail
APP="$1"
TIMEOUT="${SMOKE_TIMEOUT:-180}"
OUT_DIR="${SMOKE_OUT:-$RUNNER_TEMP/smoke}"
mkdir -p "$OUT_DIR"
BUNDLE_ID="$(/usr/libexec/PlistBuddy -c 'Print :CFBundleIdentifier' "$APP/Info.plist")"

# The newest iOS runtime's first available iPhone; the runner image decides which models exist.
UDID="$(xcrun simctl list devices available -j | python3 -c '
import json, re, sys
best = None
for runtime, devices in json.load(sys.stdin)["devices"].items():
    m = re.search(r"iOS-(\d+)-(\d+)", runtime)
    if not m:
        continue
    version = (int(m[1]), int(m[2]))
    for d in devices:
        if d.get("isAvailable") and d["name"].startswith("iPhone") and (best is None or version > best[0]):
            best = (version, d["udid"], d["name"])
if best is None:
    sys.exit("No available iPhone simulator")
print(f"Simulator: {best[2]} on iOS {best[0][0]}.{best[0][1]}", file=sys.stderr)
print(best[1])
')"

xcrun simctl boot "$UDID"
xcrun simctl bootstatus "$UDID" -b
xcrun simctl install "$UDID" "$APP"
STDOUT="$OUT_DIR/ios-stdout.log"
STDERR="$OUT_DIR/ios-stderr.log"
# Prints "<bundle id>: <pid>"; simulator apps are ordinary host processes, so kill -0 works on the pid.
PID="$(xcrun simctl launch --stdout="$STDOUT" --stderr="$STDERR" "$UDID" "$BUNDLE_ID" | awk '{print $NF}')"
DATA="$(xcrun simctl get_app_container "$UDID" "$BUNDLE_ID" data)"

for _ in $(seq "$TIMEOUT"); do
  grep -rqs 'Viewer loaded' "$STDOUT" "$STDERR" "$DATA" && break
  kill -0 "$PID" 2>/dev/null || { echo "The app exited early"; break; }
  sleep 1
done
xcrun simctl io "$UDID" screenshot "$OUT_DIR/ios.png" || true
found=false
grep -rqs 'Viewer loaded' "$STDOUT" "$STDERR" "$DATA" && found=true
xcrun simctl terminate "$UDID" "$BUNDLE_ID" 2>/dev/null || true
cat "$STDOUT" "$STDERR" 2>/dev/null || true
find "$DATA" -name 'app*.log' -print -exec cat {} \; 2>/dev/null || true
xcrun simctl shutdown "$UDID" || true
[ "$found" = true ] || { echo "::error::The iOS app never logged 'Viewer loaded'"; exit 1; }
echo "The iOS app logged 'Viewer loaded'"
