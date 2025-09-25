#!/bin/bash

set -e

# Get the directory of this script and project root
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
APIS_FILE="$PROJECT_ROOT/apicompat/apis.json"

# Load API versions from JSON config
load_apis_from_config() {
    DEFAULT_APIS=()
    if [ -f "$APIS_FILE" ]; then
        # Extract versions using jq
        while IFS= read -r version; do
            if [[ -n "$version" ]]; then
                DEFAULT_APIS+=("$version")
            fi
        done < <(jq -r '.[].version' "$APIS_FILE")
    fi
    
    # Exit if config file not found or empty
    if [ ${#DEFAULT_APIS[@]} -eq 0 ]; then
        echo "❌ ERROR: Could not load APIs from $APIS_FILE"
        echo "Please ensure the file exists and contains valid JSON."
        exit 1
    fi
}

# Load APIs from config
load_apis_from_config

# Allow specifying a single API version via command line argument
if [ $# -eq 1 ]; then
    APIS=("$1")
    echo "🔍 API Compatibility Testing (Single API: $1)"
else
    APIS=("${DEFAULT_APIS[@]}")
    echo "🔍 API Compatibility Testing (All APIs)"
fi

echo "============================="
echo ""

# Check if Microsoft.DotNet.ApiCompat.Tool is installed
if ! command -v apicompat &> /dev/null; then
    echo "Installing Microsoft.DotNet.ApiCompat.Tool..."
    dotnet tool install --global Microsoft.DotNet.ApiCompat.Tool
    echo ""
fi

# Build the current version
echo "📦 Building current version..."
dotnet build src/Spiffy.Monitoring/Spiffy.Monitoring.csproj --configuration Release --no-restore
echo ""

# Create a temporary directory for baselines
TEMP_DIR=$(mktemp -d)
echo "📁 Using temporary directory: $TEMP_DIR"
echo ""

# Function to cleanup on exit
cleanup() {
    echo "🧹 Cleaning up temporary directory..."
    rm -rf "$TEMP_DIR"
}
trap cleanup EXIT

# Test each baseline version
TOTAL_TESTS=0
PASSED_TESTS=0
FAILED_TESTS=0

for api_version in "${APIS[@]}"; do
    echo "🔍 Testing against API version $api_version..."
    TOTAL_TESTS=$((TOTAL_TESTS + 1))
    
    # Download the API version
    api_dir="$TEMP_DIR/apis/$api_version"
    mkdir -p "$api_dir"
    cd "$api_dir"
    
    # Create a temporary project to download the specific version
    dotnet new console -n TempApp --force > /dev/null 2>&1
    cd TempApp
    
    # Add the specific version of Spiffy.Monitoring
    dotnet add package Spiffy.Monitoring --version "$api_version" > /dev/null 2>&1
    dotnet restore > /dev/null 2>&1
    
    # Find the assembly - all current APIs use netstandard2.0
    api_dll=$(find ~/.nuget/packages/spiffy.monitoring/$api_version -name "Spiffy.Monitoring.dll" -path "*/netstandard2.0/*" | head -1)
    
    if [ -z "$api_dll" ]; then
        echo "❌ ERROR: Could not find Spiffy.Monitoring.dll for version $api_version"
        FAILED_TESTS=$((FAILED_TESTS + 1))
        continue
    fi
    
    # Go back to the project root for apicompat execution
    cd "$PROJECT_ROOT"
    
    # Build apicompat command with absolute paths
    current_dll="$PROJECT_ROOT/src/Spiffy.Monitoring/bin/Release/netstandard2.0/Spiffy.Monitoring.dll"
    apicompat_cmd="apicompat --left \"$api_dll\" --right \"$current_dll\""
    
    # Run the compatibility check (disable exit on error temporarily)
    echo "   Running apicompat check..."
    set +e
    apicompat_output=$(eval "$apicompat_cmd" 2>&1)
    apicompat_exit_code=$?
    set -e
    
    if [ $apicompat_exit_code -eq 0 ]; then
        echo "   ✅ PASS"
        PASSED_TESTS=$((PASSED_TESTS + 1))
    else
        echo "   ❌ FAIL - API breaking changes detected"
        echo ""
        echo "   🔍 API Compatibility Issues:"
        echo "$apicompat_output" | sed 's/^/      /'
        echo ""
        FAILED_TESTS=$((FAILED_TESTS + 1))
    fi
    echo ""
done

# Summary
echo "📊 Summary"
echo "=========="
echo "Total tests: $TOTAL_TESTS"
echo "Passed: $PASSED_TESTS"
echo "Failed: $FAILED_TESTS"

if [ $FAILED_TESTS -gt 0 ]; then
    echo ""
    echo "❌ Some compatibility tests failed!"
    exit 1
else
    echo ""
    echo "✅ All compatibility tests passed!"
fi