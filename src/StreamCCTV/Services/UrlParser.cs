using System.Text.RegularExpressions;
using StreamCCTV.Models;

namespace StreamCCTV.Services;

public record ParsedSource(TileSource Source, string Ref);

public static partial class UrlParser
{
    [GeneratedRegex(@"^[A-Za-z0-9_-]{11}$")] private static partial Regex VideoIdRx();
    [GeneratedRegex(@"^UC[A-Za-z0-9_-]{22}$")] private static partial Regex ChannelIdRx();
    [GeneratedRegex(@"^[A-Za-z0-9_]{2,25}$")] private static partial Regex TwitchNameRx();

    static readonly HashSet<string> TwitchReserved = new(StringComparer.OrdinalIgnoreCase)
        { "videos", "directory", "downloads", "settings", "p", "login", "signup", "search", "embed" };

    /// <summary>把貼上的文字切成一行一個項目（也接受逗號或空白分隔）</summary>
    public static IEnumerable<string> SplitInput(string? text) =>
        (text ?? "").Split(new[] { '\r', '\n', ',', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim()).Where(s => s.Length > 0);

    public static ParsedSource? Parse(string? raw)
    {
        var s = raw?.Trim();
        if (string.IsNullOrEmpty(s)) return null;

        if (ChannelIdRx().IsMatch(s)) return new(TileSource.YouTubeChannel, s);
        if (VideoIdRx().IsMatch(s)) return new(TileSource.YouTube, s);

        var candidate = s.Contains("://") ? s : "https://" + s;
        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri)) return null;

        var host = uri.Host.ToLowerInvariant();
        if (host.StartsWith("www.")) host = host[4..];
        if (host.StartsWith("m.")) host = host[2..];
        var segs = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var q = ParseQuery(uri.Query);

        if (host == "youtu.be")
            return segs.Length > 0 && VideoIdRx().IsMatch(segs[0]) ? new(TileSource.YouTube, segs[0]) : null;

        if (host is "youtube.com" or "music.youtube.com" or "youtube-nocookie.com")
        {
            if (segs.Length == 0) return null;
            switch (segs[0].ToLowerInvariant())
            {
                case "watch":
                    return q.TryGetValue("v", out var v) && VideoIdRx().IsMatch(v) ? new(TileSource.YouTube, v) : null;
                case "live":
                case "shorts":
                case "v":
                    return segs.Length > 1 && VideoIdRx().IsMatch(segs[1]) ? new(TileSource.YouTube, segs[1]) : null;
                case "embed":
                    if (segs.Length > 1 && segs[1] == "live_stream")
                        return q.TryGetValue("channel", out var ch) && ChannelIdRx().IsMatch(ch) ? new(TileSource.YouTubeChannel, ch) : null;
                    return segs.Length > 1 && VideoIdRx().IsMatch(segs[1]) ? new(TileSource.YouTube, segs[1]) : null;
                case "channel":
                    return segs.Length > 1 && ChannelIdRx().IsMatch(segs[1]) ? new(TileSource.YouTubeChannel, segs[1]) : null;
            }
            return null; // /@handle 無法在不呼叫 YouTube Data API 的情況下解析
        }

        if (host == "player.twitch.tv")
            return q.TryGetValue("channel", out var c) && TwitchNameRx().IsMatch(c) ? new(TileSource.Twitch, c.ToLowerInvariant()) : null;

        if (host is "twitch.tv" or "go.twitch.tv")
        {
            if (segs.Length == 0 || TwitchReserved.Contains(segs[0]) || !TwitchNameRx().IsMatch(segs[0])) return null;
            return new(TileSource.Twitch, segs[0].ToLowerInvariant());
        }
        return null;
    }

    public static Dictionary<string, string> ParseQuery(string? query)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(query)) return dict;
        foreach (var part in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var i = part.IndexOf('=');
            var k = Uri.UnescapeDataString(i < 0 ? part : part[..i]);
            var v = i < 0 ? "" : Uri.UnescapeDataString(part[(i + 1)..].Replace('+', ' '));
            dict[k] = v;
        }
        return dict;
    }
}
