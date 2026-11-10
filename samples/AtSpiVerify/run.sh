#!/usr/bin/env bash
# Runs the built AtSpiVerify in its own session bus with the accessibility bus enabled; check.py writes its JSON to
# the output file, since the session bus logs to standard output.
# Usage: run.sh <directory holding the built app> <output file>   (needs DISPLAY)
set -euo pipefail
here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
app="$1/AtSpiVerify"
output="$2"

dbus-run-session -- bash -s "$app" "$here/check.py" "$output" <<'SESSION'
set -euo pipefail
app="$1" check="$2" output="$3"
for property in IsEnabled ScreenReaderEnabled; do
  gdbus call --session --dest org.a11y.Bus --object-path /org/a11y/bus \
    --method org.freedesktop.DBus.Properties.Set org.a11y.Status "$property" '<true>' > /dev/null
done
trigger="$(mktemp -u)"
"$app" "$trigger" > "$(mktemp)" 2>&1 &
pid=$!
status=0
python3 "$check" "$trigger" "$output" || status=$?
kill "$pid" 2> /dev/null || true
exit "$status"
SESSION
