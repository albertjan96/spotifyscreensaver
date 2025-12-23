using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace SpotifyScreenSaver;

public sealed class BlackoutWindow : Window
{
    private readonly TextBlock _clockText;
    private readonly DispatcherTimer _clockTimer = new();

    public BlackoutWindow()
    {
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        Topmost = true;

        // 👇 EXPLICIET WPF TYPES (geen ambiguity meer)
        Background = System.Windows.Media.Brushes.Black;
        Cursor = System.Windows.Input.Cursors.None;

        _clockText = new TextBlock
        {
            Foreground = System.Windows.Media.Brushes.White,
            Opacity = 0.85,
            FontSize = 160, // BIG CLOCK
            FontWeight = FontWeights.SemiBold,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            VerticalAlignment = System.Windows.VerticalAlignment.Center
        };

        Content = new Grid
        {
            Children = { _clockText }
        };

        _clockTimer.Interval = TimeSpan.FromSeconds(1);
        _clockTimer.Tick += (_, __) => UpdateClock();

        Loaded += (_, __) =>
        {
            UpdateClock();
            _clockTimer.Start();
        };

        Closed += (_, __) => _clockTimer.Stop();
    }

    private void UpdateClock()
    {
        _clockText.Text = DateTime.Now.ToString("HH:mm");
    }
}
