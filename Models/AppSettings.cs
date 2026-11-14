using System;
using System.IO;
using System.Text.Json;

namespace C64UViewer.Models;

public class AppSettings
{
    public int UdpPort { get; set; } = 11000;
    public int UdpAudioPort { get; set; } = 11001;
    public bool IsAudioEnabled { get; set; } = false;
    public bool ScanlinesEnabled { get; set; }
    public int ScanlineStrength { get; set; } = 30;
    public bool GlowEnabled { get; set; }
    public int GlowStrength { get; set; } = 25;
    
    public static string FilePath
    {
        get 
        {
            // Erstellt den Pfad: /home/user/.config/c64uviewer/ (Linux) 
            // oder AppData/Roaming/c64uviewer/ (Windows)
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string configDir = Path.Combine(appData, "c64uviewer");
                    
            // Ganz wichtig: Sicherstellen, dass der Ordner existiert!
            if (!Directory.Exists(configDir))
            {
                Directory.CreateDirectory(configDir);
            }
                    
            return Path.Combine(configDir, "settings.json");
        }
    }

    public static AppSettings Load()
    {

        try
        {
            var path = FilePath;
            return File.Exists(path)
                ? JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path)) ?? new AppSettings()
                : new AppSettings();
        }
        catch { return new AppSettings(); }
    }

    public void Save()
    {
        File.WriteAllText(FilePath, JsonSerializer.Serialize(this));
    }
}
