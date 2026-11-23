namespace StreamCCTV.Models;

// Holodex /api/v2/live 回傳的欄位（snake_case，由 JsonNamingPolicy.SnakeCaseLower 對應）。
// 實際 JSON 請用「設定 → 探勘 Holodex」確認後再調整。
public class HolodexVideo
{
    public string? Id { get; set; }
    public string? Title { get; set; }
    /// <summary>stream / placeholder / clip</summary>
    public string? Type { get; set; }
    public string? TopicId { get; set; }
    public string? Status { get; set; }
    /// <summary>placeholder 專用：外部連結（例如 twitch.tv）</summary>
    public string? Link { get; set; }
    public int? LiveViewers { get; set; }
    public HolodexChannel? Channel { get; set; }
}

public class HolodexChannel
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? EnglishName { get; set; }
    public string? Photo { get; set; }
    public string? Org { get; set; }
}

public record HolodexResult(List<HolodexVideo> Videos, string? Error);
