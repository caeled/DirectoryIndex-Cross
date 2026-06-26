#!/bin/bash
set -e

APP_NAME="Directory Index Generator"
BUNDLE_ID="com.stevepowell.directoryindexgenerator"
VERSION="1.0.0"
PUBLISH_DIR="./publish-mac"
APP_BUNDLE="${APP_NAME}.app"
DMG_NAME="DirectoryIndexGenerator-mac.dmg"

echo "==> Building..."
dotnet publish DirectoryIndexGenerator.csproj -c Release -r osx-x64 -o "$PUBLISH_DIR"

echo "==> Creating .app bundle..."
rm -rf "$APP_BUNDLE"
mkdir -p "$APP_BUNDLE/Contents/MacOS"
mkdir -p "$APP_BUNDLE/Contents/Resources"

# Copy all published files into the bundle
cp -r "$PUBLISH_DIR"/. "$APP_BUNDLE/Contents/MacOS/"

# Make the executable actually executable
chmod +x "$APP_BUNDLE/Contents/MacOS/DirectoryIndexGenerator"

# Write Info.plist
cat > "$APP_BUNDLE/Contents/Info.plist" << EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleIdentifier</key>
    <string>${BUNDLE_ID}</string>
    <key>CFBundleName</key>
    <string>${APP_NAME}</string>
    <key>CFBundleDisplayName</key>
    <string>${APP_NAME}</string>
    <key>CFBundleExecutable</key>
    <string>DirectoryIndexGenerator</string>
    <key>CFBundleVersion</key>
    <string>${VERSION}</string>
    <key>CFBundleShortVersionString</key>
    <string>${VERSION}</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleIconFile</key>
    <string>AppIcon</string>
    <key>LSMinimumSystemVersion</key>
    <string>10.15</string>
    <key>NSHighResolutionCapable</key>
    <true/>
    <key>NSPrincipalClass</key>
    <string>NSApplication</string>
</dict>
</plist>
EOF

# Copy icon if it exists
if [ -f "Assets/icon.png" ]; then
    # Convert PNG to ICNS using built-in sips
    mkdir -p /tmp/AppIcon.iconset
    sips -z 16 16     Assets/icon.png --out /tmp/AppIcon.iconset/icon_16x16.png    2>/dev/null
    sips -z 32 32     Assets/icon.png --out /tmp/AppIcon.iconset/icon_16x16@2x.png 2>/dev/null
    sips -z 32 32     Assets/icon.png --out /tmp/AppIcon.iconset/icon_32x32.png    2>/dev/null
    sips -z 64 64     Assets/icon.png --out /tmp/AppIcon.iconset/icon_32x32@2x.png 2>/dev/null
    sips -z 128 128   Assets/icon.png --out /tmp/AppIcon.iconset/icon_128x128.png  2>/dev/null
    sips -z 256 256   Assets/icon.png --out /tmp/AppIcon.iconset/icon_128x128@2x.png 2>/dev/null
    sips -z 256 256   Assets/icon.png --out /tmp/AppIcon.iconset/icon_256x256.png  2>/dev/null
    sips -z 512 512   Assets/icon.png --out /tmp/AppIcon.iconset/icon_256x256@2x.png 2>/dev/null
    sips -z 512 512   Assets/icon.png --out /tmp/AppIcon.iconset/icon_512x512.png  2>/dev/null
    iconutil -c icns /tmp/AppIcon.iconset -o "$APP_BUNDLE/Contents/Resources/AppIcon.icns" 2>/dev/null || true
    rm -rf /tmp/AppIcon.iconset
fi

echo "==> Creating DMG..."
rm -f "$DMG_NAME"
hdiutil create -volname "$APP_NAME" -srcfolder "$APP_BUNDLE" -ov -format UDZO "$DMG_NAME"

echo ""
echo "Done! Created: $DMG_NAME"
echo "Distribute that file. Users drag the app to their Applications folder."
