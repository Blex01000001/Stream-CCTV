// 全域快捷鍵、輸入框 Enter 送出、全螢幕
let handler = null;
const HANDLED = new Set(['m', 'c', 'f', 'h', '?', 'Escape']);

function isTyping(el) {
    if (!el) return false;
    const tag = el.tagName;
    return tag === 'INPUT' || tag === 'TEXTAREA' || tag === 'SELECT' || el.isContentEditable;
}

export function init(dotnet) {
    dispose();
    handler = (e) => {
        if (isTyping(e.target) || e.ctrlKey || e.metaKey || e.altKey) return;
        let key = e.key;
        if (key.length === 1) key = key.toLowerCase();
        if (!(HANDLED.has(key) || (key.length === 1 && key >= '1' && key <= '9'))) return;
        if (key !== 'Escape') e.preventDefault();
        dotnet.invokeMethodAsync('OnKey', key).catch(() => { });
    };
    window.addEventListener('keydown', handler);
}

export function dispose() {
    if (handler) window.removeEventListener('keydown', handler);
    handler = null;
}

export function toggleFullscreen() {
    if (document.fullscreenElement) document.exitFullscreen().catch(() => { });
    else document.documentElement.requestFullscreen().catch(() => { });
}

// 輸入框：Enter 送出（Shift+Enter 換行；輸入法組字中不送出）
export function bindSubmit(el, dotnet) {
    el.addEventListener('keydown', (e) => {
        if (e.key === 'Enter' && !e.shiftKey && !e.isComposing && e.keyCode !== 229) {
            e.preventDefault();
            dotnet.invokeMethodAsync('Submit').catch(() => { });
        }
    });
}

export function takeValue(el) {
    const v = el.value;
    el.value = '';
    return v;
}

export function setValue(el, v) { el.value = v; }

export function focus(el) { el.focus(); el.scrollIntoView?.({ block: 'nearest' }); }
