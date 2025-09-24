# API Compatibility Testing

This directory contains API compatibility testing for the Spiffy.Monitoring library using Microsoft's [`apicompat`](https://www.nuget.org/packages/Microsoft.DotNet.ApiCompat.Tool/) tool to ensure backward compatibility against multiple API versions.

## Directory Structure

```
apicompat/
├── README.md                         # This documentation
├── apis.json                         # API versions configuration
├── test-api-compatibility.sh         # Main compatibility test script
└── test-api-compatibility-matrix.sh  # Matrix test runner
```

## Configuration

API versions are defined in `apis.json` with structured data:

```json
[
  {
    "version": "6.4.6",
    "release_date": "2025-09-22"
  },
  {
    "version": "6.1.0",
    "release_date": "2021-11-02"
  }
]
```

The script automatically detects the appropriate target framework (.NET Standard 2.0, .NET Standard 1.3, or .NET Framework 4.0) by probing the NuGet package structure.

## Usage

### Local Testing

**Test all API versions:**
```bash
# Install the Microsoft apicompat tool
dotnet tool install --global Microsoft.DotNet.ApiCompat.Tool

# Test all versions (summary output)
./apicompat/test-api-compatibility.sh

# Test all versions (detailed progress)
./apicompat/test-api-compatibility-matrix.sh
```

**Test a specific version:**
```bash
./apicompat/test-api-compatibility.sh 6.3.1
```

The scripts automatically:
- Build the current version of Spiffy.Monitoring
- Download specified API versions from NuGet  
- Run compatibility analysis using Microsoft's tool
- Report any breaking changes with detailed output

### Suppression Files

Each suppression file contains known breaking changes for a specific API version that have been reviewed and accepted. These files are in Microsoft's standard XML suppression format and contain:

- **DiagnosticId**: The type of compatibility issue (e.g., CP0002 for missing members)
- **Target**: The specific API member that changed

Example suppression file:
```xml
<?xml version="1.0" encoding="utf-8"?>
<Suppressions xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
  <Suppression>
    <DiagnosticId>CP0002</DiagnosticId>
    <Target>M:Spiffy.Monitoring.Config.InitializationApi.ProvidersApi.#ctor(Spiffy.Monitoring.Config.InitializationApi)</Target>
  </Suppression>
</Suppressions>
```

## Usage

### Local Testing

**Test all APIs:**
```bash
# Make sure the tool is installed
dotnet tool install --global Microsoft.DotNet.ApiCompat.Tool

# Quick test (shows summary only)
././apicompat/test-api-compatibility.sh

# Detailed test with progress indicators
././apicompat/test-api-compatibility-matrix.sh
```

**Test a specific API:**
```bash
././apicompat/test-api-compatibility.sh 6.1.0
```

## Handling Breaking Changes  

When compatibility tests detect breaking changes, review the detailed output to understand what changed. Since this project maintains strict backward compatibility, any breaking changes should be addressed by fixing the implementation rather than suppressing the issues.

The Microsoft.DotNet.ApiCompat.Tool provides detailed diagnostic information including:
- Missing types or members
- Signature changes
- Accessibility changes
- Breaking changes across assemblies

### Manual Testing

For manual testing or debugging, you can run the Microsoft apicompat tool directly:

```bash
# Build the current version
dotnet build src/Spiffy.Monitoring/Spiffy.Monitoring.csproj --configuration Release

# Test against a specific API version (example: 6.1.0)
apicompat \
  --left ~/.nuget/packages/spiffy.monitoring/6.1.0/lib/netstandard2.0/Spiffy.Monitoring.dll \
  --right ./src/Spiffy.Monitoring/bin/Release/netstandard2.0/Spiffy.Monitoring.dll
```

## Troubleshooting

**"No API found for version X.X.X"**
- The script automatically downloads packages from NuGet
- Ensure internet connectivity and that the version exists in NuGet package history

**"Command not found: apicompat"** 
- Install the tool globally: `dotnet tool install --global Microsoft.DotNet.ApiCompat.Tool`

**Build issues**
- Ensure the project builds successfully before running compatibility tests
- Check that the output directory contains the expected assemblies

The matrix test runner provides visual progress indicators, clear separation between API tests, and comprehensive pass/fail summaries for easy debugging.