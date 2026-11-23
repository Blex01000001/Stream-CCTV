// YouTube IFrame API / Twitch Embed 播放器管理。
// iframe 全部由這裡建立在 Blazor 不管理內容的 host div 裡；Blazor 重繪不會碰它們。
const players = new Map();   // tileId -> state
let ytPromise = null;
let twPromise = null;
let timer = null;

function loadScript(src) {
    return new Promise((resolve, reject) => {
        const s = document.createElement('script');
        s.src = src;
        s.async = true;
        s.onload = () => resolve();
        s.onerror = () => reject(new Error('載入失敗：' + src));
        document.head.appendChild(s);
    });
}

function loadYT() {
    if (window.YT && window.YT.Player) return Promise.resolve();
    if (!ytPromise) {
        ytPromise = new Promise((resolve, reject) => {
            const prev = window.onYouTubeIframeAPIReady;
            window.onYouTubeIframeAPIReady = () => { try { prev && prev(); } catch { } resolve(); };
            loadScript('https://www.youtube.com/iframe_api').catch((e) => { ytPromise = null; reject(e); });
        });
    }
    return ytPromise;
}

function loadTwitch() {
    if (window.Twitch && window.Twitch.Player) return Promise.resolve();
    if (!twPromise) {
        twPromise = loadScript('https://embed.twitch.tv/embed/v1.js').catch((e) => { twPromise = null; throw e; });
    }
    return twPromise;
}

function emit(p, kind, data) {
    try {
        p.dotnet.invokeMethodAsync('OnPlayerEvent', kind, data == null ? '' : String(data)).catch(() => { });
    } catch { }
}

function applyAudio(p) {
    if (!p.ready || !p.api) return;
    p.lastSet = Date.now();
    try {
        if (p.kind === 'twitch') {
            p.api.setMuted(p.desiredMuted);
            if (!p.desiredMuted) p.api.setVolume(p.volume / 100);
        } else if (p.desiredMuted) {
            p.api.mute();
        } else {
            p.api.unMute();
            p.api.setVolume(p.volume);
        }
    } catch { }
}

async function build(p) {
    p.ready = false;
    p.api = null;
    p.host.innerHTML = '';
    const mount = document.createElement('div');
    mount.style.cssText = 'width:100%;height:100%';
    p.host.appendChild(mount);
    const gen = ++p.gen;
    try {
        if (p.kind === 'twitch') {
            await loadTwitch();
            if (gen !== p.gen) return;
            const player = new window.Twitch.Player(mount, {
                channel: p.ref,
                parent: [location.hostname],   // Twitch 要求 parent 為嵌入頁面的網域
                autoplay: true,
                muted: true,
                width: '100%',
                height: '100%'
            });
            p.api = player;
            const T = window.Twitch.Player;
            player.addEventListener(T.READY, () => { if (gen !== p.gen) return; p.ready = true; applyAudio(p); emit(p, 'ready'); });
            player.addEventListener(T.ONLINE, () => emit(p, 'online'));
            player.addEventListener(T.OFFLINE, () => emit(p, 'offline'));
            player.addEventListener(T.PLAYING, () => emit(p, 'playing'));
        } else {
            await loadYT();
            if (gen !== p.gen) return;
            const params = new URLSearchParams({
                autoplay: '1', mute: '1', enablejsapi: '1', playsinline: '1', rel: '0',
                origin: location.origin
            });
            const iframe = document.createElement('iframe');
            iframe.src = p.kind === 'ytchannel'
                ? `https://www.youtube.com/embed/live_stream?channel=${encodeURIComponent(p.ref)}&${params}`
                : `https://www.youtube.com/embed/${encodeURIComponent(p.ref)}?${params}`;
            iframe.allow = 'autoplay; encrypted-media; picture-in-picture; fullscreen';
            iframe.allowFullscreen = true;
            iframe.style.cssText = 'width:100%;height:100%;border:0;display:block';
            mount.appendChild(iframe);
            p.api = new window.YT.Player(iframe, {
                events: {
                    onReady: () => { if (gen !== p.gen) return; p.ready = true; applyAudio(p); emit(p, 'ready'); },
                    onError: (e) => emit(p, 'error', e.data),
                    onStateChange: (e) => { if (e.data === 1) emit(p, 'playing'); }
                }
            });
        }
    } catch (err) {
        emit(p, 'error', 'script');
    }
}

function isMuted(p) {
    return p.kind === 'twitch' ? p.api.getMuted() : p.api.isMuted();
}

function ensureTimer() {
    if (timer) return;
    // 每 2 秒檢查實際靜音狀態，與 C# 不一致就回報（例如控制模式下使用者按了 YouTube 的音量鍵）
    timer = setInterval(() => {
        for (const p of players.values()) {
            if (!p.ready || !p.api || Date.now() - p.lastSet < 2500) continue;
            try {
                const actual = !!isMuted(p);
                if (actual !== p.desiredMuted) {
                    p.desiredMuted = actual;
                    emit(p, 'muteMismatch', actual);
                }
            } catch { }
        }
    }, 2000);
}

export async function create(tileId, host, kind, ref, dotnet) {
    destroy(tileId);
    const p = { tileId, host, kind, ref, dotnet, api: null, ready: false, desiredMuted: true, volume: 60, lastSet: 0, gen: 0 };
    players.set(tileId, p);
    ensureTimer();
    await build(p);
}

export function setMuted(tileId, muted) {
    const p = players.get(tileId);
    if (!p) return;
    p.desiredMuted = !!muted;
    applyAudio(p);
}

export function setVolume(tileId, volume) {
    const p = players.get(tileId);
    if (!p) return;
    p.volume = volume;
    if (!p.desiredMuted) applyAudio(p);
}

export async function reload(tileId) {
    const p = players.get(tileId);
    if (!p) return;
    p.desiredMuted = true;
    try { p.api && p.api.destroy && p.api.destroy(); } catch { }
    await build(p);
}

export function destroy(tileId) {
    const p = players.get(tileId);
    if (!p) return;
    p.gen++;
    players.delete(tileId);
    try { p.api && p.api.destroy && p.api.destroy(); } catch { }
    try { p.host.innerHTML = ''; } catch { }
    if (players.size === 0 && timer) { clearInterval(timer); timer = null; }
}
