using System;
using System.Security.Cryptography;
using System.Text;

namespace SpotifyScreenSaver;

public static class Pkce
{
    public static string CreateCodeVerifier()
    {
        // 43-128 chars, URL-safe
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Base64Url(bytes);
    }

    public static string CreateCodeChallenge(string verifier)
    {
        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(verifier));
        return Base64Url(hash);
    }

    private static string Base64Url(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}