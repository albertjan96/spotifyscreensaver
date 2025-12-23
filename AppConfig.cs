namespace SpotifyScreenSaver;

public static class AppConfig
{
    // Redirect URI - MUST be exactly the same as in Spotify Developer Dashboard
    public const string RedirectUri = "http://127.0.0.1:5543/callback";

    // HttpListener prefixes must end with /
    public const string ListenerPrefix = "http://127.0.0.1:5543/callback/";
    
    // Client ID storage location
    private static readonly string ConfigDir = 
        System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "SpotifyNowPlayingScreensaver");
    private static readonly string ClientIdFile = System.IO.Path.Combine(ConfigDir, "clientid.txt");
    
    public static string? LoadClientId()
    {
        try
        {
            if (System.IO.File.Exists(ClientIdFile))
            {
                var id = System.IO.File.ReadAllText(ClientIdFile).Trim();
                return string.IsNullOrWhiteSpace(id) ? null : id;
            }
        }
        catch { }
        return null;
    }
    
    public static void SaveClientId(string clientId)
    {
        try
        {
            System.IO.Directory.CreateDirectory(ConfigDir);
            System.IO.File.WriteAllText(ClientIdFile, clientId.Trim());
        }
        catch { }
    }
}