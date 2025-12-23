using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace SpotifyScreenSaver;

public partial class ConfigWindow : Window
{
    private readonly SpotifyAuth _auth = new(AppConfig.SpotifyClientId, AppConfig.RedirectUri, AppConfig.ListenerPrefix);
    private readonly SpotifyApi _api = new();

    public ConfigWindow()
    {
        InitializeComponent();
        Append("Ready.");
        if (AppConfig.SpotifyClientId.Contains("PASTE_"))
            Append("⚠️ First set your Client ID in AppConfig.cs");
    }

    private async void Login_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            LoginBtn.IsEnabled = false;
            Append("Opening browser for Spotify login...");
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
            var tokens = await _auth.LoginWithPkceAsync(cts.Token);
            Append("✅ Login OK. Tokens stored.");
            Append($"Access expires at unix: {tokens.ExpiresAtUnix}");
        }
        catch (Exception ex)
        {
            Append("❌ Login failed:");
            Append(ex.Message);
        }
        finally
        {
            LoginBtn.IsEnabled = true;
        }
    }

    private async void Test_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            TestBtn.IsEnabled = false;
            var tok = _auth.LoadTokens();
            if (tok is null)
            {
                Append("No tokens found. Login first.");
                return;
            }

            var nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (nowUnix >= tok.ExpiresAtUnix)
            {
                Append("Refreshing access token...");
                tok = await _auth.RefreshAsync(tok.RefreshToken, CancellationToken.None);
                Append("Refresh OK.");
            }

            var (status, data, raw) = await _api.GetNowPlayingAsync(tok.AccessToken, CancellationToken.None);
            Append($"HTTP {((int)status)} {status}");

            if (data is null)
            {
                Append("No track (likely 204) or error.");
                if (!string.IsNullOrWhiteSpace(raw)) Append(raw);
                return;
            }

            Append($"Now playing: {data.TrackName} — {data.ArtistName}");
            Append($"Album: {data.AlbumName}");
            Append($"Link: {data.SpotifyUrl}");
        }
        catch (Exception ex)
        {
            Append("❌ Test failed:");
            Append(ex.Message);
        }
        finally
        {
            TestBtn.IsEnabled = true;
        }
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        _auth.ClearTokens();
        Append("🧹 Cleared stored tokens.");
    }

    private void Append(string msg)
    {
        StatusBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}\r\n");
        StatusBox.ScrollToEnd();
        Debug.WriteLine(msg);
    }
}