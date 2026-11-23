using System.Net.Http.Json;
using System.Text.Json;
using StreamCCTV.Models;

namespace StreamCCTV.Services;

public class HolodexService : IHolodexService
{
    static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    readonly HttpClient _http;
    public HolodexService(HttpClient http) => _http = http;

    static string Url(string org) =>
        $"https://holodex.net/api/v2/live?org={Uri.EscapeDataString(org)}&status=live";

    HttpRequestMessage Req(string apiKey, string org)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, Url(org));
        req.Headers.Add("X-APIKEY", apiKey);
        return req;
    }

    public async Task<string> GetLiveRawAsync(string apiKey, string org, CancellationToken ct = default)
    {
        using var res = await _http.SendAsync(Req(apiKey, org), ct);
        var body = await res.Content.ReadAsStringAsync(ct);
        return $"HTTP {(int)res.StatusCode}\n{body}";
    }

    public async Task<HolodexResult> GetLiveAsync(string apiKey, string org, CancellationToken ct = default)
    {
        try
        {
            using var res = await _http.SendAsync(Req(apiKey, org), ct);
            if (!res.IsSuccessStatusCode)
                return res.StatusCode is System.Net.HttpStatusCode.Forbidden or System.Net.HttpStatusCode.Unauthorized
                    ? new(new(), "live.denied") : new(new(), "live.http", ((int)res.StatusCode).ToString());
            var list = await res.Content.ReadFromJsonAsync<List<HolodexVideo>>(Json, ct) ?? new();
            return new(list, null);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception e) { return new(new(), "live.conn", e.Message); }
    }
}
