# C64U Slim-Viewer im Microsoft Store

1. Im [Partner Center](https://partner.microsoft.com/dashboard/) eine Windows-App für den **C64U Slim-Viewer** anlegen und den Namen reservieren.
2. In der Produktidentität die drei Werte übernehmen. In GitHub unter **Settings → Secrets and variables → Actions → Variables** eintragen.

| GitHub-Variable | Wert aus Partner Center |
| --- | --- |
| `WINDOWS_STORE_PACKAGE_NAME` | `Package/Identity/Name` |
| `WINDOWS_STORE_PUBLISHER` | `Package/Identity/Publisher`, einschließlich `CN=` |
| `WINDOWS_STORE_PUBLISHER_DISPLAY_NAME` | `Package/Properties/PublisherDisplayName` |

3. Unter **Actions → Windows Store → Run workflow** den Build starten. Das Artefakt herunterladen und die darin enthaltene **.msix** im Partner Center hochladen. Microsoft signiert das Paket im Veröffentlichungsprozess.
4. Bei der Einreichung die Verwendung von `runFullTrust` begründen. Die Avalonia-Desktop-App empfängt auf dem lokalen Netzwerk Video und optional Audio per UDP und gibt Ton über SDL2 aus. Für die Zertifizierung im Prüferhinweis den Hardwarebedarf und die Schritte unter **Setup** beschreiben.

Das Paket enthält .NET 10 und SDL2 für Windows x64. Updates laufen über den Microsoft Store. Die Action baut das Paket für die manuelle Einreichung.

## Lokal bauen

Voraussetzungen sind das .NET 10 SDK und das Windows SDK mit MakeAppx. Im Repository ausführen und die Beispielwerte durch die Produktidentität ersetzen.

```powershell
.\packaging\Build-WindowsStore.ps1 -PackageName 'NAME_AUS_PARTNER_CENTER' -Publisher 'CN=ID_AUS_PARTNER_CENTER' -PublisherDisplayName 'ANZEIGENAME'
```

Die MSIX liegt unter `artifacts/windows-store/`.