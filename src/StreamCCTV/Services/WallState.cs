using Microsoft.AspNetCore.Components;
using StreamCCTV.Models;

namespace StreamCCTV.Services;

/// <summary>
/// 牆的所有狀態：格子清單、布局、音源格、設定與成員名單。
/// 注意：Tiles 清單的順序永遠不變（只在尾端新增、移除時刪除），
/// 位置調整只改 Tile.Slot / GridArea，這樣 Blazor 不會搬動 DOM、直播不會重新載入。
/// </summary>
public class WallState
{
    readonly PlayerService _player;
    readonly StorageService _storage;
    readonly NavigationManager _nav;
    readonly Loc _loc;

    public WallState(PlayerService player, StorageService storage, NavigationManager nav, Loc loc)
    {
        _player = player; _storage = storage; _nav = nav; _loc = loc;
    }

    public List<Tile> Tiles { get; } = new();
    public LayoutDef Layout { get; private set; } = Layouts.Get("2x2");
    public string? ActiveAudioId { get; private set; }
    public string? ZoomedId { get; private set; }
    public bool GlobalControl { get; private set; }
    /// <summary>工具列（含 Holodex 直播列）是否隱藏</summary>
    public bool HideToolbar { get; private set; }
    /// <summary>各格標題列是否隱藏（滑鼠移到格子上方仍會淡入）</summary>
    public bool HideTitles { get; private set; }
    public AppSettings Settings { get; private set; } = new();
    public List<MemberList> Members { get; private set; } = new();
    public string? Notice { get; private set; }

    public event Action? OnChange;
    public event Action? OnSettingsChanged;
    public event Action? OnFocusInput;

    /// <summary>目前實際使用的格線（自動布局依格數計算）</summary>
    public LayoutDef Grid => Layout.Auto ? Layouts.Auto(Tiles.Count) : Layout;

    public void Notify() => OnChange?.Invoke();
    public void RequestFocusInput() => OnFocusInput?.Invoke();

    CancellationTokenSource? _noticeCts;
    public async void ShowNotice(string message)
    {
        _noticeCts?.Cancel();
        var cts = _noticeCts = new CancellationTokenSource();
        Notice = message; Notify();
        try { await Task.Delay(5000, cts.Token); Notice = null; Notify(); } catch (TaskCanceledException) { }
    }

    // ───────── 載入 / 儲存 ─────────

    public async Task LoadAsync()
    {
        Settings = await _storage.GetAsync<AppSettings>("settings") ?? new();
        Members = await _storage.GetAsync<List<MemberList>>("members") ?? new();
        var layoutId = await _storage.GetAsync<string>("layout");

        var q = UrlParser.ParseQuery(_nav.ToAbsoluteUri(_nav.Uri).Query);
        var urlTiles = new List<PersistedTile>();
        void Collect(string key, TileSource src)
        {
            if (!q.TryGetValue(key, out var val)) return;
            foreach (var item in val.Split(',', StringSplitOptions.RemoveEmptyEntries))
                urlTiles.Add(new PersistedTile { Source = src, Ref = item.Trim(), Slot = urlTiles.Count });
        }
        Collect("v", TileSource.YouTube);
        Collect("ch", TileSource.YouTubeChannel);
        Collect("tw", TileSource.Twitch);
        var fromUrl = urlTiles.Count > 0;
        if (q.TryGetValue("layout", out var ql) && Layouts.Find(ql) is not null) { layoutId = ql; fromUrl = true; }

        Layout = Layouts.Find(layoutId) ?? Layouts.Get("2x2");
        var source = fromUrl && urlTiles.Count > 0 ? urlTiles : await _storage.GetAsync<List<PersistedTile>>("tiles") ?? new();
        foreach (var p in source.OrderBy(t => t.Slot))
            Tiles.Add(new Tile { Source = p.Source, Ref = p.Ref, ChannelName = p.Name, Slot = p.Slot });

        if (!Layout.Auto && Tiles.Count > Layout.Capacity)
            Layout = Layouts.All.Where(l => !l.Auto && l.Capacity >= Tiles.Count).OrderBy(l => l.Capacity).FirstOrDefault() ?? Layouts.AutoPlaceholder;
        Normalize();
        if (fromUrl) Save();
        Notify();
    }

