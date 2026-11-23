namespace StreamCCTV.Services;

/// <summary>簡易在地化：英文（預設）與繁體中文。語言存在 localStorage，下次開啟沿用。</summary>
public class Loc
{
    readonly StorageService _storage;
    public Loc(StorageService storage) => _storage = storage;

    public string Lang { get; private set; } = "en";
    public bool IsZh => Lang == "zh";
    public event Action? OnChange;

    public async Task InitAsync()
    {
        var saved = await _storage.GetAsync<string>("lang");
        Lang = saved == "zh" ? "zh" : "en";   // 第一次開啟預設英文
    }

    public void Toggle() => Set(IsZh ? "en" : "zh");

    public void Set(string lang)
    {
        Lang = lang == "zh" ? "zh" : "en";
        _ = _storage.SetAsync("lang", Lang);
        OnChange?.Invoke();
    }

    public string this[string key] =>
        T.TryGetValue(key, out var v) ? (IsZh ? v.Zh : v.En) : key;

    public string F(string key, params object[] args) => string.Format(this[key], args);

    public string LayoutName(Models.LayoutDef l) => l.Id switch
    {
        "auto" => this["layout.auto"],
        "1b3" => this["layout.1b3"],
        "1b5" => this["layout.1b5"],
        _ => l.Name
    };

    record Pair(string En, string Zh);
    static Pair P(string en, string zh) => new(en, zh);

