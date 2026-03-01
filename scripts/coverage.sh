#!/bin/bash
set -e

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"

echo -e "${YELLOW}Running tests with coverage...${NC}"

# Clean previous results
rm -rf "$ROOT_DIR/coverage" "$ROOT_DIR/coverage-report"

# Run tests with coverage collection
dotnet test "$ROOT_DIR" \
  --settings "$ROOT_DIR/tests/coverage.runsettings" \
  --collect:"XPlat Code Coverage" \
  --results-directory "$ROOT_DIR/coverage"

echo -e "${YELLOW}Generating HTML report...${NC}"

# Check if reportgenerator is installed
if ! command -v reportgenerator &> /dev/null; then
  echo -e "${YELLOW}Installing reportgenerator...${NC}"
  dotnet tool install -g dotnet-reportgenerator-globaltool
fi

# Generate report
reportgenerator \
  -reports:"$ROOT_DIR/coverage/**/coverage.cobertura.xml" \
  -targetdir:"$ROOT_DIR/coverage-report" \
  -reporttypes:"Html;TextSummary;Cobertura"

echo ""
echo -e "${GREEN}Coverage report generated!${NC}"
echo ""
cat "$ROOT_DIR/coverage-report/Summary.txt"
echo ""
echo -e "Open ${GREEN}coverage-report/index.html${NC} in your browser to view detailed coverage."
