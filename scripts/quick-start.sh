#!/bin/bash

# Quick Start Script for Realtime Meeting Intelligence Monorepo
# This script provides quick commands for common development tasks

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

echo -e "${BLUE}=== Realtime Meeting Intelligence - Quick Start ===${NC}\n"

print_usage() {
    echo "Usage: $0 [command]"
    echo ""
    echo "Commands:"
    echo "  start-all       Start all services with Docker Compose"
    echo "  stop-all        Stop all services"
    echo "  logs            View logs from all services"
    echo "  zoom-bot        Quick start Zoom bot development"
    echo "  stream          Quick start Stream Processor development"
    echo "  status          Show status of all services"
    echo "  clean           Clean all build artifacts"
    echo ""
}

start_all() {
    echo -e "${YELLOW}Starting all services with Docker Compose...${NC}\n"
    cd "$PROJECT_ROOT"
    docker-compose up -d
    echo -e "\n${GREEN}All services started!${NC}"
    echo -e "Use './scripts/quick-start.sh logs' to view logs"
}

stop_all() {
    echo -e "${YELLOW}Stopping all services...${NC}\n"
    cd "$PROJECT_ROOT"
    docker-compose down
    echo -e "\n${GREEN}All services stopped!${NC}"
}

show_logs() {
    echo -e "${YELLOW}Showing logs from all services...${NC}\n"
    cd "$PROJECT_ROOT"
    docker-compose logs -f
}

zoom_bot_dev() {
    echo -e "${BLUE}=== Zoom Bot Development ===${NC}\n"
    echo "Available commands:"
    echo "  ./scripts/dev-zoom-bot.sh build-docker    # Build Docker image"
    echo "  ./scripts/dev-zoom-bot.sh run-docker      # Run in Docker"
    echo "  ./scripts/dev-zoom-bot.sh check-deps      # Check dependencies"
    echo "  ./scripts/dev-zoom-bot.sh setup-sdk       # SDK setup guide"
    echo ""
    echo "Configuration file: apps/zoom-bot/src/demo/config.txt"
    echo ""
}

stream_dev() {
    echo -e "${BLUE}=== Stream Processor Development ===${NC}\n"
    echo "Quick start:"
    echo "  cd services/stream-processor"
    echo "  python -m venv venv"
    echo "  source venv/bin/activate"
    echo "  pip install -r requirements.txt"
    echo "  python main.py"
    echo ""
}

show_status() {
    echo -e "${YELLOW}Service Status:${NC}\n"
    cd "$PROJECT_ROOT"
    
    if command -v docker-compose &> /dev/null; then
        docker-compose ps
    else
        echo "Docker Compose not installed"
    fi
}

clean_all() {
    echo -e "${YELLOW}Cleaning all build artifacts...${NC}\n"
    
    # Clean Zoom bot
    if [ -d "$PROJECT_ROOT/apps/zoom-bot/src/demo/build" ]; then
        rm -rf "$PROJECT_ROOT/apps/zoom-bot/src/demo/build"
        echo "✓ Cleaned Zoom bot build"
    fi
    
    if [ -d "$PROJECT_ROOT/apps/zoom-bot/src/demo/bin" ]; then
        rm -rf "$PROJECT_ROOT/apps/zoom-bot/src/demo/bin"
        echo "✓ Cleaned Zoom bot bin"
    fi
    
    # Clean Python cache
    find "$PROJECT_ROOT" -type d -name "__pycache__" -exec rm -rf {} + 2>/dev/null || true
    find "$PROJECT_ROOT" -type d -name "*.egg-info" -exec rm -rf {} + 2>/dev/null || true
    echo "✓ Cleaned Python cache"
    
    echo -e "\n${GREEN}Clean complete!${NC}"
}

case "${1:-}" in
    start-all)
        start_all
        ;;
    stop-all)
        stop_all
        ;;
    logs)
        show_logs
        ;;
    zoom-bot)
        zoom_bot_dev
        ;;
    stream)
        stream_dev
        ;;
    status)
        show_status
        ;;
    clean)
        clean_all
        ;;
    *)
        print_usage
        exit 1
        ;;
esac
