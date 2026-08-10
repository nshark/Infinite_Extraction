#!/usr/bin/env bash
# Proves the architecture tests actually bite, by making them fail on purpose.
# A guardrail nobody has ever seen go red is not a guardrail.
#
# Injects a float field into IE.Model, expects the suite to FAIL, removes it,
# expects the suite to PASS. Always cleans up after itself.
. "$(dirname "${BASH_SOURCE[0]}")/_env.sh"
cd "$REPO_ROOT"
CANARY="src/IE.Model/__GuardrailCanary.cs"
cleanup() { rm -f "$CANARY"; }
trap cleanup EXIT

cat > "$CANARY" <<'CANARYEOF'
namespace IE.Model;

// Temporary file written by scripts/verify-guardrails.sh. Deleted automatically.
internal sealed class GuardrailCanary
{
    public float Drift;
}
CANARYEOF

echo "==> canary planted; expecting the no-floats test to FAIL"
if dotnet test tests/IE.Tests/IE.Tests.csproj --nologo --filter 'FullyQualifiedName~NoFloatingPoint' >/dev/null 2>&1; then
  echo "[fail] the no-floats test PASSED with a float field in IE.Model -- the guardrail is broken" >&2
  exit 1
fi
echo "    red, as expected"

cleanup
echo "==> canary removed; expecting the suite to PASS"
dotnet test InfiniteExtraction.sln --nologo
echo "==> guardrails verified"