    static readonly Dictionary<string, Pair> T = new()
    {
        ["app.title"] = P("Stream CCTV", "Stream CCTV"),

        // toolbar
        ["tb.placeholder"] = P("Paste YouTube / Twitch URLs or IDs, press Enter", "貼上 YouTube / Twitch 網址或 ID，按 Enter"),
        ["tb.add"] = P("Add", "加入"),
        ["tb.layout"] = P("Layout", "布局"),
        ["tb.split"] = P("{0}-way split ({1})", "{0} 分割（{1}）"),
        ["tb.members"] = P("Member lists…", "成員名單…"),
        ["tb.load"] = P("Load", "載入"),
        ["tb.share"] = P("Share", "分享"),
        ["tb.volume"] = P("Volume of the current audio tile", "目前音源格的音量"),
        ["tb.muteall"] = P("Mute all", "全靜音"),
        ["tb.muteall.tip"] = P("Mute all (M)", "全部靜音（M）"),
        ["tb.control"] = P("Global control mode (C)", "全域控制模式（C）"),
        ["tb.hide"] = P("Hide", "隱藏"),
        ["tb.hide.tip"] = P("Hide toolbar and title bars (H)", "隱藏工具列與標題列（H）"),
        ["tb.fullscreen"] = P("Fullscreen (F)", "全螢幕（F）"),
        ["tb.settings"] = P("Settings", "設定"),
        ["tb.help"] = P("Keyboard shortcuts (?)", "快捷鍵（?）"),
        ["tb.lang"] = P("Language: English (click for 繁體中文)", "語言：繁體中文（點擊切換為 English）"),
        ["tb.copied"] = P("Share link copied to clipboard", "已複製分享連結到剪貼簿"),
        ["tb.copyfail"] = P("Could not copy. Link: {0}", "無法複製，連結：{0}"),

        // layouts
        ["layout.auto"] = P("Auto", "自動"),
        ["layout.1b3"] = P("1 big + 3 small", "1 大 + 3 小"),
        ["layout.1b5"] = P("1 big + 5 small", "1 大 + 5 小"),

        // wall / tiles
        ["wall.add"] = P("＋ Add", "＋ 加入"),
        ["tile.grip"] = P("Drag to swap position", "拖曳以交換位置"),
        ["tile.ctlmode"] = P("Control mode", "控制模式"),
        ["tile.audio"] = P("Switch audio here", "切換音源"),
        ["tile.control"] = P("Control mode (use the player's own controls)", "控制模式（可操作 YouTube 控制列）"),
        ["tile.reload"] = P("Reload", "重新整理"),
        ["tile.open"] = P("Open on {0}", "在 {0} 開啟"),
        ["tile.remove"] = P("Remove", "移除"),
        ["tile.hint"] = P("Click to listen", "點擊收聽"),
        ["tile.notlive"] = P("Not live", "未開台"),
        ["tile.offline"] = P("Offline", "離線"),
        ["tile.cantplay"] = P("Cannot play", "無法播放"),
        ["err.2"] = P("Invalid video ID", "影片 ID 無效"),
        ["err.5"] = P("Player error — try reloading", "播放器發生錯誤，請重新整理"),
        ["err.100"] = P("Video not found, deleted or private", "影片不存在、已刪除或設為私人"),
        ["err.101"] = P("Embedding is disabled or the video is members-only", "此影片禁止嵌入或為會員限定"),
        ["err.script"] = P("Player script failed to load (network or blocker?)", "播放器腳本載入失敗（網路或擴充功能封鎖？）"),
        ["err.other"] = P("Cannot play (error {0})", "無法播放（錯誤 {0}）"),

        // state notices
        ["n.duplicate"] = P("That stream is already on the wall", "這個畫面已經在牆上了"),
        ["n.unrecognized"] = P("Unrecognized: {0}", "無法辨識：{0}"),
        ["n.full"] = P("Layout was full — switched to “{0}”", "目前布局已滿，已自動切換為「{0}」"),
        ["n.toomany"] = P("Too many streams for that layout — using “Auto”", "格數超過該布局容量，已改用「自動」布局"),

        // live bar
        ["live.empty"] = P("No live streams match “{0}”", "目前沒有符合「{0}」的直播"),
        ["live.denied"] = P("Holodex API key is invalid or was rejected", "Holodex API Key 無效或被拒絕"),
        ["live.http"] = P("Holodex returned HTTP {0}", "Holodex 回應 {0}"),
        ["live.conn"] = P("Cannot reach Holodex: {0}", "無法連線 Holodex：{0}"),

        // settings
        ["set.title"] = P("Settings", "設定"),
        ["set.key.ph"] = P("Stored only in this browser's localStorage", "只存在這台瀏覽器的 localStorage"),
        ["set.org"] = P("Organization (org)", "組織 (org)"),
        ["set.keyword"] = P("Title keyword / topic", "標題關鍵字 / topic"),
        ["set.keyword.ph"] = P("Empty = show all", "留空＝顯示全部"),
        ["set.save"] = P("Save", "儲存"),
        ["set.probe"] = P("Probe Holodex (show raw JSON)", "探勘 Holodex（顯示原始 JSON）"),
        ["set.loading"] = P("Loading…", "讀取中…"),
        ["set.truncated"] = P("…(truncated; full output in the browser console)", "…（已截斷，完整內容見瀏覽器 console）"),
        ["set.failed"] = P("Failed: {0}", "失敗：{0}"),
        ["set.saved"] = P("Settings saved", "設定已儲存"),
        ["mem.title"] = P("Member lists", "成員名單"),
        ["mem.new"] = P("＋ New list", "＋ 新增名單"),
        ["mem.delete"] = P("Delete this list", "刪除此名單"),
        ["mem.name"] = P("Name", "名稱"),
        ["mem.entries"] = P("Channels (one URL / video ID / channel ID per line)", "頻道清單（每行一個網址 / 影片 ID / 頻道 ID）"),
        ["mem.create"] = P("Create list", "新增名單"),
        ["mem.update"] = P("Save changes", "儲存修改"),
        ["mem.needname"] = P("Please enter a list name", "請輸入名單名稱"),
        ["mem.dupe"] = P("A list with that name already exists", "已有同名名單"),

        // help
        ["help.title"] = P("Keyboard shortcuts", "快捷鍵"),
        ["help.digits"] = P("Switch audio to tile N (by screen position)", "切換音源到第 N 格（依畫面位置）"),
        ["help.m"] = P("Mute all", "全部靜音"),
        ["help.c"] = P("Global control mode (use the YouTube controls)", "全域控制模式（可操作 YouTube 控制列）"),
        ["help.f"] = P("Fullscreen", "全螢幕"),
        ["help.h"] = P("Hide / show toolbar and title bars", "隱藏／顯示工具列與標題列"),
        ["help.esc"] = P("Un-zoom / close dialogs", "取消放大／關閉視窗"),
        ["help.q"] = P("Show this list", "顯示此列表"),
        ["help.click"] = P("Click video / title bar", "單擊畫面／標題列"),
        ["help.click.d"] = P("Audio moves to that tile; click again to mute all", "音源切到該格；再點一次＝全部靜音"),
        ["help.dbl"] = P("Double-click title bar", "雙擊標題列"),
        ["help.dbl.d"] = P("Zoom / restore that tile", "放大／還原該格"),
        ["help.drag"] = P("Drag title bar", "拖曳標題列"),
        ["help.drag.d"] = P("Swap places with another tile", "與另一格交換位置"),
        ["help.note"] = P("Shortcuts are disabled while an input is focused.", "輸入框聚焦時快捷鍵不會觸發。"),
    };
}
