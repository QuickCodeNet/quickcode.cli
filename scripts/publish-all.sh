#!/bin/bash
set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
PROJECT_PATH="$PROJECT_ROOT/src/QuickCode.Cli/QuickCode.Cli.csproj"
OUTPUT_DIR="$PROJECT_ROOT/dist"
VERSION=$(grep -oP '<Version>\K[^<]+' "$PROJECT_ROOT/Directory.Build.props" | head -1)

echo -e "${GREEN}Building QuickCode.Cli v${VERSION}${NC}"
echo "Project: $PROJECT_PATH"
echo "Output: $OUTPUT_DIR"
echo ""

# Clean output directory
rm -rf "$OUTPUT_DIR"
mkdir -p "$OUTPUT_DIR"

# Function to publish for a specific RID
publish_rid() {
    local RID=$1
    local PLATFORM_NAME=$2
    local EXT=$3
    
    echo -e "${YELLOW}Publishing for $PLATFORM_NAME ($RID)...${NC}"
    
    dotnet publish "$PROJECT_PATH" \
        -c Release \
        -r "$RID" \
        --self-contained true \
        -p:PublishSingleFile=true \
        -p:IncludeNativeLibrariesForSelfExtract=true \
        -p:IncludeAllContentForSelfExtract=true \
        -p:PublishTrimmed=false \
        -o "$OUTPUT_DIR/$RID"
    
    # Create archive
    local ARCHIVE_NAME="quickcode-cli-${PLATFORM_NAME}-v${VERSION}.${EXT}"
    local ARCHIVE_PATH="$OUTPUT_DIR/$ARCHIVE_NAME"
    
    if [ "$EXT" = "zip" ]; then
        cd "$OUTPUT_DIR/$RID"
        zip -r "$ARCHIVE_PATH" . > /dev/null
        cd "$PROJECT_ROOT"
    else
        tar -czf "$ARCHIVE_PATH" -C "$OUTPUT_DIR/$RID" .
    fi
    
    # Create checksum
    if command -v shasum &> /dev/null; then
        shasum -a 256 "$ARCHIVE_PATH" > "${ARCHIVE_PATH}.sha256"
        echo -e "${GREEN}✓ Created $ARCHIVE_NAME${NC}"
    fi
    
    echo ""
}

# Publish for all platforms
publish_rid "osx-arm64" "osx-arm64" "tar.gz"
publish_rid "osx-x64" "osx-x64" "tar.gz"
publish_rid "win-x64" "win-x64" "zip"
publish_rid "win-arm64" "win-arm64" "zip"

# List all artifacts
echo -e "${GREEN}Build completed! Artifacts:${NC}"
ls -lh "$OUTPUT_DIR"/*.{tar.gz,zip} 2>/dev/null || true
echo ""
echo -e "${GREEN}Checksums:${NC}"
ls -lh "$OUTPUT_DIR"/*.sha256 2>/dev/null || true

