#!/usr/bin/env bash
set -euo pipefail

tag="${1:?Release tag required}"
output="${2:?Output directory required}"
version=$(dotnet msbuild C64UViewer.csproj -nologo -getProperty:Version)
[[ "$tag" == "v$version" ]] || { echo "Tag $tag does not match project version $version" >&2; exit 1; }

app="$output/C64UViewer.app"
mkdir -p "$app/Contents/MacOS" "$app/Contents/Resources"
dotnet publish C64UViewer.csproj -c Release -r osx-arm64 --self-contained true \
    -p:PublishSingleFile=false -o "$app/Contents/MacOS"
rm -f "$app/Contents/MacOS/createdump"

# Bundle SDL2 so library validation can stay enabled on the signed app.
sdl_dir=$(mktemp -d)
trap 'rm -rf -- "$sdl_dir"' EXIT
sdl_version=2.32.10
curl --fail --silent --show-error --location \
    "https://www.libsdl.org/release/SDL2-$sdl_version.tar.gz" -o "$sdl_dir/sdl.tar.gz"
echo "5f5993c530f084535c65a6879e9b26ad441169b3e25d789d83287040a9ca5165  $sdl_dir/sdl.tar.gz" | shasum -a 256 -c
tar -xzf "$sdl_dir/sdl.tar.gz" -C "$sdl_dir"
cmake -S "$sdl_dir/SDL2-$sdl_version" -B "$sdl_dir/build" \
    -DCMAKE_BUILD_TYPE=Release -DCMAKE_OSX_ARCHITECTURES=arm64 \
    -DSDL_SHARED=ON -DSDL_STATIC=OFF -DSDL_TEST=OFF
cmake --build "$sdl_dir/build" --parallel
# SDL2 builds libSDL2-2.0.dylib; .NET resolves SDL2 as libSDL2.dylib.
cp -L "$sdl_dir/build/libSDL2-2.0.dylib" "$app/Contents/MacOS/libSDL2.dylib"
cp "$sdl_dir/SDL2-$sdl_version/LICENSE.txt" "$app/Contents/Resources/SDL2-LICENSE.txt"

cp Assets/icon.png "$app/Contents/Resources/icon.png"
cat > "$app/Contents/Info.plist" <<EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleExecutable</key><string>c64uviewer</string>
    <key>CFBundleIconFile</key><string>icon.png</string>
    <key>CFBundleIdentifier</key><string>com.gruetze.c64uviewer</string>
    <key>CFBundleName</key><string>C64U Slim-Viewer</string>
    <key>CFBundlePackageType</key><string>APPL</string>
    <key>CFBundleShortVersionString</key><string>$version</string>
    <key>CFBundleVersion</key><string>$version</string>
</dict>
</plist>
EOF
plutil -lint "$app/Contents/Info.plist"
