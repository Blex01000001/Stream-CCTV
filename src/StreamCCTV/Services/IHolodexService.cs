using StreamCCTV.Models;

namespace StreamCCTV.Services;

/// <summary>Holodex 存取介面。目前直接由瀏覽器呼叫；之後可換成呼叫自家後端代理的實作。</summary>
public interface IHolodexService
{
    Task<HolodexResult> GetLiveAsync(string apiKey, string org, CancellationToken ct = default);

    /// <summary>探勘用：回傳原始 JSON 字串</summary>
    Task<string> GetLiveRawAsync(string apiKey, string org, CancellationToken ct = default);
}
