using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace C64UViewer.Views;

public sealed class ScanlineOverlay : Control
{
    public override void Render(DrawingContext context)
    {
        for (int row = 0; row < Bounds.Height; row++)
            context.FillRectangle(Brushes.Black, new Rect(0, row + 0.5, Bounds.Width, 0.5));
    }
}
