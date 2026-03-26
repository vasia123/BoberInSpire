#!/bin/bash
# Prepare workshop content folder for Steam Workshop upload.
# Run from the project root: bash workshop/prepare.sh

set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
CONTENT_DIR="$SCRIPT_DIR/content"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

# Default game mods path (override with $MODS_DIR)
MODS_DIR="${MODS_DIR:-C:/Program Files (x86)/Steam/steamapps/common/Slay the Spire 2/mods}"

echo "=== SmartPick Workshop Preparation ==="

# 1. Build the mod
echo "[1/3] Building mod..."
cd "$PROJECT_ROOT"
dotnet build SmartPick.csproj -c Release

# 2. Copy files to content folder
echo "[2/3] Copying files to workshop/content..."
rm -rf "$CONTENT_DIR"
mkdir -p "$CONTENT_DIR"

cp "$MODS_DIR/SmartPick.dll" "$CONTENT_DIR/"
cp "$MODS_DIR/SmartPick.pck" "$CONTENT_DIR/"
cp "$MODS_DIR/SmartPick.json" "$CONTENT_DIR/"

# 3. Check preview image
echo "[3/3] Checking preview image..."
if [ ! -f "$SCRIPT_DIR/preview.png" ]; then
    echo "WARNING: workshop/preview.png not found!"
    echo "  Add a 512x512 PNG preview image before uploading."
else
    echo "  Preview image found."
fi

echo ""
echo "=== Done! ==="
echo "Content ready in: $CONTENT_DIR"
echo ""
echo "To upload, run:"
echo "  steamcmd +login YOUR_LOGIN +workshop_build_item \"$SCRIPT_DIR/workshop_item.vdf\" +quit"
echo ""
echo "After first upload, save the publishedfileid into workshop_item.vdf for future updates."
