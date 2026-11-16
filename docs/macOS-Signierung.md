# macOS signieren und notarieren

Für C64UViewer kannst du das **Developer ID Application**-Zertifikat und den **App Store Connect Team-API-Schlüssel** aus deinem BingWallE-Setup wiederverwenden. Die fünf Secret-Namen und ihre Formate sind identisch. Zertifikat und Schlüssel müssen zum selben Apple-Team gehören.

## 1. Bestehende Werte in C64UViewer hinterlegen

Öffne [Settings → Secrets and variables → Actions](https://github.com/gruetze-software/C64UViewer/settings/secrets/actions). Lege dort die Secrets mit denselben Werten wie in BingWallE an. Falls du organisationsweite Secrets verwendest, gib C64UViewer Zugriff darauf.

| Secret | Wert aus deinem BingWallE-Setup |
| --- | --- |
| `MACOS_CERTIFICATE_P12_BASE64` | Base64-Inhalt der passwortgeschützten P12 mit Zertifikat und privatem Schlüssel |
| `MACOS_CERTIFICATE_PASSWORD` | Passwort des P12-Exports |
| `APPLE_API_KEY_P8` | Vollständiger Text der P8-Datei mit BEGIN- und END-Zeile sowie Zeilenumbrüchen |
| `APPLE_API_KEY_ID` | Key ID des App Store Connect Team-API-Schlüssels |
| `APPLE_API_ISSUER_ID` | Issuer ID aus App Store Connect |

GitHub zeigt gespeicherte Secret-Werte nachträglich nicht an. Verwende deine sicher abgelegten Originale. Falls du die P12 erneut aus dem Schlüsselbund exportierst, übernimm auch das dazugehörige Exportpasswort. Auf dem Mac kopierst du die Base64-Kodierung so in die Zwischenablage:

```sh
base64 -i developer-id.p12 | tr -d '\n' | pbcopy
```

## 2. DMGs bauen oder zu einem Release hinzufügen

Der Release-Workflow baut und veröffentlicht für neue Versions-Tags zwei signierte und notarisierte DMGs. Er erstellt den GitHub-Release, sobald beide macOS-Pakete und die übrigen Release-Dateien vorliegen.

1. Öffne **Actions → Sign and notarize macOS → Run workflow**.
2. Wähle `main` und gib bei `release_tag` einen vorhandenen Tag ein, zum Beispiel `v1.3.3`.
3. Mit **Add the DMGs to the existing GitHub release** lädt der Workflow beide DMGs in das bestehende Release und entfernt das bisherige macOS-Release-ZIP nach erfolgreichem Upload. Deaktiviere die Option für einen reinen Test-Build.
4. Die Artefakte `c64uviewer-macos-arm64-signed` und `c64uviewer-macos-x64-signed` enthalten jeweils eine DMG inklusive SDL2.

Der API-Schlüssel dient der Anmeldung bei Apples Notarisierungsdienst.

## 3. App auf dem Mac prüfen

Öffne die DMG und ziehe die App in **Programme**. Ersetze im Prüfkommando den Paketnamen durch den tatsächlichen DMG-Dateinamen:

```sh
xcrun stapler validate c64uviewer_1.3.3_macos_arm64.dmg
codesign --verify --deep --strict --verbose=2 /Applications/C64UViewer.app
spctl --assess --type execute --verbose=4 /Applications/C64UViewer.app
```

[Apple Developer ID](https://developer.apple.com/developer-id/) · [Apple-Notarisierung](https://developer.apple.com/documentation/security/customizing-the-notarization-workflow)
