namespace C64UViewer.Models;

public static class C64Colors
{
    /// <summary>
    /// Authentische C64 Palette (Pepto Standard) im BGRA8888 Format.
    /// Die Reihenfolge entspricht exakt den VIC-II Farbindizes 0-15.
    /// Format: 0xAARRGGBB (Alpha, Red, Green, Blue)
    /// </summary>
    public static readonly uint[] Palette =
    [
        0xFF000000, // 0: Black
        0xFFFFFFFF, // 1: White
        0xFF883932, // 2: Red
        0xFF67B6BD, // 3: Cyan
        0xFF8B3E96, // 4: Purple
        0xFF59AB59, // 5: Green
        0xFF483AAA, // 6: Blue
        0xFFB8C76F, // 7: Yellow
        0xFF936B32, // 8: Orange
        0xFF574700, // 9: Brown
        0xFFB8766E, // 10: Light Red
        0xFF525252, // 11: Dark Grey
        0xFF838383, // 12: Grey
        0xFF89E97D, // 13: Light Green
        0xFF7B6FBE, // 14: Light Blue
        0xFFB0B0B0  // 15: Light Grey
    ];

    // Hilfsmethode, falls mal eine Farbe per Name abzugreifen ist
    public static uint GetColorByName(string name) => name.ToLower() switch
    {
        "black" => Palette[0],
        "white" => Palette[1],
        "red" => Palette[2],
        "cyan" => Palette[3],
        "purple" => Palette[4],
        "green" => Palette[5],
        "blue" => Palette[6],
        "yellow" => Palette[7],
        "orange" => Palette[8],
        "brown" => Palette[9],
        "lightred" => Palette[10],
        "darkgrey" => Palette[11],
        "grey" => Palette[12],
        "lightgreen" => Palette[13],
        "lightblue" => Palette[14],
        "lightgrey" => Palette[15],
        _ => Palette[0]
    };
}


