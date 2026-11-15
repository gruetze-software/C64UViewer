#!/usr/bin/env bash
set -euo pipefail
umask 077

tag="${1:?Release tag required}"
output="${2:?Output directory required}"
app="$output/C64UViewer.app"

for secret in MACOS_CERTIFICATE_P12_BASE64 MACOS_CERTIFICATE_PASSWORD APPLE_API_KEY_P8 APPLE_API_KEY_ID APPLE_API_ISSUER_ID; do
    if [[ -z "${!secret}" ]]; then
        echo "::error::Missing GitHub Actions secret $secret"
        exit 1
    fi
done

work=$(mktemp -d)
keychain="$work/build.keychain-db"
cleanup() {
    security delete-keychain "$keychain" >/dev/null 2>&1 || true
    rm -rf -- "$work"
}
trap cleanup EXIT

password=$(openssl rand -hex 32)
printf '%s' "$MACOS_CERTIFICATE_P12_BASE64" | base64 -D > "$work/certificate.p12"
notary_key="$work/AuthKey.p8"
printf '%s\n' "$APPLE_API_KEY_P8" > "$notary_key"
notary_auth=(--key "$notary_key" --key-id "$APPLE_API_KEY_ID" --issuer "$APPLE_API_ISSUER_ID")
security create-keychain -p "$password" "$keychain"
security unlock-keychain -p "$password" "$keychain"
security set-keychain-settings -lut 21600 "$keychain"
security import "$work/certificate.p12" -k "$keychain" -P "$MACOS_CERTIFICATE_PASSWORD" -T /usr/bin/codesign
security set-key-partition-list -S apple-tool:,apple:,codesign: -s -k "$password" "$keychain" >/dev/null

identity=$(security find-identity -v -p codesigning "$keychain" |
    awk '/"Developer ID Application:/ { print $2 }')
if [[ ! "$identity" =~ ^[[:xdigit:]]{40}$ ]]; then
    echo "::error::Expected exactly one valid Developer ID Application identity in the P12"
    exit 1
fi

# Sign native code individually before sealing the app bundle.
while IFS= read -r -d '' file; do
    if [[ "$file" != "$app/Contents/MacOS/c64uviewer" && "$(file -b "$file")" == *Mach-O* ]]; then
        codesign --force --keychain "$keychain" --sign "$identity" --timestamp --options runtime "$file"
    fi
done < <(find "$app/Contents" -type f -print0)

codesign --force --keychain "$keychain" --sign "$identity" --timestamp --options runtime \
    --entitlements "$(dirname "$0")/macos-entitlements.plist" "$app"
codesign --verify --deep --strict --verbose=2 "$app"
ditto -c -k --keepParent "$app" "$work/notarization.zip"

xcrun notarytool submit "$work/notarization.zip" "${notary_auth[@]}" \
    --wait --output-format json > "$work/result.json" || true
status=$(plutil -extract status raw -o - "$work/result.json" 2>/dev/null || true)
if [[ "$status" != Accepted ]]; then
    echo "::error::Apple notarization did not return Accepted"
    if id=$(plutil -extract id raw -o - "$work/result.json" 2>/dev/null); then
        xcrun notarytool log "$id" "${notary_auth[@]}"
    fi
    exit 1
fi
xcrun stapler staple "$app"
xcrun stapler validate "$app"
ditto -c -k --keepParent "$app" "$output/c64uviewer_${tag#v}_macos_arm64_signed.zip"
