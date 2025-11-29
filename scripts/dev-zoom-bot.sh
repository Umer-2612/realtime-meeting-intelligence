#!/bin/bash

# Zoom Bot Local Development Helper Script
# This script helps you build and run the Zoom bot locally

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
ZOOM_BOT_DIR="$PROJECT_ROOT/apps/zoom-bot/src/demo"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${GREEN}=== Zoom Bot Development Helper ===${NC}\n"

# Function to check if running on macOS
is_macos() {
    [[ "$OSTYPE" == "darwin"* ]]
}

# Function to check if running on Linux
is_linux() {
    [[ "$OSTYPE" == "linux-gnu"* ]]
}

# Function to print usage
print_usage() {
    echo "Usage: $0 [command]"
    echo ""
    echo "Commands:"
    echo "  build-native    Build the Zoom bot natively (Linux only)"
    echo "  build-docker    Build the Zoom bot Docker image"
    echo "  run-native      Run the Zoom bot natively (Linux only)"
    echo "  run-docker      Run the Zoom bot in Docker"
    echo "  clean           Clean build artifacts"
    echo "  setup-sdk       Guide for setting up Zoom SDK"
    echo "  check-deps      Check if dependencies are installed"
    echo ""
}

# Function to check dependencies
check_dependencies() {
    echo -e "${YELLOW}Checking dependencies...${NC}\n"
    
    local missing_deps=()
    
    # Check CMake
    if ! command -v cmake &> /dev/null; then
        missing_deps+=("cmake")
    else
        echo -e "${GREEN}✓${NC} CMake: $(cmake --version | head -n1)"
    fi
    
    # Check GCC/G++
    if ! command -v g++ &> /dev/null; then
        missing_deps+=("g++")
    else
        echo -e "${GREEN}✓${NC} G++: $(g++ --version | head -n1)"
    fi
    
    # Check Docker
    if ! command -v docker &> /dev/null; then
        echo -e "${YELLOW}⚠${NC} Docker: Not installed (required for Docker builds)"
    else
        echo -e "${GREEN}✓${NC} Docker: $(docker --version)"
    fi
    
    # Check pkg-config
    if ! command -v pkg-config &> /dev/null; then
        missing_deps+=("pkg-config")
    else
        echo -e "${GREEN}✓${NC} pkg-config: $(pkg-config --version)"
    fi
    
    if [ ${#missing_deps[@]} -ne 0 ]; then
        echo -e "\n${RED}Missing dependencies: ${missing_deps[*]}${NC}"
        echo -e "${YELLOW}Install them using:${NC}"
        if is_linux; then
            echo "  sudo apt-get install ${missing_deps[*]}"
        elif is_macos; then
            echo "  brew install ${missing_deps[*]}"
        fi
        return 1
    fi
    
    echo -e "\n${GREEN}All dependencies are installed!${NC}\n"
    return 0
}

# Function to setup SDK
setup_sdk_guide() {
    echo -e "${YELLOW}=== Zoom SDK Setup Guide ===${NC}\n"
    echo "1. Download Zoom Meeting SDK from: https://marketplace.zoom.us"
    echo "2. Extract the SDK archive"
    echo "3. Copy files to the following locations:"
    echo ""
    echo "   Headers (h/*):"
    echo "     → $ZOOM_BOT_DIR/include/h/"
    echo ""
    echo "   Libraries (lib*.so):"
    echo "     → $ZOOM_BOT_DIR/lib/zoom_meeting_sdk/"
    echo ""
    echo "   Qt Libraries (qt_libs/*):"
    echo "     → $ZOOM_BOT_DIR/lib/zoom_meeting_sdk/qt_libs/"
    echo ""
    echo "   Translations (translations.json):"
    echo "     → $ZOOM_BOT_DIR/lib/zoom_meeting_sdk/json/"
    echo ""
    echo "4. Create symbolic link:"
    echo "   cd $ZOOM_BOT_DIR/lib/zoom_meeting_sdk"
    echo "   ln -sf libmeetingsdk.so libmeetingsdk.so.1"
    echo ""
    echo "5. Update config.txt with your meeting credentials"
    echo ""
}

# Function to build natively
build_native() {
    if is_macos; then
        echo -e "${RED}Error: Native build is not supported on macOS${NC}"
        echo -e "${YELLOW}Use 'build-docker' instead${NC}"
        exit 1
    fi
    
    echo -e "${YELLOW}Building Zoom bot natively...${NC}\n"
    
    cd "$ZOOM_BOT_DIR"
    
    # Create build directory
    mkdir -p build
    cd build
    
    # Run CMake
    echo -e "${YELLOW}Running CMake...${NC}"
    cmake ..
    
    # Build
    echo -e "${YELLOW}Building...${NC}"
    cmake --build . -- -j$(nproc)
    
    echo -e "\n${GREEN}Build complete!${NC}"
    echo -e "Binary location: $ZOOM_BOT_DIR/bin/MeetingSdkDemo"
}

# Function to build Docker image
build_docker() {
    echo -e "${YELLOW}Building Zoom bot Docker image...${NC}\n"
    
    cd "$PROJECT_ROOT"
    
    docker build -f apps/zoom-bot/docker/Dockerfile -t zoom-bot:dev apps/zoom-bot
    
    echo -e "\n${GREEN}Docker image built successfully!${NC}"
    echo -e "Image: zoom-bot:dev"
}

# Function to run natively
run_native() {
    if is_macos; then
        echo -e "${RED}Error: Native run is not supported on macOS${NC}"
        echo -e "${YELLOW}Use 'run-docker' instead${NC}"
        exit 1
    fi
    
    echo -e "${YELLOW}Running Zoom bot natively...${NC}\n"
    
    cd "$ZOOM_BOT_DIR/bin"
    
    # Set library path
    export LD_LIBRARY_PATH=$(pwd):$LD_LIBRARY_PATH
    
    # Run the demo
    ./MeetingSdkDemo
}

# Function to run Docker container
run_docker() {
    echo -e "${YELLOW}Running Zoom bot in Docker...${NC}\n"
    
    docker run -it --rm \
        -v "$ZOOM_BOT_DIR/config.txt:/opt/zoom-bot/bin/config.txt:ro" \
        zoom-bot:dev
}

# Function to clean build artifacts
clean() {
    echo -e "${YELLOW}Cleaning build artifacts...${NC}\n"
    
    cd "$ZOOM_BOT_DIR"
    
    rm -rf build
    rm -rf bin
    
    echo -e "${GREEN}Clean complete!${NC}"
}

# Main script logic
case "${1:-}" in
    build-native)
        check_dependencies && build_native
        ;;
    build-docker)
        build_docker
        ;;
    run-native)
        run_native
        ;;
    run-docker)
        run_docker
        ;;
    clean)
        clean
        ;;
    setup-sdk)
        setup_sdk_guide
        ;;
    check-deps)
        check_dependencies
        ;;
    *)
        print_usage
        exit 1
        ;;
esac
