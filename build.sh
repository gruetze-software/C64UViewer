#!/bin/bash

# 1. Konfiguration
PROJ="C64UViewer.csproj"
APP_NAME="c64uviewer"
VERSION="1.0.0"
BUILD_ROOT="dist/linux_pkg"

echo "--- Starte Build-Prozess ---"
echo "Raeume alte Dateien auf..."
rm -rf obj bin dist
mkdir -p $BUILD_ROOT/usr/share/applications
mkdir -p $BUILD_ROOT/usr/share/icons/hicolor/256x256/apps
mkdir -p $BUILD_ROOT/usr/share/$APP_NAME
mkdir -p $BUILD_ROOT/usr/local/bin
mkdir -p $BUILD_ROOT/DEBIAN

# 2. App bauen
echo "Kompiliere Linux Version..."
dotnet publish $PROJ -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true --output ./dist/temp_publish

# 3. Dateien kopieren
cp -r ./dist/temp_publish/* $BUILD_ROOT/usr/share/$APP_NAME/
cp c64uviewer.desktop $BUILD_ROOT/usr/share/applications/
cp Assets/icon.png $BUILD_ROOT/usr/share/icons/hicolor/256x256/apps/c64uviewer.png

# 4. Symlink fuer Terminal-Start
ln -s /usr/share/$APP_NAME/$APP_NAME $BUILD_ROOT/usr/local/bin/$APP_NAME

# 5. Control Datei erstellen (Wichtig: keine Umlaute hier!)
echo "Package: $APP_NAME
Version: $VERSION
Architecture: amd64
Maintainer: Grütze-Software <info@gruetze-software.de>
Description: Video-Stream-Viewer for Commodore 64 Ultimate in .NET 8.0
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

# Damit rm -rf beim n�chsten Mal wieder funktioniert
sudo chown -R $USER:$USER dist/

echo "--- Build erfolgreich beendet! ---"