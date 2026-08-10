#!/usr/bin/env bash
# Run the full xUnit suite. No engine on the graph -- this runs anywhere dotnet does.
#   scripts/test.sh                                  # everything
#   scripts/test.sh 'FullyQualifiedName~NoFloating'  # single test / class filter
. "$(dirname "${BASH_SOURCE[0]}")/_env.sh"
cd "$REPO_ROOT"
if [ $# -gt 0 ]; then
  exec dotnet test tests/IE.Tests/IE.Tests.csproj --nologo --filter "$1"
fi
exec dotnet test InfiniteExtraction.sln --nologo
