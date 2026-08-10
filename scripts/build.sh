#!/usr/bin/env bash
# Build every project. Run this after ANY source edit -- the Godot runtime does not
# reliably rebuild on launch, so skipping this means running stale code.
#   scripts/build.sh [Debug|Release]
. "$(dirname "${BASH_SOURCE[0]}")/_env.sh"
CONFIG="${1:-Debug}"
cd "$REPO_ROOT"
exec dotnet build InfiniteExtraction.sln -c "$CONFIG" --nologo
