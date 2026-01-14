using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace C64UViewer.Converters;

public class StatusColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        string? msg = value as string;
        if (msg != null && msg.StartsWith("Fail", StringComparison.OrdinalIgnoreCase)) return Brushes.Red;
        if (msg != null && msg.Contains("active", StringComparison.OrdinalIgnoreCase)) return Brushes.LightGreen;
        return Brushes.White;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}