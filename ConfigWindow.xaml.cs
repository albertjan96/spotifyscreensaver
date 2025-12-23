using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace SpotifyScreenSaver;

public partial class ConfigWindow : Window
{
    private SpotifyAuth? _auth;
    private readonly SpotifyApi _api = new();

    public ConfigWindow()
    {
        InitializeComponent();
        
        var clientId = AppConfig.LoadClientId();
        if (!string.IsNullOrWhiteSpace(clientId))
        {
            ClientIdBox.Text = clientId;
            _auth = new SpotifyAuth(clientId, AppConfig.RedirectUri, AppConfig.ListenerPrefix);
            Append("Ready. Client ID loaded.");
        }
        else
        {
            Append("⚠️ Please enter your Spotify Client ID and click Save.");
            LoginBtn.IsEnabled = false;
            TestBtn.IsEnabled = false;
        }
    }

    private void SaveClientId_Click(object sender, RoutedEventArgs e)
    {
        var clientId = ClientIdBox.Text.Trim();
        
        if (string.IsNullOrWhiteSpace(clientId))
        {
            Append("❌ Client ID cannot be empty.");
            return;
        }
        
        AppConfig.SaveClientId(clientId);
        _auth = new SpotifyAuth(clientId, AppConfig.RedirectUri, AppConfig.ListenerPrefix);
        
        LoginBtn.IsEnabled = true;
        TestBtn.IsEnabled = true;
        
        Append("✅ Client ID saved successfully.");
    }

    private async void Login_Click(object sender, RoutedEventArgs e)
    {
        if (_auth == null)
        {
            Append("❌ Please save Client ID first.");
            return;
        }
        
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
        if (_auth == null)
        {
            Append("❌ Please save Client ID first.");
            return;
        }
        
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
        if (_auth == null)
        {
            Append("❌ Please save Client ID first.");
            return;
        }
        
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