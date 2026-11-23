// 以 Pointer Events 實作的標題列拖曳交換。不搬動任何 DOM，只在放開時回呼 C#。
let dotnet = null;
let st = null;
let ghost = null;

const THRESHOLD = 6;

function clearTarget() {
    document.querySelectorAll('.drop-target').forEach(e => e.classList.remove('drop-target'));
}

function onDown(e) {
    if (e.button !== 0 || st) return;
    const bar = e.target.closest('[data-drag]');
    if (!bar || e.target.closest('button,a,input,select')) return;
    const tile = bar.closest('[data-tile-id]');
    if (!tile) return;
    st = { id: tile.dataset.tileId, name: tile.dataset.name || '', tile, x: e.clientX, y: e.clientY, active: false, target: null };
    window.addEventListener('pointermove', onMove);
    window.addEventListener('pointerup', onUp);
    window.addEventListener('pointercancel', onCancel);
}

function begin() {
    st.active = true;
    document.body.classList.add('dragging');           // CSS：所有 iframe 與遮罩 pointer-events:none
    st.tile.classList.add('drag-source');
    ghost = document.createElement('div');
    ghost.className = 'drag-ghost';
    ghost.textContent = st.name;
    document.body.appendChild(ghost);
}

function onMove(e) {
    if (!st) return;
    if (!st.active) {
        if (Math.hypot(e.clientX - st.x, e.clientY - st.y) < THRESHOLD) return;
        begin();
    }
    ghost.style.left = (e.clientX + 12) + 'px';
    ghost.style.top = (e.clientY + 12) + 'px';
    clearTarget();
    const el = document.elementFromPoint(e.clientX, e.clientY);
    const target = el && el.closest('[data-tile-id],[data-slot]');
    st.target = target && target !== st.tile ? target : null;
    if (st.target) st.target.classList.add('drop-target');
}

function finish() {
    window.removeEventListener('pointermove', onMove);
    window.removeEventListener('pointerup', onUp);
    window.removeEventListener('pointercancel', onCancel);
    clearTarget();
    document.body.classList.remove('dragging');
    st?.tile.classList.remove('drag-source');
    ghost?.remove(); ghost = null;
}

function onUp() {
    if (!st) return;
    const s = st;
    finish();
    st = null;
    if (!s.active) return;
    // 拖曳結束後瀏覽器仍會送出 click，吃掉它避免誤觸「切音源」
    const swallow = (ev) => { ev.stopPropagation(); ev.preventDefault(); };
    window.addEventListener('click', swallow, true);
    setTimeout(() => window.removeEventListener('click', swallow, true), 60);
    if (!s.target || !dotnet) return;
    if (s.target.dataset.tileId) dotnet.invokeMethodAsync('SwapTiles', s.id, s.target.dataset.tileId).catch(() => { });
    else if (s.target.dataset.slot) dotnet.invokeMethodAsync('MoveToSlot', s.id, parseInt(s.target.dataset.slot, 10)).catch(() => { });
}

function onCancel() {
    finish();
    st = null;
}

export function init(ref) {
    dispose();
    dotnet = ref;
    window.addEventListener('pointerdown', onDown, true);
}

export function dispose() {
    window.removeEventListener('pointerdown', onDown, true);
    if (st) { finish(); st = null; }
    dotnet = null;
}