    public void Save() => _ = SaveAsync();

    async Task SaveAsync()
    {
        await _storage.SetAsync("tiles", Tiles.Select(t => new PersistedTile { Source = t.Source, Ref = t.Ref, Slot = t.Slot, Name = t.ChannelName }).ToList());
        await _storage.SetAsync("layout", Layout.Id);
        await _storage.SetAsync("members", Members);
        await _storage.SetAsync("settings", Settings);
    }

    // ───────── 格子 ─────────

    /// <summary>重新整理 Slot（避免超出容量或重複）並更新 GridArea</summary>
    void Normalize()
    {
        var grid = Grid;
        var ordered = Tiles.OrderBy(t => t.Slot).ToList();
        if (Layout.Auto)
        {
            for (var i = 0; i < ordered.Count; i++) ordered[i].Slot = i;
        }
        else
        {
            var used = new HashSet<int>();
            var pending = new List<Tile>();
            foreach (var t in ordered)
            {
                if (t.Slot >= 0 && t.Slot < grid.Capacity && used.Add(t.Slot)) continue;
                pending.Add(t);
            }
            var next = 0;
            foreach (var t in pending)
            {
                while (used.Contains(next)) next++;
                t.Slot = next; used.Add(next);
            }
        }
        RefreshAreas();
    }

    void RefreshAreas()
    {
        var grid = Grid;
        foreach (var t in Tiles)
            t.GridArea = t.Id == ZoomedId ? "1 / 1 / -1 / -1" : grid.Areas[Math.Clamp(t.Slot, 0, grid.Capacity - 1)];
    }

    public IEnumerable<Tile> ByPosition => Tiles.OrderBy(t => t.Slot);

    public IEnumerable<(int Slot, string Area)> FreeSlots()
    {
        var grid = Grid;
        var used = Tiles.Select(t => t.Slot).ToHashSet();
        for (var i = 0; i < grid.Capacity; i++)
            if (!used.Contains(i)) yield return (i, grid.Areas[i]);
    }

    public bool HasSource(TileSource source, string reference) =>
        Tiles.Any(t => t.Source == source && string.Equals(t.Ref, reference, StringComparison.OrdinalIgnoreCase));

    public Tile? AddTile(TileSource source, string reference, string? name = null)
    {
        if (HasSource(source, reference)) { ShowNotice(_loc["n.duplicate"]); return null; }

        int slot;
        if (Layout.Auto)
        {
            slot = Tiles.Count; // 自動布局：永遠補在最後（格數確定後格線會重算）
        }
        else
        {
            var used = Tiles.Select(t => t.Slot).ToHashSet();
            slot = Enumerable.Range(0, Layout.Capacity).FirstOrDefault(i => !used.Contains(i), -1);
            if (slot < 0)
            {
                // 已滿：換成放得下的最小布局，沒有就用自動
                var bigger = Layouts.All.Where(l => !l.Auto && l.Capacity > Tiles.Count)
                                        .OrderBy(l => l.Capacity).FirstOrDefault() ?? Layouts.AutoPlaceholder;
                Layout = bigger;
                Normalize();
                used = Tiles.Select(t => t.Slot).ToHashSet();
                slot = bigger.Auto ? Tiles.Count : Enumerable.Range(0, bigger.Capacity).First(i => !used.Contains(i));
                ShowNotice(_loc.F("n.full", _loc.LayoutName(bigger)));
            }
        }

        var tile = new Tile { Source = source, Ref = reference, ChannelName = name, Slot = slot };
        Tiles.Add(tile);
        Normalize();
        Save(); Notify();
        return tile;
    }

