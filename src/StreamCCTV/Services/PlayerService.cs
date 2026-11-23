using Microsoft.JSInterop;
using StreamCCTV.Models;

namespace StreamCCTV.Services;

/// <summary>包裝 player.js。所有呼叫失敗都吞掉（播放器問題不應讓整個 UI 掛掉）。</summary>
public class PlayerService : IAsyncDisposable
{
    readonly IJSRuntime _js;
    Task<IJSObjectReference>? _module;

    public PlayerService(IJSRuntime js) => _js = js;

    Task<IJSObjectReference> Module => _module ??= _js.InvokeAsync<IJSObjectReference>("import", "./js/player.js").AsTask();

    async Task Call(string fn, params object?[] args)
    {
        try { await (await Module).InvokeVoidAsync(fn, args); }
        catch (Exception e) when (e is JSException or JSDisconnectedException or InvalidOperationException or TaskCanceledException)
        { Console.WriteLine($"player.{fn}: {e.Message}"); }
    }

    public Task CreateAsync<T>(Tile tile, Microsoft.AspNetCore.Components.ElementReference host, DotNetObjectReference<T> callback) where T : class =>
        Call("create", tile.Id, host, tile.JsSource, tile.Ref, callback);

    public Task SetMutedAsync(string id, bool muted) => Call("setMuted", id, muted);
    public Task SetVolumeAsync(string id, int volume) => Call("setVolume", id, volume);
    public Task ReloadAsync(string id) => Call("reload", id);
    public Task DestroyAsync(string id) => Call("destroy", id);

    public async ValueTask DisposeAsync()
    {
        try { if (_module is { IsCompletedSuccessfully: true }) await (await _module).DisposeAsync(); } catch { }
    }
}
