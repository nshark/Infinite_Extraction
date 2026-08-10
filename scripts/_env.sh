# Sourced by the other scripts. Resolves the toolchain or explains how to get one.
set -euo pipefail
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
TOOLCHAIN_DIR="${IE_TOOLCHAIN_DIR:-$REPO_ROOT/.toolchain}"

if [ -f "$TOOLCHAIN_DIR/env.sh" ]; then
  # shellcheck disable=SC1091
  . "$TOOLCHAIN_DIR/env.sh"
else
  echo "No toolchain found. Run: bash scripts/setup-toolchain.sh" >&2
  exit 1
fi

command -v dotnet >/dev/null 2>&1 || { echo "dotnet not on PATH after sourcing env.sh" >&2; exit 1; }
[ -x "${GODOT_BIN:-}" ] || { echo "GODOT_BIN is not executable: ${GODOT_BIN:-unset}" >&2; exit 1; }

APP_DIR="$REPO_ROOT/app"
LOG_DIR="$REPO_ROOT/logs"
mkdir -p "$LOG_DIR"
