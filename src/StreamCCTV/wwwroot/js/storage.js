// localStorage 與少量瀏覽器輔助功能。每個函式都自行 try/catch。
export function get(key) {
    try { return window.localStorage.getItem(key); } catch { return null; }
}

export function set(key, value) {
    try { window.localStorage.setItem(key, value); } catch { /* 隱私模式或空間已滿 */ }
}

export function remove(key) {
    try { window.localStorage.removeItem(key); } catch { }
}

export async function copyText(text) {
    try {
        await navigator.clipboard.writeText(text);
        return true;
    } catch {
        try {
            const ta = document.createElement('textarea');
            ta.value = text;
            ta.style.cssText = 'position:fixed;opacity:0';
            document.body.appendChild(ta);
            ta.select();
            const ok = document.execCommand('copy');
            ta.remove();
            return ok;
        } catch { return false; }
    }
}
