using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SpotifyScreenSaver;

public sealed class SpotifyApi
{
    private static readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    public record NowPlaying(
        bool IsPlaying,
        string TrackName,
        string ArtistName,
        string AlbumName,
        string AlbumArtUrl,
        string SpotifyUrl,
        int ProgressMs,
        int DurationMs
    );

    public record QueueInfo(
        string? NextTrackName,
        string? NextArtistName
    );

    public async Task<(HttpStatusCode Status, NowPlaying? Data, string Raw)> GetNowPlayingAsync(string accessToken, CancellationToken ct)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.spotify.com/v1/me/player/currently-playing");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var resp = await _httpClient.SendAsync(request, ct);

        if (resp.StatusCode == HttpStatusCode.NoContent)
            return (resp.StatusCode, null, "");

        var raw = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
            return (resp.StatusCode, null, raw);

        using var doc = JsonDocument.Parse(raw);

        var isPlaying = doc.RootElement.GetProperty("is_playing").GetBoolean();
        var progress = doc.RootElement.TryGetProperty("progress_ms", out var p) ? p.GetInt32() : 0;

        var item = doc.RootElement.GetProperty("item");
        var trackName = item.GetProperty("name").GetString() ?? "Unknown";
        var duration = item.TryGetProperty("duration_ms", out var d) ? d.GetInt32() : 0;

        var artistsEl = item.GetProperty("artists");
        var artists = "";
        for (int i = 0; i < artistsEl.GetArrayLength(); i++)
        {
            var n = artistsEl[i].GetProperty("name").GetString();
            if (!string.IsNullOrWhiteSpace(n))
                artists += (artists.Length == 0 ? "" : ", ") + n;
        }

        var album = item.GetProperty("album");
        var albumName = album.GetProperty("name").GetString() ?? "";

        var images = album.GetProperty("images");
        var art = images.GetArrayLength() > 0 ? images[0].GetProperty("url").GetString() ?? "" : "";

        var spotifyUrl = "";
        if (item.TryGetProperty("external_urls", out var eu) && eu.TryGetProperty("spotify", out var su))
            spotifyUrl = su.GetString() ?? "";

        return (resp.StatusCode,
            new NowPlaying(isPlaying, trackName, artists, albumName, art, spotifyUrl, progress, duration),
            raw);
    }

    public async Task<(HttpStatusCode Status, QueueInfo? Data, string Raw)> GetQueueAsync(string accessToken, CancellationToken ct)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.spotify.com/v1/me/player/queue");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var resp = await _httpClient.SendAsync(request, ct);

        if (resp.StatusCode == HttpStatusCode.NoContent)
            return (resp.StatusCode, null, "");

        var raw = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
            return (resp.StatusCode, null, raw);

        using var doc = JsonDocument.Parse(raw);

        if (!doc.RootElement.TryGetProperty("queue", out var queue) || queue.GetArrayLength() == 0)
            return (resp.StatusCode, new QueueInfo(null, null), raw);

        var nextTrack = queue[0];
        var nextTrackName = nextTrack.GetProperty("name").GetString();

        var artistsEl = nextTrack.GetProperty("artists");
        var nextArtists = "";
        for (int i = 0; i < artistsEl.GetArrayLength(); i++)
        {
            var n = artistsEl[i].GetProperty("name").GetString();
            if (!string.IsNullOrWhiteSpace(n))
                nextArtists += (nextArtists.Length == 0 ? "" : ", ") + n;
        }

        return (resp.StatusCode, new QueueInfo(nextTrackName, nextArtists), raw);
    }
}