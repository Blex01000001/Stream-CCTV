namespace StreamCCTV.Models;

/// <summary>布局定義：每個格位對應一個 grid-area（row-start / col-start / row-end / col-end）</summary>
public record LayoutDef(string Id, string Name, int Cols, int Rows, string[] Areas, bool Auto = false)
{
    public int Capacity => Areas.Length;
}

public static class Layouts
{
    static string[] Grid(int cols, int rows)
    {
        var list = new List<string>();
        for (var r = 1; r <= rows; r++)
            for (var c = 1; c <= cols; c++)
                list.Add($"{r} / {c} / {r + 1} / {c + 1}");
        return list.ToArray();
    }

    static LayoutDef G(int cols, int rows) => new($"{cols}x{rows}", $"{cols}×{rows}", cols, rows, Grid(cols, rows));

    public static readonly LayoutDef AutoPlaceholder = new("auto", "自動", 1, 1, Grid(1, 1), true);

    public static readonly IReadOnlyList<LayoutDef> All = new List<LayoutDef>
    {
        G(1, 1), G(2, 1), G(2, 2), G(3, 2), G(2, 3), G(3, 3), G(4, 4),
        new("1b3", "1 大 + 3 小", 4, 3, new[] { "1 / 1 / 4 / 4", "1 / 4 / 2 / 5", "2 / 4 / 3 / 5", "3 / 4 / 4 / 5" }),
        new("1b5", "1 大 + 5 小", 3, 3, new[] { "1 / 1 / 3 / 3", "1 / 3 / 2 / 4", "2 / 3 / 3 / 4", "3 / 1 / 4 / 2", "3 / 2 / 4 / 3", "3 / 3 / 4 / 4" }),
        AutoPlaceholder
    };

    public static LayoutDef? Find(string? id) => All.FirstOrDefault(l => l.Id == id);

    public static LayoutDef Get(string id) => Find(id) ?? All[2];

    /// <summary>依格數產生自動布局</summary>
    public static LayoutDef Auto(int count)
    {
        var n = Math.Max(1, count);
        var cols = (int)Math.Ceiling(Math.Sqrt(n));
        var rows = (int)Math.Ceiling(n / (double)cols);
        return new LayoutDef("auto", "自動", cols, rows, Grid(cols, rows), true);
    }
}
