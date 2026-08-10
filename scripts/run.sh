#!/usr/bin/env bash
# Launch the game. Builds first, then runs with output captured to logs/run.log.
#
#   scripts/run.sh                        # windowed, for a human to test
#   scripts/run.sh --editor               # open the Godot editor
#   IE_LOG=Controller:debug scripts/run.sh
#
# NOTE: a windowed launch is LONG-RUNNING. It exits when the user closes the window.
# The shell command returning, or a short log file, is not evidence that it crashed --
# output is block-buffered, so lines flush in bursts and lag the user's actions.
. "$(dirname "${BASH_SOURCE[0]}")/_env.sh"
"$REPO_ROOT/scripts/build.sh" >/dev/null

if [ "${1:-}" = "--editor" ]; then
  exec "$GODOT_BIN" --editor --path "$APP_DIR"
fi

LOG_FILE="$LOG_DIR/run.log"
: > "$LOG_FILE"
echo "logging to $LOG_FILE (IE_LOG=${IE_LOG:-<unset>})"

set +e
"$GODOT_BIN" --path "$APP_DIR" 2>&1 | tee "$LOG_FILE"
STATUS=${PIPESTATUS[0]}
set -e
exit "$STATUS"