    /// <summary>從貼上的文字加入多個來源；回傳無法辨識的項目</summary>
    public List<string> AddFromText(string? text)
    {
        var failed = new List<string>();
        foreach (var item in UrlParser.SplitInput(text))
        {
            var p = UrlParser.Parse(item);
            if (p is null) failed.Add(item);
            else AddTile(p.Source, p.Ref);
        }
        if (failed.Count > 0) ShowNotice(_loc.F("n.unrecognized", string.Join(", ", failed)));
        return failed;
    }

    public List<string> LoadMemberList(MemberList list) => AddFromText(string.Join('\n', list.Entries));

    public async Task RemoveTileAsync(string id)
    {
        var t = Tiles.FirstOrDefault(x => x.Id == id);
        if (t is null) return;
        if (ActiveAudioId == id) ActiveAudioId = null;
        if (ZoomedId == id) ZoomedId = null;
        Tiles.Remove(t);
        Normalize();
        Save(); Notify();
        await Task.CompletedTask;
    }

    public void SwapTiles(string idA, string idB)
    {
        var a = Tiles.FirstOrDefault(t => t.Id == idA);
        var b = Tiles.FirstOrDefault(t => t.Id == idB);
        if (a is null || b is null || a == b) return;
        (a.Slot, b.Slot) = (b.Slot, a.Slot);
        (a.GridArea, b.GridArea) = (b.GridArea, a.GridArea);
        RefreshAreas();
        Save(); Notify();
    }

    public void MoveToSlot(string id, int slot)
    {
        var t = Tiles.FirstOrDefault(x => x.Id == id);
        if (t is null || slot < 0 || slot >= Grid.Capacity || Tiles.Any(x => x.Slot == slot)) return;
        t.Slot = slot;
        Normalize(); Save(); Notify();
    }

    public void SetLayout(string id)
    {
        var target = Layouts.Find(id) ?? Layout;
        if (!target.Auto && Tiles.Count > target.Capacity)
        {
            target = Layouts.AutoPlaceholder;
            ShowNotice(_loc["n.toomany"]);
        }
        Layout = target;
        Normalize(); Save(); Notify();
    }

    public void ToggleZoom(string id)
    {
        ZoomedId = ZoomedId == id ? null : id;
        RefreshAreas(); Notify();
    }

    public void ClearZoom()
    {
        if (ZoomedId is null) return;
        ZoomedId = null; RefreshAreas(); Notify();
    }

    public void ToggleControl(string id)
    {
        var t = Tiles.FirstOrDefault(x => x.Id == id);
        if (t is null) return;
        t.Control = !t.Control; Notify();
    }

    public void ToggleGlobalControl() { GlobalControl = !GlobalControl; Notify(); }
    public void ToggleToolbar() { HideToolbar = !HideToolbar; Notify(); }
    public void ToggleTitles() { HideTitles = !HideTitles; Notify(); }

    /// <summary>H 鍵：有任何一項顯示就全部隱藏，兩項都已隱藏就全部還原</summary>
    public void ToggleHideBars()
    {
        var hide = !(HideToolbar && HideTitles);
        HideToolbar = HideTitles = hide;
        Notify();
    }
    public bool IsControl(Tile t) => GlobalControl || t.Control;

    public void SetStatus(string id, TileStatus status, string? message = null)
    {
        var t = Tiles.FirstOrDefault(x => x.Id == id);
        if (t is null || (t.Status == status && t.StatusMessage == message)) return;
        t.Status = status; t.StatusMessage = message; Notify();
    }

    // ───────── 音源（獨佔模式） ─────────

    public async Task ToggleAudioAsync(string id)
    {
        ActiveAudioId = ActiveAudioId == id ? null : id;
        await ApplyAudioAsync();
    }

