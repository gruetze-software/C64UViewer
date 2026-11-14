#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")/.."
app_id=de.gruetze_software.C64UViewer
payload="$PWD/artifacts/linux-payload"
publish="$(mktemp -d)"
trap 'rm -rf "$publish"' EXIT

dotnet publish C64UViewer.csproj -c Release -r linux-x64 --self-contained true -o "$publish"
rm -rf -- "$payload"
install -d "$payload/bin" "$payload/lib/c64uviewer"
cp -a "$publish/." "$payload/lib/c64uviewer/"
install -m 755 packaging/c64uviewer-launch "$payload/bin/c64uviewer"
install -Dm644 "packaging/$app_id.desktop" "$payload/share/applications/$app_id.desktop"
install -Dm644 "packaging/$app_id.metainfo.xml" "$payload/share/metainfo/$app_id.metainfo.xml"
install -Dm644 Assets/icon.png "$payload/share/icons/hicolor/256x256/apps/$app_id.png"
dotnet msbuild C64UViewer.csproj -getProperty:Version > "$payload/VERSION"
