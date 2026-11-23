using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.JSInterop;

namespace StreamCCTV.Services;

/// <summary>透過 JS Interop 存取 localStorage，所有呼叫都 try/catch（隱私模式等情況可能丟例外）</summary>
public class StorageService
{
    static readonly JsonSerializerOptions Json = new() { Converters = { new JsonStringEnumConverter() } };
    readonly IJSRuntime _js;
    Task<IJSObjectReference>? _module;

    public StorageService(IJSRuntime js) => _js = js;

    Task<IJSObjectReference> Module => _module ??= _js.InvokeAsync<IJSObjectReference>("import", "./js/storage.js").AsTask();

    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            var raw = await (await Module).InvokeAsync<string?>("get", "cctv." + key);
            return string.IsNullOrEmpty(raw) ? default : JsonSerializer.Deserialize<T>(raw, Json);
        }
        catch (Exception e) { Console.WriteLine($"storage get {key}: {e.Message}"); return default; }
    }

    public async Task SetAsync<T>(string key, T value)
    {
        try { await (await Module).InvokeVoidAsync("set", "cctv." + key, JsonSerializer.Serialize(value, Json)); }
        catch (Exception e) { Console.WriteLine($"storage set {key}: {e.Message}"); }
    }

    public async Task<bool> CopyToClipboardAsync(string text)
    {
        try { return await (await Module).InvokeAsync<bool>("copyText", text); }
        catch { return false; }
    }
}
