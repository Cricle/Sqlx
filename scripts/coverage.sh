#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat <<'EOF'
Usage: scripts/coverage.sh [--unit|--e2e]

  --unit   Run unit tests only (exclude E2E).
  --e2e    Run E2E tests only.
  default  Run the full Sqlx.Tests suite.
EOF
}

mode="all"
if [[ $# -gt 1 ]]; then
  usage
  exit 1
fi

if [[ $# -eq 1 ]]; then
  case "$1" in
    --unit) mode="unit" ;;
    --e2e) mode="e2e" ;;
    -h|--help) usage; exit 0 ;;
    *) usage; exit 1 ;;
  esac
fi

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "$script_dir/.." && pwd)"
project="$repo_root/tests/Sqlx.Tests/Sqlx.Tests.csproj"
results_dir="$repo_root/TestResults"
report_dir="$repo_root/CoverageReport"

rm -rf "$results_dir" "$report_dir"
mkdir -p "$results_dir"

args=(
  test "$project"
  --collect:"XPlat Code Coverage"
  --results-directory "$results_dir"
  --verbosity minimal
)

case "$mode" in
  unit)
    args+=(--filter "FullyQualifiedName!~E2E")
    ;;
  e2e)
    args+=(--filter "FullyQualifiedName~E2E")
    ;;
esac

dotnet "${args[@]}"

coverage_file="$(find "$results_dir" -name coverage.cobertura.xml -print -quit)"
if [[ -z "${coverage_file:-}" ]]; then
  echo "coverage.cobertura.xml not found"
  exit 1
fi

python3 - "$coverage_file" <<'PY'
import sys
import xml.etree.ElementTree as ET

path = sys.argv[1]
root = ET.parse(path).getroot()
line_rate = float(root.attrib["line-rate"]) * 100
branch_rate = float(root.attrib.get("branch-rate", "0")) * 100
print(f"Line: {line_rate:.2f}%")
print(f"Branch: {branch_rate:.2f}%")
PY

if command -v reportgenerator >/dev/null 2>&1; then
  reportgenerator \
    -reports:"$coverage_file" \
    -targetdir:"$report_dir" \
    -reporttypes:"Html;TextSummary" >/dev/null
  echo "HTML: $report_dir/index.html"
fi
