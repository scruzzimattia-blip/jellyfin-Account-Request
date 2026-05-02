#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$ROOT_DIR/Jellyfin.Plugin.AccountRequest"
PROJECT_FILE="$PROJECT_DIR/Jellyfin.Plugin.AccountRequest.csproj"
OUTPUT_DIR="$PROJECT_DIR/bin/Release/net9.0"
DIST_DIR="$ROOT_DIR/dist"

dotnet build "$PROJECT_FILE" --configuration Release

rm -rf "$DIST_DIR"
mkdir -p "$DIST_DIR"

cp "$OUTPUT_DIR/Jellyfin.Plugin.AccountRequest.dll" "$DIST_DIR/"
cp "$OUTPUT_DIR/Jellyfin.Plugin.AccountRequest.deps.json" "$DIST_DIR/"

if [ -f "$ROOT_DIR/meta.json" ]; then
    cp "$ROOT_DIR/meta.json" "$DIST_DIR/"
fi

echo "Release artifacts copied to $DIST_DIR"
