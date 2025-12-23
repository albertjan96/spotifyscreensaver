using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SpotifyScreenSaver;

public sealed class SpotifyAuth
{
    private readonly string _clientId;
    private readonly string _redirectUri;      // exact (zoals in Spotify dashboard)
    private readonly string _listenerPrefix;   // met trailing slash voor HttpListener
    private readonly TokenStore _tokenStore = new();

    public SpotifyAuth(string clientId, string redirectUri, string listenerPrefix)
    {
        _clientId = clientId;
        _redirectUri = redirectUri;
        _listenerPrefix = listenerPrefix;
    }

    public TokenStore.TokenData? LoadTokens() => _tokenStore.Load();
    public void ClearTokens() => _tokenStore.Clear();

    public async Task<TokenStore.TokenData> LoginWithPkceAsync(CancellationToken ct)
    {
        var verifier = Pkce.CreateCodeVerifier();
        var challenge = Pkce.CreateCodeChallenge(verifier);
        var state = Guid.NewGuid().ToString("N");

        var scope = "user-read-currently-playing user-read-playback-state";

        var authUrl =
            "https://accounts.spotify.com/authorize" +
            $"?client_id={Uri.EscapeDataString(_clientId)}" +
            $"&response_type=code" +
            $"&redirect_uri={Uri.EscapeDataString(_redirectUri)}" +
            $"&code_challenge_method=S256" +
            $"&code_challenge={Uri.EscapeDataString(challenge)}" +
            $"&state={Uri.EscapeDataString(state)}" +
            $"&scope={Uri.EscapeDataString(scope)}";

        using var listener = new HttpListener();
        listener.Prefixes.Add(_listenerPrefix);
        listener.Start();

        Process.Start(new ProcessStartInfo(authUrl) { UseShellExecute = true });

        var context = await listener.GetContextAsync().WaitAsync(ct);

        var request = context.Request;
        var query = request.QueryString;

        var code = query["code"];
        var returnedState = query["state"];
        var error = query["error"];

        string responseHtml;
        if (!string.IsNullOrWhiteSpace(error))
        {
            responseHtml = $"<html><body><h2>Spotify login error</h2><p>{WebUtility.HtmlEncode(error)}</p>You can close this tab.</body></html>";
            await RespondAsync(context.Response, responseHtml);
            throw new InvalidOperationException($"Spotify auth error: {error}");
        }

        if (string.IsNullOrWhiteSpace(code) || returnedState != state)
        {
            responseHtml = "<html><body><h2>Spotify login failed</h2><p>Invalid response.</p>You can close this tab.</body></html>";
            await RespondAsync(context.Response, responseHtml);
            throw new InvalidOperationException("Invalid auth callback (missing code or state mismatch).");
        }

        responseHtml = "<html><body><h2>Spotify login OK ✅</h2><p>You can close this tab and return to the screensaver config.</p></body></html>";
        await RespondAsync(context.Response, responseHtml);

        using var http = new HttpClient();
        var tokenUrl = "https://accounts.spotify.com/api/token";

        var body = new Dictionary<string, string>
        {
            ["client_id"] = _clientId,
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = _redirectUri,   // MUST be exactly the same as authorize + dashboard
            ["code_verifier"] = verifier,
        };

        var resp = await http.PostAsync(tokenUrl, new FormUrlEncodedContent(body), ct);
        var json = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"Token exchange failed: {resp.StatusCode} {json}");

        using var doc = JsonDocument.Parse(json);
        var access = doc.RootElement.GetProperty("access_token").GetString()!;
        var refresh = doc.RootElement.GetProperty("refresh_token").GetString()!;
        var expiresIn = doc.RootElement.GetProperty("expires_in").GetInt32();

        var expiresAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + expiresIn - 30;
        var data = new TokenStore.TokenData(access, refresh, expiresAt);
        _tokenStore.Save(data);
        return data;
    }

    public async Task<TokenStore.TokenData> RefreshAsync(string refreshToken, CancellationToken ct)
    {
        using var http = new HttpClient();
        var tokenUrl = "https://accounts.spotify.com/api/token";

        var body = new Dictionary<string, string>
        {
            ["client_id"] = _clientId,
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken
        };

        var resp = await http.PostAsync(tokenUrl, new FormUrlEncodedContent(body), ct);
        var json = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"Token refresh failed: {resp.StatusCode} {json}");

        using var doc = JsonDocument.Parse(json);
        var access = doc.RootElement.GetProperty("access_token").GetString()!;
        var expiresIn = doc.RootElement.GetProperty("expires_in").GetInt32();

        var newRefresh = refreshToken;
        if (doc.RootElement.TryGetProperty("refresh_token", out var rt) &&
            rt.GetString() is string rts && !string.IsNullOrWhiteSpace(rts))
        {
            newRefresh = rts;
        }

        var expiresAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + expiresIn - 30;
        var data = new TokenStore.TokenData(access, newRefresh, expiresAt);
        _tokenStore.Save(data);
        return data;
    }

    private static async Task RespondAsync(HttpListenerResponse response, string html)
    {
        var bytes = Encoding.UTF8.GetBytes(html);
        response.ContentType = "text/html; charset=utf-8";
        response.ContentLength64 = bytes.Length;
        await response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
        response.OutputStream.Close();
    }
}
