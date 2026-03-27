#!/usr/bin/env bash
# Build script for LiveMap mod

set -e

PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_FILE="$PROJECT_ROOT/livemap.csproj"

# Default values
CONFIGURATION="Release"
CLEAN=false
SKIP_WEB=false

# Colors
CYAN='\033[0;36m'
GREEN='\033[0;32m'
RED='\033[0;31m'
MAGENTA='\033[0;35m'
YELLOW='\033[0;33m'
GRAY='\033[0;90m'
NC='\033[0m' # No Color

step() { echo -e "${CYAN}==> $1${NC}"; }
success() { echo -e "${GREEN}✓ $1${NC}"; }
error() { echo -e "${RED}✗ $1${NC}"; }

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -c|--configuration)
            CONFIGURATION="$2"
            shift 2
        ;;
        --clean)
            CLEAN=true
            shift
        ;;
        --skip-web)
            SKIP_WEB=true
            shift
        ;;
        -h|--help)
            echo "Usage: $0 [options]"
            echo ""
            echo "Options:"
            echo "  -c, --configuration <Debug|Release>  Build configuration (default: Release)"
            echo "  --clean                              Clean before building"
            echo "  --skip-web                           Skip building web frontend"
            echo "  -h, --help                           Show this help message"
            exit 0
        ;;
        *)
            echo "Unknown option: $1"
            exit 1
        ;;
    esac
done

echo ""
echo -e "${MAGENTA}LiveMap Build Script${NC}"
echo -e "${MAGENTA}====================${NC}"
echo ""

# Check for VINTAGE_STORY environment variable
if [[ -z "$VINTAGE_STORY" ]]; then
    error "VINTAGE_STORY environment variable is not set."
    echo "Please set it to your Vintage Story installation directory."
    echo "Example: export VINTAGE_STORY=\"/home/user/.config/vintagestory\""
    exit 1
fi

if [[ ! -d "$VINTAGE_STORY" ]]; then
    error "VINTAGE_STORY path does not exist: $VINTAGE_STORY"
    exit 1
fi

# Ensure jq exists
if ! command -v jq &> /dev/null; then
    error "jq could not be found. Please install it before running this script."
    exit 1
fi

success "VINTAGE_STORY: $VINTAGE_STORY"

# Clean if requested
if [[ "$CLEAN" == true ]]; then
    step "Cleaning build artifacts..."
    dotnet clean "$PROJECT_FILE" -c "$CONFIGURATION" --nologo -v q
    rm -rf "$PROJECT_ROOT/web/dist"
    success "Clean complete"
fi

# Build web frontend
if [[ "$SKIP_WEB" == false ]]; then
    step "Building web frontend..."
    pushd "$PROJECT_ROOT/web" > /dev/null
    npm install --silent
    npm run build
    popd > /dev/null
    success "Web frontend built"
fi

# Build the mod
step "Building mod ($CONFIGURATION)..."
dotnet build "$PROJECT_FILE" -c "$CONFIGURATION" --nologo
success "Build complete"

# Find the output zip
LATEST_ZIP=$(find "$PROJECT_ROOT/bin" -name "*.zip" -type f -printf '%T@ %p\n' 2>/dev/null | sort -rn | head -1 | cut -d' ' -f2-)
if [[ -n "$LATEST_ZIP" ]]; then
    ZIP_SIZE=$(du -h "$LATEST_ZIP" | cut -f1)
    echo ""
    echo -e "${GREEN}Package created:${NC}"
    echo -e "  ${YELLOW}$LATEST_ZIP${NC}"
    echo -e "  ${GRAY}Size: $ZIP_SIZE${NC}"
fi

# Move the zip out into a Release folder
RELEASE_DIR="$PROJECT_ROOT/Release"
if [[ -d "$RELEASE_DIR" ]]; then
    rm -f "$RELEASE_DIR"/*.zip
else
    mkdir "$RELEASE_DIR"
fi
mv "$LATEST_ZIP" "$RELEASE_DIR/"
# append version to zip name
VERSION=$(cat "$PROJECT_ROOT/resources/modinfo.json" | jq -r .version)
NEW_ZIP_NAME="LiveMap-$VERSION.zip"
mv "$RELEASE_DIR/livemap.zip" "$RELEASE_DIR/$NEW_ZIP_NAME"

echo ""
success "Build completed successfully!"
