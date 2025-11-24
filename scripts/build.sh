#!/bin/bash
set -e

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
PROJECT_PATH="$PROJECT_ROOT/src/QuickCode.Cli/QuickCode.Cli.csproj"

# Detect platform
if [[ "$OSTYPE" == "darwin"* ]]; then
    if [[ $(uname -m) == "arm64" ]]; then
        RID="osx-arm64"
    else
        RID="osx-x64"
    fi
    EXT="tar.gz"
elif [[ "$OSTYPE" == "linux-gnu"* ]]; then
    RID="linux-x64"
    EXT="tar.gz"
else
    echo "Unsupported platform: $OSTYPE"
    exit 1
fi

echo -e "${GREEN}Building for $RID...${NC}"

dotnet publish "$PROJECT_PATH" \
    -c Release \
    -r "$RID" \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -p:IncludeAllContentForSelfExtract=true \
    -p:PublishTrimmed=false

echo -e "${GREEN}Build completed!${NC}"
echo "Output: $PROJECT_ROOT/src/QuickCode.Cli/bin/Release/net10.0/$RID/publish/"