    public async Task MuteAllAsync()
    {
        ActiveAudioId = null;
        await ApplyAudioAsync();
    }

    public async Task SelectByPositionAsync(int n)
    {
        var t = ByPosition.ElementAtOrDefault(n - 1);
        if (t is not null) await ToggleAudioAsync(t.Id);
    }

    public async Task SetVolumeAsync(int volume)
    {
        Settings.Volume = Math.Clamp(volume, 0, 100);
        if (ActiveAudioId is not null) await _player.SetVolumeAsync(ActiveAudioId, Settings.Volume);
        Save();
    }

    async Task ApplyAudioAsync()
    {
        Notify();
        foreach (var t in Tiles.Where(t => t.Id != ActiveAudioId).ToList())
            await _player.SetMutedAsync(t.Id, true);
        if (ActiveAudioId is not null)
        {
            await _player.SetVolumeAsync(ActiveAudioId, Settings.Volume);
            await _player.SetMutedAsync(ActiveAudioId, false);
        }
    }

    /// <summary>player.js 每 2 秒檢查發現實際靜音狀態與 C# 不一致（例如控制模式下用 YouTube 控制列取消靜音）</summary>
    public async Task OnActualMuteChangedAsync(string id, bool actuallyMuted)
    {
        if (!actuallyMuted) { ActiveAudioId = id; }
        else if (ActiveAudioId == id) { ActiveAudioId = null; }
        else return;
        await ApplyAudioAsync();
    }

    public async Task ReloadAsync(string id)
    {
        SetStatus(id, TileStatus.Loading);
        await _player.ReloadAsync(id);
    }

    // ───────── 設定 / 成員名單 ─────────

    public void UpdateSettings(AppSettings s)
    {
        Settings = s;
        Save(); Notify();
        OnSettingsChanged?.Invoke();
    }

    public void SaveMembers(List<MemberList> members)
    {
        Members = members; Save(); Notify();
    }

    // ───────── 分享 ─────────

    public string BuildShareUrl()
    {
        var ordered = ByPosition.ToList();
        var parts = new List<string>();
        void Add(string key, TileSource src)
        {
            var ids = ordered.Where(t => t.Source == src).Select(t => Uri.EscapeDataString(t.Ref)).ToList();
            if (ids.Count > 0) parts.Add($"{key}={string.Join(',', ids)}");
        }
        Add("v", TileSource.YouTube);
        Add("ch", TileSource.YouTubeChannel);
        Add("tw", TileSource.Twitch);
        parts.Add("layout=" + Layout.Id);
        return _nav.BaseUri + "?" + string.Join('&', parts);
    }

    // ───────── Holodex 資料套用（只影響標題列） ─────────

    public void ApplyLive(IReadOnlyList<HolodexVideo> videos)
    {
        var changed = false;
        foreach (var t in Tiles)
        {
            HolodexVideo? v = t.Source switch
            {
                TileSource.YouTube => videos.FirstOrDefault(x => x.Id == t.Ref),
                TileSource.YouTubeChannel => videos.FirstOrDefault(x => x.Channel?.Id == t.Ref && x.Type != "placeholder"),
                _ => videos.FirstOrDefault(x => TwitchName(x) == t.Ref)
            };
            var name = v?.Channel?.EnglishName ?? v?.Channel?.Name ?? t.ChannelName;
            var viewers = v?.LiveViewers;
            if (name != t.ChannelName || viewers != t.Viewers)
            {
                t.ChannelName = name; t.Viewers = viewers; changed = true;
            }
        }
        if (changed) { Save(); Notify(); }
    }

    /// <summary>placeholder 項目若 link 指向 twitch.tv，回傳頻道名稱</summary>
    public static string? TwitchName(HolodexVideo v) =>
        v.Type == "placeholder" && UrlParser.Parse(v.Link) is { Source: TileSource.Twitch } p ? p.Ref : null;
}
