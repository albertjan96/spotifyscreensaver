using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;


using WinForms = System.Windows.Forms;
using WpfPoint = System.Windows.Point;

namespace SpotifyScreenSaver;

public partial class ScreenSaverWindow : Window
{
    private SpotifyAuth? _auth;

    private readonly SpotifyApi _api = new();

    private readonly DispatcherTimer _clockTimer = new();

    private TokenStore.TokenData? _tokens;
    private readonly DispatcherTimer _timer = new();
    private readonly DispatcherTimer _progressTimer = new();

    private readonly List<BlackoutWindow> _blackouts = new();
    private WpfPoint? _firstMousePos;

    private int _currentProgressMs;
    private int _currentDurationMs;
    private DateTime _lastUpdateTime;
    private string? _currentTrackName;
    private bool _isPlaying;
    private TimeSpan _currentTimerInterval = TimeSpan.FromSeconds(3);
    
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _isTickRunning;
    private bool _isClosing;

    public ScreenSaverWindow()
    {
        InitializeComponent();

        KeyDown += OnKeyDown;
        MouseDown += OnMouseDown;
        MouseMove += OnMouseMoveExitLogic;

        Loaded += async (_, __) =>
        {
            _cancellationTokenSource = new CancellationTokenSource();

            PlaceOnPrimaryScreen();
            CreateBlackoutsOnSecondaryScreens();

            var clientId = AppConfig.LoadClientId();
            if (string.IsNullOrWhiteSpace(clientId))
            {
                SongText.Text = "Not configured";
                ArtistText.Text = "Run screensaver config (/c) to set Client ID.";
                AlbumArt.Source = null;
                return;
            }
            
            _auth = new SpotifyAuth(clientId, AppConfig.RedirectUri, AppConfig.ListenerPrefix);
            _tokens = _auth.LoadTokens();
            
            if (_tokens is null)
            {
                SongText.Text = "Not logged in";
                ArtistText.Text = "Run screensaver config (/c) to login.";
                AlbumArt.Source = null;
                return;
            }

            _timer.Interval = TimeSpan.FromSeconds(3);
            _timer.Tick += OnTimerTick;
            _timer.Start();

            _progressTimer.Interval = TimeSpan.FromSeconds(1);
            _progressTimer.Tick += UpdateProgressDisplay;
            _progressTimer.Start();

            await TickAsync();
        };

        Closing += (_, __) =>
        {
            if (_isClosing) return;
            _isClosing = true;
            
            _cancellationTokenSource?.Cancel();
            
            _timer.Stop();
            _progressTimer.Stop();
            _clockTimer.Stop();
            
            foreach (var w in _blackouts)
            {
                try 
                { 
                    w.Hide();
                    w.Close(); 
                } 
                catch { }
            }
            _blackouts.Clear();
        };

        Closed += (_, __) =>
        {
            _cancellationTokenSource?.Dispose();
        };
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (!_isClosing)
            Close();
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (!_isClosing)
            Close();
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        if (_isTickRunning || _cancellationTokenSource?.Token.IsCancellationRequested == true)
            return;

        _ = TickAsync();
    }

    private void PlaceOnPrimaryScreen()
    {
        var ps = WinForms.Screen.PrimaryScreen;
        if (ps is null)
        {
            // Fallback: just maximize on current monitor
            WindowState = WindowState.Maximized;
            return;
        }

        var b = ps.Bounds;

        WindowState = WindowState.Normal;
        Left = b.Left;
        Top = b.Top;
        Width = b.Width;
        Height = b.Height;
    }

    private void CreateBlackoutsOnSecondaryScreens()
    {
        foreach (var s in WinForms.Screen.AllScreens)
        {
            if (s.Primary) continue;

            var b = s.Bounds;
            var w = new BlackoutWindow
            {
                WindowState = WindowState.Normal,
                Left = b.Left,
                Top = b.Top,
                Width = b.Width,
                Height = b.Height
            };

            w.Show();
            _blackouts.Add(w);
        }
    }

    private void OnMouseMoveExitLogic(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (_isClosing) return;
        
        var pos = e.GetPosition(this);
        if (_firstMousePos is null)
        {
            _firstMousePos = pos;
            return;
        }

        var dx = Math.Abs(pos.X - _firstMousePos.Value.X);
        var dy = Math.Abs(pos.Y - _firstMousePos.Value.Y);

        if (dx > 5 || dy > 5)
            Close();
    }

    private string FormatTime(int milliseconds)
    {
        var ts = TimeSpan.FromMilliseconds(milliseconds);
        return $"{(int)ts.TotalMinutes}:{ts.Seconds:D2}";
    }

    private void SetTimerInterval(TimeSpan newInterval)
    {
        if (_currentTimerInterval != newInterval)
        {
            _currentTimerInterval = newInterval;
            _timer.Stop();
            _timer.Interval = newInterval;
            _timer.Start();
        }
    }

