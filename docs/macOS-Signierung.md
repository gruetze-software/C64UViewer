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

## 2. Signierten Test-Build starten

1. Öffne **Actions → Sign and notarize macOS → Run workflow**.
2. Wähle `main` und gib bei `release_tag` einen vorhandenen Tag ein, zum Beispiel `v1.3.1`.
3. Lade nach erfolgreichem Lauf das Artefakt `c64uviewer-macos-arm64-signed` herunter. Darin liegt das signierte und notarisierte ZIP für Apple Silicon inklusive SDL2.

Der Workflow legt das ZIP ausschließlich als GitHub-Artefakt ab. Der API-Schlüssel dient der Anmeldung bei Apples Notarisierungsdienst.

## 3. App auf dem Mac prüfen

Entpacke das innere ZIP und führe im Ordner mit der App aus:

```sh
codesign --verify --deep --strict --verbose=2 C64UViewer.app
xcrun stapler validate C64UViewer.app
spctl --assess --type execute --verbose=4 C64UViewer.app
```

[Apple Developer ID](https://developer.apple.com/developer-id/) · [Apple-Notarisierung](https://developer.apple.com/documentation/security/customizing-the-notarization-workflow)
