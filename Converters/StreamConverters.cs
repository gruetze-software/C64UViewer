using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace C64UViewer.Converters;

public class StreamButtonTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return (value is bool isStreaming && isStreaming) ? "Stop Stream" : "Start Stream";
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class StreamButtonColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return (value is bool isStreaming && isStreaming) ? Brushes.IndianRed : Brushes.ForestGreen;
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}