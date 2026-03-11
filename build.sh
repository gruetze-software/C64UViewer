#!/bin/bash

# 1. Konfiguration
PROJ="C64UViewer.csproj"
APP_NAME="c64uviewer"
BUILD_ROOT="dist/linux_pkg"

echo "--- Starte Build-Prozess ---"
echo "Raeume alte Dateien auf..."
rm -rf obj bin dist
mkdir -p $BUILD_ROOT/usr/share/applications
mkdir -p $BUILD_ROOT/usr/share/icons/hicolor/256x256/apps
mkdir -p $BUILD_ROOT/usr/share/$APP_NAME
mkdir -p $BUILD_ROOT/usr/local/bin
mkdir -p $BUILD_ROOT/DEBIAN

echo "Hole Versionsinfo aus der csproj Datei..."
VERSION=$(grep -oPm1 "(?<=<Version>)[^<]+" C64UViewer.csproj)

# 2. App bauen
echo "Kompiliere Linux Version (Self-Contained)..."
dotnet publish $PROJ -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true --output ./dist/temp_publish

# 3. Dateien kopieren
cp -r ./dist/temp_publish/* $BUILD_ROOT/usr/share/$APP_NAME/
cp c64uviewer.desktop $BUILD_ROOT/usr/share/applications/
cp Assets/icon.png $BUILD_ROOT/usr/share/icons/hicolor/256x256/apps/c64uviewer.png

# 4. Symlink fuer Terminal-Start
ln -s /usr/share/$APP_NAME/$APP_NAME $BUILD_ROOT/usr/local/bin/$APP_NAME

# 5. Control Datei erstellen (Depends: libsdl2-2.0-0)
echo "Package: $APP_NAME
Version: $VERSION
Architecture: amd64
Maintainer: Gruetze-Software
Depends: libsdl2-2.0-0
Description: Small Video/Audio-Stream-Viewer for Commodore 64 Ultimate in .NET 9.0
" > $BUILD_ROOT/DEBIAN/control

# 6. Debian Paket bauen
# Sicherstellen, dass der Zielordner existiert
mkdir -p dist/linux
echo "Erstelle .deb Paket..."
# Besitzer auf root setzen (simuliert System-Installation)
sudo chown -R root:root $BUILD_ROOT
dpkg-deb --build $BUILD_ROOT dist/linux/${APP_NAME}_${VERSION}_amd64.deb

# 7. Windows Build
echo "Baue Windows Version..."
dotnet publish $PROJ -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true --output ./dist/windows
# KOPIERE NATIVE DLL (WICHTIG!)
if [ -f "Libs/Win64/SDL2.dll" ]; then
    echo "Kopiere SDL2.dll in den Windows-Build-Ordner..."
    cp Libs/Win64/SDL2.dll ./dist/windows/
else
    echo "WARNUNG: Libs/Win64/SDL2.dll nicht gefunden! Sound wird unter Windows nicht funktionieren."
fi

# Rechte zurückgeben, damit rm -rf beim naechsten Mal wieder funktioniert
sudo chown -R $USER:$USER dist/

# 8. macOS Build (Apple Silicon / M1-M3)
echo "Baue macOS (ARM64) App-Struktur..."
APP_BUNDLE="dist/macos/C64UViewer.app"
MACOS_BIN="$APP_BUNDLE/Contents/MacOS"
RESOURCES="$APP_BUNDLE/Contents/Resources"

# Ordnerstruktur erstellen
mkdir -p "$MACOS_BIN"
mkdir -p "$RESOURCES"

# Kompilieren für Apple Silicon (M1/M2/M3)
dotnet publish $PROJ -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true -p:BundleReadyToRun=true --output ./dist/temp_macos

# Dateien in das App-Bundle verschieben
cp -r ./dist/temp_macos/* "$MACOS_BIN/"
cp Assets/icon.png "$RESOURCES/icon.png"

# 9. Die PList-Datei erstellen (Damit macOS weiß, was das für eine App ist)
echo '<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleExecutable</key>
    <string>c64uviewer</string>
    <key>CFBundleIconFile</key>
    <string>icon.png</string>
    <key>CFBundleIdentifier</key>
    <string>com.gruetze.c64uviewer</string>
    <key>CFBundleName</key>
    <string>C64U Viewer</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleShortVersionString</key>
    <string>'$VERSION'</string>
    <key>LSMinimumSystemVersion</key>
    <string>10.15</string>
</dict>
</plist>' > "$APP_BUNDLE/Contents/Info.plist"

# WICHTIG: Ausführungsrechte für das Binary setzen
chmod +x "$MACOS_BIN/c64uviewer"

echo "macOS App wurde unter $APP_BUNDLE erstellt."

# Erzeuge die ReadMe Datei zuerst in einem neutralen Ort
echo "Erzeuge ReadMe.txt..."
cat << EOF > dist/README_FIRST.txt
C64U Slim-Viewer v$VERSION
---------------------------
Requirements:
- Windows: SDL2.dll must be in the same folder (included).
- Linux: sudo apt install libsdl2-2.0-0
- macOS: brew install sdl2

Ultimate 64 Settings:
- Set Audio and Video Stream to 'Enabled'
- Set Audio and Video Target IP to your PC's IP address
- Set Video Port to: 11000
- Set Audio Port to: 11001
EOF

# 10. Finale Release-Pakete schnüren
echo "--- Erstelle finale Release-ZIPs ---"
RELEASE_DIR="dist/release"
mkdir -p "$RELEASE_DIR"

# A. Windows ZIP erstellen
echo "Kopiere ReadMe in Windows-Build..."
cp dist/README_FIRST.txt dist/windows/
echo "Zippe Windows Version..."
cd dist/windows && zip -r "../../$RELEASE_DIR/${APP_NAME}_${VERSION}_windows_x64.zip" . && cd ../..

# B. macOS ZIP erstellen
# Bei macOS gehört die ReadMe NICHT in das .app Bundle, sondern daneben ins ZIP
echo "Zippe macOS Version..."
cp dist/README_FIRST.txt dist/macos/
cd dist/macos && zip -r "../../$RELEASE_DIR/${APP_NAME}_${VERSION}_macos_arm64.zip" "${APP_NAME^}Viewer.app" README_FIRST.txt && cd ../..

# C. Debian Paket kopieren
cp dist/linux/*.deb "$RELEASE_DIR/"
# Die ReadMe auch noch mal lose in den Release-Ordner legen
cp dist/README_FIRST.txt "$RELEASE_DIR/"

echo "--------------------------------------------"
echo "FERTIG! Deine Pakete liegen in: $RELEASE_DIR"
ls -lh "$RELEASE_DIR"
