#!/usr/bin/env bash
# Smoke test: install a FW Lite APK on the running emulator, launch it and wait for it to log
# "Viewer loaded" (console logging lands in logcat under the DOTNET tag). Leaves logcat and a
# screenshot in $SMOKE_OUT (default $RUNNER_TEMP/smoke).
# Usage: smoke-android.sh <path to .apk>   (run inside reactivecircus/android-emulator-runner)
# Used by fw-lite.yaml (launch-android) and pr.yaml (tier android).
set -euo pipefail
APK="$1"
TIMEOUT="${SMOKE_TIMEOUT:-240}"
OUT_DIR="${SMOKE_OUT:-$RUNNER_TEMP/smoke}"
mkdir -p "$OUT_DIR"
AAPT2="$(ls -d "$ANDROID_HOME"/build-tools/* | sort -V | tail -1)/aapt2"
PKG="$("$AAPT2" dump packagename "$APK")"
LOGCAT="$OUT_DIR/logcat.txt"

adb install -r "$APK"
adb logcat -c
adb shell monkey -p "$PKG" -c android.intent.category.LAUNCHER 1 > /dev/null
found=false
for i in $(seq "$TIMEOUT"); do
  # To a file, not a pipe: grep -q closing the pipe early would fail adb under pipefail.
  adb logcat -d > "$LOGCAT"
  grep -q 'Viewer loaded' "$LOGCAT" && { found=true; break; }
  # Give the process a moment to start before treating its absence as a crash.
  if [ "$i" -gt 15 ] && ! adb shell pidof "$PKG" > /dev/null; then echo "The app exited early"; break; fi
  sleep 1
done
adb exec-out screencap -p > "$OUT_DIR/android.png" || true
adb logcat -d > "$LOGCAT"
grep -E 'DOTNET|AndroidRuntime|FATAL' "$LOGCAT" | tail -300 || true
adb shell am force-stop "$PKG" || true
[ "$found" = true ] || { echo "::error::The Android app never logged 'Viewer loaded'"; exit 1; }
echo "The Android app logged 'Viewer loaded'"
