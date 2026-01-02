#!/usr/bin/env bash
# Run Vintage Story client with the LiveMap mod from the Release folder

set -e

PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
RELEASE_DIR="$PROJECT_ROOT/Release"
RUN_DATA_DIR="$PROJECT_ROOT/runDataClient"

# Colors
RED='\033[0;31m'
CYAN='\033[0;36m'
GRAY='\033[0;90m'
NC='\033[0m'

# Check for VINTAGE_STORY environment variable
if [[ -z "$VINTAGE_STORY" ]]; then
    echo -e "${RED}Error: VINTAGE_STORY environment variable is not set.${NC}"
    echo "Please set it to your Vintage Story installation directory."
    exit 1
fi

VINTAGE_STORY_EXE="$VINTAGE_STORY/Vintagestory"

if [[ ! -f "$VINTAGE_STORY_EXE" ]]; then
    echo -e "${RED}Error: Vintagestory not found at: $VINTAGE_STORY_EXE${NC}"
    exit 1
fi

if [[ ! -d "$RELEASE_DIR" ]]; then
    echo -e "${RED}Error: Release directory not found. Run build.sh first.${NC}"
    exit 1
fi

echo -e "${CYAN}Starting Vintage Story Server with LiveMap mod...${NC}"
echo -e "${GRAY}  Exe: $VINTAGE_STORY_EXE${NC}"
echo -e "${GRAY}  Mod Path: $RELEASE_DIR${NC}"
echo -e "${GRAY}  Data Path: $RUN_DATA_DIR${NC}"
echo ""

"$VINTAGE_STORY_EXE" --tracelog --addModPath "$RELEASE_DIR" --dataPath "$RUN_DATA_DIR"
