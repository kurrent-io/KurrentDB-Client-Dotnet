#!/bin/bash

# Script to inspect the contents of a NuGet package on macOS.
# Usage: ./inspect_nuget.sh <path_to_nupkg_file>

set -e

NUPKG_FILE="$1"
TEMP_DIR="nuget_inspect_temp_$$"

if [ -z "$NUPKG_FILE" ]; then
  echo "Error: NuGet package file path not provided."
  echo "Usage: $0 <path_to_nupkg_file>"
  exit 1
fi

if [ ! -f "$NUPKG_FILE" ]; then
  echo "Error: File not found at '$NUPKG_FILE'"
  exit 1
fi

# Check if the file is a .nupkg file
if [[ "$NUPKG_FILE" != *.nupkg ]]; then
  echo "Error: The provided file does not have a .nupkg extension."
  exit 1
fi

echo "Inspecting NuGet package: $NUPKG_FILE"

# Create a temporary directory for extraction
mkdir "$TEMP_DIR"
echo "Created temporary directory: $TEMP_DIR"

# NuGet packages are ZIP archives, so we can use unzip
echo "Extracting package contents..."
unzip -q "$NUPKG_FILE" -d "$TEMP_DIR"

echo ""
echo "Listing all extracted files recursively:"
echo "----------------------------------------"
# List all files recursively
find "$TEMP_DIR" -type f
echo "----------------------------------------"
echo ""

## Clean up
#echo "Cleaning up temporary directory: $TEMP_DIR"
#rm -rf "$TEMP_DIR"
#
#echo "Deleting package: $NUPKG_FILE"
#rm -rf "$NUPKG_FILE"

echo "Inspection complete."
