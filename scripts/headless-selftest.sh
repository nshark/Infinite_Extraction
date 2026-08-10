#!/usr/bin/env bash
# Headless boot check: starts the real app with IE_HEADLESS_SELFTEST=1, which wires
# the controller to the real views, runs one scripted interaction, and quits on its
# own. Self-exiting on purpose -- 'sleep && kill' produces teardown noise that looks
# like a crash but isn't.
#
#   scripts/headless-selftest.sh
#   IE_LOG=Boot:debug,Controller:debug scripts/headless-selftest.sh
. "$(dirname "${BASH_SOURCE[0]}")/_env.sh"
"$REPO_ROOT/scripts/build.sh" >/dev/null

LOG_FILE="$LOG_DIR/selftest.log"
: > "$LOG_FILE"

set +e
IE_HEADLESS_SELFTEST=1 IE_LOG="${IE_LOG:-Boot:info,Controller:info}" \
  "$GODOT_BIN" --headless --path "$APP_DIR" 2>&1 | tee "$LOG_FILE"
STATUS=${PIPESTATUS[0]}
set -e

echo "---- instrumentation captured in $LOG_FILE ----"
grep -E '^\[(BOOT|CONTROLLER|MODEL|VIEW|INPUT|UNDO)\]' "$LOG_FILE" || \
  echo "(none -- the code path did not run, or IE_LOG left those categories at warn)"

exit "$STATUS"
