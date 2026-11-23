namespace StreamCCTV.Models;

public class MemberList
{
    public string Name { get; set; } = "";
    /// <summary>每項是一個網址 / 影片 ID / 頻道 ID / Twitch 網址</summary>
    public List<string> Entries { get; set; } = new();
}

public class AppSettings
{
    public string HolodexApiKey { get; set; } = "";
    public string FilterKeyword { get; set; } = "Among Us";
    public string Org { get; set; } = "Hololive";
    public int Volume { get; set; } = 60;
}

public class PersistedTile
{
    public TileSource Source { get; set; }
    public string Ref { get; set; } = "";
    public int Slot { get; set; }
    public string? Name { get; set; }
}
