#!/bin/bash
# Run all benchmarks with memory diagnostics
# Usage: ./run-benchmarks.sh [filter]
# Examples:
#   ./run-benchmarks.sh                          # Run all benchmarks
#   ./run-benchmarks.sh --filter '*Lifecycle*'   # Run only lifecycle benchmarks
#   ./run-benchmarks.sh --filter '*String*'      # Run only string extension benchmarks
#   ./run-benchmarks.sh --filter '*Render*'      # Run only render path benchmarks

set -e
cd "$(dirname "$0")"

dotnet run -c Release -- "$@"
