using System;
using System.IO;
using System.Text.Json;

namespace C64UViewer.Models;

public class AppSettings
{
    public string LastIpAddress { get; set; } = "192.168.178.164";

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
        if (!File.Exists(FilePath)) return new AppSettings();
        try { return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new AppSettings(); }
        catch { return new AppSettings(); }
    }

    public void Save()
    {
        File.WriteAllText(FilePath, JsonSerializer.Serialize(this));
    }
}