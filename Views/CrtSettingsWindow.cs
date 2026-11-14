using Avalonia;
using Avalonia.Controls;
using C64UViewer.ViewModels;

namespace C64UViewer.Views;

public sealed class CrtSettingsWindow : Window
{
    public CrtSettingsWindow(MainWindowViewModel viewModel)
    {
        Title = "CRT Settings";
        Width = 340;
        SizeToContent = SizeToContent.Height;
        CanResize = false;
        ShowInTaskbar = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        var enabled = new CheckBox { Content = "Scanlines", IsChecked = viewModel.ScanlinesEnabled };
        var label = new TextBlock();
        var glowEnabled = new CheckBox { Content = "Screen glow", IsChecked = viewModel.GlowEnabled };
        var glowLabel = new TextBlock();
        var glow = new Slider
        {
            Minimum = 0, Maximum = 100, Value = viewModel.GlowStrength,
            TickFrequency = 1, IsSnapToTickEnabled = true
        };
        var strength = new Slider
        {
            Minimum = 0, Maximum = 70, Value = viewModel.ScanlineStrength,
            TickFrequency = 1, IsSnapToTickEnabled = true
        };
        void UpdateLabel()
        {
            strength.IsEnabled = enabled.IsChecked == true;
            label.Text = $"Scanline strength ({strength.Value:0}%)";
            glow.IsEnabled = glowEnabled.IsChecked == true;
            glowLabel.Text = $"Glow strength ({glow.Value:0}%)";
        }
        enabled.IsCheckedChanged += (_, _) => UpdateLabel();
        strength.ValueChanged += (_, _) => UpdateLabel();
        glowEnabled.IsCheckedChanged += (_, _) => UpdateLabel();
        glow.ValueChanged += (_, _) => UpdateLabel();
        UpdateLabel();

        var apply = new Button { Content = "Apply", IsDefault = true };
        var cancel = new Button { Content = "Cancel", IsCancel = true };
        apply.Click += (_, _) =>
        {
            viewModel.SetCrtSettings(enabled.IsChecked == true, (int)strength.Value,
                glowEnabled.IsChecked == true, (int)glow.Value);
            Close();
        };
        cancel.Click += (_, _) => Close();
        Content = new StackPanel
        {
            Margin = new Thickness(24), Spacing = 12,
            Children = { enabled, label, strength, new Separator(), glowEnabled, glowLabel, glow, new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 8,
                Children = { apply, cancel }
            } }
        };
    }
}