    private void UpdateProgressDisplay(object? sender, EventArgs e)
    {
        if (_currentDurationMs == 0) return;

        if (!_isPlaying)
        {
            return;
        }

        var elapsedSinceUpdate = (DateTime.Now - _lastUpdateTime).TotalMilliseconds;
        var estimatedProgress = _currentProgressMs + (int)elapsedSinceUpdate;

        if (estimatedProgress > _currentDurationMs)
            estimatedProgress = _currentDurationMs;

        var elapsed = FormatTime(estimatedProgress);
        var remaining = FormatTime(_currentDurationMs - estimatedProgress);
        var total = FormatTime(_currentDurationMs);
        TimeText.Text = $"{elapsed} / {total} ({remaining} left)";

        if (estimatedProgress >= _currentDurationMs - 2000)
        {
            SetTimerInterval(TimeSpan.FromSeconds(2));
        }
    }

    private async Task TickAsync()
    {
        if (_tokens is null || _isTickRunning || _auth is null || _isClosing) return;

        _isTickRunning = true;
        
        try
        {
            var cts = _cancellationTokenSource;
            if (cts == null || cts.Token.IsCancellationRequested)
                return;

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(10));

            var nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (nowUnix >= _tokens.ExpiresAtUnix)
            {
                _tokens = await _auth.RefreshAsync(_tokens.RefreshToken, timeoutCts.Token);
            }

            var (status, data, _) = await _api.GetNowPlayingAsync(_tokens.AccessToken, timeoutCts.Token);

            if (status == HttpStatusCode.Unauthorized)
            {
                _tokens = await _auth.RefreshAsync(_tokens.RefreshToken, timeoutCts.Token);
                (status, data, _) = await _api.GetNowPlayingAsync(_tokens.AccessToken, timeoutCts.Token);
            }

            if (timeoutCts.Token.IsCancellationRequested)
                return;

            if (data is null)
            {
                SongText.Text = "Nothing playing";
                ArtistText.Text = "Start Spotify on any device.";
                AlbumArt.Source = null;
                TimeText.Text = "";
                NextTrackPanel.Visibility = Visibility.Collapsed;
                _currentProgressMs = 0;
                _currentDurationMs = 0;
                _currentTrackName = null;
                _isPlaying = false;
                SetTimerInterval(TimeSpan.FromSeconds(5));
                return;
            }

            bool trackChanged = _currentTrackName != data.TrackName;

            if (trackChanged)
            {
                SetTimerInterval(TimeSpan.FromSeconds(3));
            }
            else
            {
                SetTimerInterval(TimeSpan.FromSeconds(10));
            }

            SongText.Text = data.TrackName;
            ArtistText.Text = data.ArtistName;

            _currentProgressMs = data.ProgressMs;
            _currentDurationMs = data.DurationMs;
            _currentTrackName = data.TrackName;
            _isPlaying = data.IsPlaying;
            _lastUpdateTime = DateTime.Now;

            var elapsed = FormatTime(data.ProgressMs);
            var remaining = FormatTime(data.DurationMs - data.ProgressMs);
            var total = FormatTime(data.DurationMs);
            TimeText.Text = $"{elapsed} / {total} ({remaining} left)";

            if (!string.IsNullOrWhiteSpace(data.AlbumArtUrl))
            {
                await LoadAlbumArtAsync(data.AlbumArtUrl, timeoutCts.Token);
            }
            else
            {
                AlbumArt.Source = null;
            }

            if (timeoutCts.Token.IsCancellationRequested)
                return;

            var (queueStatus, queueData, _) = await _api.GetQueueAsync(_tokens.AccessToken, timeoutCts.Token);
            
            if (queueStatus == HttpStatusCode.OK && queueData != null && 
                !string.IsNullOrWhiteSpace(queueData.NextTrackName))
            {
                NextSongText.Text = queueData.NextTrackName;
                NextArtistText.Text = queueData.NextArtistName ?? "";
                NextTrackPanel.Visibility = Visibility.Visible;
            }
            else
            {
                NextTrackPanel.Visibility = Visibility.Collapsed;
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch
        {
            SongText.Text = "Spotify error";
            ArtistText.Text = "Check login (/c) or internet.";
            AlbumArt.Source = null;
            TimeText.Text = "";
            NextTrackPanel.Visibility = Visibility.Collapsed;
            _currentProgressMs = 0;
            _currentDurationMs = 0;
            _currentTrackName = null;
            _isPlaying = false;
        }
        finally
        {
            _isTickRunning = false;
        }
    }

    private async Task LoadAlbumArtAsync(string url, CancellationToken ct)
    {
        try
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = new Uri(url);
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.EndInit();
            
            await bmp.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Loaded);
            
            if (!ct.IsCancellationRequested)
            {
                AlbumArt.Source = bmp;
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch
        {
            AlbumArt.Source = null;
        }
    }
}
