#!/bin/bash
set -euo pipefail
cd "$(dirname "$0")"

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
install -m 755 "./dist/temp_publish/$APP_NAME" "$BUILD_ROOT/usr/share/$APP_NAME/"
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
Description: Small video and audio stream viewer for Commodore 64 Ultimate in .NET 10
" > $BUILD_ROOT/DEBIAN/control

# 6. Debian Paket bauen
# Sicherstellen, dass der Zielordner existiert
mkdir -p dist/linux
echo "Erstelle .deb Paket..."
# Besitzer auf root setzen (simuliert System-Installation)
dpkg-deb --root-owner-group --build "$BUILD_ROOT" "dist/linux/${APP_NAME}_${VERSION}_amd64.deb"

# 7. Windows Build
echo "Baue Windows Version..."
dotnet publish $PROJ -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true --output ./dist/temp_windows
mkdir -p dist/windows
cp "./dist/temp_windows/$APP_NAME.exe" ./dist/temp_windows/SDL2.dll dist/windows/

# Rechte zurückgeben, damit rm -rf beim naechsten Mal wieder funktioniert

# Erzeuge die ReadMe Datei zuerst in einem neutralen Ort
echo "Erzeuge ReadMe.txt..."
cat << EOF > dist/README_FIRST.txt
C64U Slim-Viewer v$VERSION
---------------------------
Requirements:
- Windows: SDL2.dll must be in the same folder (included).
- Linux: sudo apt install libsdl2-2.0-0

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

# B. Debian Paket kopieren
cp dist/linux/*.deb "$RELEASE_DIR/"
# Die ReadMe auch noch mal lose in den Release-Ordner legen
cp dist/README_FIRST.txt "$RELEASE_DIR/"

echo "--------------------------------------------"
echo "FERTIG! Deine Pakete liegen in: $RELEASE_DIR"
ls -lh "$RELEASE_DIR"
