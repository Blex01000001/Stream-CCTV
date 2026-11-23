namespace StreamCCTV.Models;

public enum TileSource { YouTube, YouTubeChannel, Twitch }

public enum TileStatus { Loading, Playing, Error, NotLive, Offline }

public class Tile
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N")[..8];
    public TileSource Source { get; init; }
    /// <summary>影片 ID、頻道 ID 或 Twitch 名稱</summary>
    public string Ref { get; init; } = "";
    public string? ChannelName { get; set; }
    public int? Viewers { get; set; }
    public TileStatus Status { get; set; } = TileStatus.Loading;
    public string? StatusMessage { get; set; }
    /// <summary>在目前布局中的格位編號（0 起算）</summary>
    public int Slot { get; set; }
    /// <summary>CSS grid-area 值，由 WallState 依 Slot 與布局指定</summary>
    public string GridArea { get; set; } = "";
    /// <summary>此格的控制模式（遮罩穿透）</summary>
    public bool Control { get; set; }

    public string DisplayName => string.IsNullOrWhiteSpace(ChannelName) ? Ref : ChannelName!;

    public string JsSource => Source switch
    {
        TileSource.YouTube => "youtube",
        TileSource.YouTubeChannel => "ytchannel",
        _ => "twitch"
    };

    public string OpenUrl => Source switch
    {
        TileSource.YouTube => $"https://www.youtube.com/watch?v={Ref}",
        TileSource.YouTubeChannel => $"https://www.youtube.com/channel/{Ref}/live",
        _ => $"https://www.twitch.tv/{Ref}"
    };

    public string OpenSiteName => Source == TileSource.Twitch ? "Twitch" : "YouTube";
}
