using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SpotifyScreenSaver;

public sealed class TokenStore
{
    private static readonly string Dir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SpotifyNowPlayingScreensaver");
    private static readonly string FilePath = Path.Combine(Dir, "tokens.dat");

    public record TokenData(string AccessToken, string RefreshToken, long ExpiresAtUnix);

    public void Save(TokenData data)
    {
        Directory.CreateDirectory(Dir);
        var json = JsonSerializer.Serialize(data);
        var plain = Encoding.UTF8.GetBytes(json);
        var protectedBytes = ProtectedData.Protect(plain, null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(FilePath, protectedBytes);
    }

    public TokenData? Load()
    {
        if (!File.Exists(FilePath)) return null;

        try
        {
            var protectedBytes = File.ReadAllBytes(FilePath);
            var plain = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);
            var json = Encoding.UTF8.GetString(plain);
            return JsonSerializer.Deserialize<TokenData>(json);
        }
        catch
        {
            return null;
        }
    }

    public void Clear()
    {
        try { if (File.Exists(FilePath)) File.Delete(FilePath); } catch { }
    }
}