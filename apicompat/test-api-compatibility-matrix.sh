#!/bin/bash

# API Compatibility Test Matrix Runner
# Runs compatibility tests against all API versions defined in apis.json

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
APIS_FILE="$PROJECT_ROOT/apicompat/apis.json"
TEST_SCRIPT="$PROJECT_ROOT/apicompat/test-api-compatibility.sh"

echo "🔍 API Compatibility Matrix Test Runner"
echo "========================================"
echo ""

# Check if required files exist
if [ ! -f "$APIS_FILE" ]; then
    echo "❌ ERROR: Baselines config file not found: $APIS_FILE"
    exit 1
fi

if [ ! -f "$TEST_SCRIPT" ]; then
    echo "❌ ERROR: Test script not found: $TEST_SCRIPT"
    exit 1
fi

# Load API versions from JSON file
echo "📋 Loading API versions from $APIS_FILE..."
APIS=()
while IFS= read -r version; do
    if [[ -n "$version" ]]; then
        APIS+=("$version")
        echo "   Found API: $version"
    fi
done < <(jq -r '.[].version' "$APIS_FILE")

if [ ${#APIS[@]} -eq 0 ]; then
    echo "❌ ERROR: No API versions found in $APIS_FILE"
    exit 1
fi

echo ""
echo "🚀 Running compatibility tests against ${#APIS[@]} API versions..."
echo ""

# Track results
TOTAL_TESTS=${#APIS[@]}
PASSED_TESTS=0
FAILED_TESTS=0
declare -a FAILED_APIS

# Run test for each API version
for api_version in "${APIS[@]}"; do
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo "🔍 Testing API version: $api_version"
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    
    if "$TEST_SCRIPT" "$api_version"; then
        echo "✅ $api_version: PASSED"
        PASSED_TESTS=$((PASSED_TESTS + 1))
    else
        echo "❌ $api_version: FAILED"
        FAILED_TESTS=$((FAILED_TESTS + 1))
        FAILED_APIS+=("$api_version")
    fi
    
    echo ""
done

# Final summary
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "📊 FINAL RESULTS"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "Total API versions tested: $TOTAL_TESTS"
echo "✅ Passed: $PASSED_TESTS"
echo "❌ Failed: $FAILED_TESTS"

if [ $FAILED_TESTS -gt 0 ]; then
    echo ""
    echo "Failed API versions:"
    for failed_api_version in "${FAILED_APIS[@]}"; do
        echo "  - $failed_api_version"
    done
    echo ""
    echo "💡 To debug individual failures, run:"
    for failed_api_version in "${FAILED_APIS[@]}"; do
        echo "   $TEST_SCRIPT $failed_api_version"
    done
    echo ""
    echo "❌ Some compatibility tests failed!"
    exit 1
else
    echo ""
    echo "🎉 All compatibility tests passed!"
    exit 0
fi