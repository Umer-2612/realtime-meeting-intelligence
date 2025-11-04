#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

ZOOM_IMAGE_TAG="${ZOOM_IMAGE_TAG:-zoom-bot:latest}"
STREAM_IMAGE_TAG="${STREAM_IMAGE_TAG:-stream-processor:latest}"

pushd "$REPO_ROOT" >/dev/null

echo "Building Zoom bot image: $ZOOM_IMAGE_TAG"
docker build \
  -f apps/zoom-bot/docker/Dockerfile \
  -t "$ZOOM_IMAGE_TAG" \
  apps/zoom-bot

echo "Building stream processor image: $STREAM_IMAGE_TAG"
docker build \
  -f services/stream-processor/Dockerfile \
  -t "$STREAM_IMAGE_TAG" \
  services/stream-processor

echo "Images built successfully"
