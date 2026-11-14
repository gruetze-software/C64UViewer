# C64UViewer-Icon in Paint.NET

Öffne `c64viewer_icon.png` als Vorlage. Für eine editierbare Fassung erstelle eine quadratische Leinwand mit 1024 × 1024 Pixeln. Lösche die weiße Hintergrundfläche mit **Strg+A**, **Entf** und **Strg+D**.

| Ebene | Werkzeug | Farbe | Position und Größe in Pixeln |
|---|---|---|---|
| Gehäuse | Gefülltes abgerundetes Rechteck | `#DFC8AA` | 72, 94 · 856 × 832 |
| Bildschirm | Gefülltes abgerundetes Rechteck | `#1E0D50` | 140, 174 · 720 × 548 |
| Play | Gefüllte Rechtecke | `#C88AF7` | Siehe unten |
| Schlitz | Gefülltes Rechteck | `#353535` | 150, 786 · 622 × 43 |
| LED | Gefülltes Rechteck | `#40D54A` | 810, 786 · 43 × 43 |

Erstelle für jede Zeile eine neue Ebene. Für das Play-Symbol zeichnest du fünf aneinandergrenzende Rechtecke. Ihre Oberkanten liegen bei 278, 315, 350, 385 und 420 Pixeln. Starte bei x = 371 und rücke für jedes Rechteck 52 Pixel nach rechts. Jedes Rechteck ist 52 Pixel breit. Die Höhen betragen 332, 255, 185, 115 und 46 Pixel.

Speichere die Ebenen als `.pdn`. Exportiere eine Kopie mit transparentem Hintergrund als 32-Bit-PNG. Für `Assets/icon.png` skaliere die Kopie auf 256 × 256 Pixel. Prüfe das Motiv auch bei 16 × 16 Pixeln. Für das Windows-Icon `Assets/icon.ico` exportiere mehrere Größen mit einem ICO-Plug-in oder einem ICO-Konverter.
