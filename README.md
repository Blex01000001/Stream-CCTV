# Stream-CCTV — Live Stream Multiview Wall

**Live demo:** https://blex01000001.github.io/Stream-CCTV/

Stream-CCTV is a browser-based "CCTV wall" for watching many live streams at once. Paste YouTube or Twitch links, arrange them on a grid, and listen to one stream at a time. It is built for people who follow several streamers or events simultaneously (for example, a group of VTubers playing the same game) and want everything on a single screen.

It is a static Blazor WebAssembly (.NET 8) app: no backend, no login, no tracking. Everything is stored in your own browser.

## Features

- **Add streams** from YouTube (watch / youtu.be / live / embed URLs, 11-character video IDs, `UC…` channel IDs for "the channel's current live stream") and Twitch (`twitch.tv/<name>`). Paste several lines at once.
- **Exclusive audio:** every stream starts muted and autoplays; click a tile to hear only that one. Keys `1`–`9` pick a tile by screen position, `M` mutes everything.
- **Layouts:** 1×1, 2×1, 2×2, 3×2, 2×3, 3×3, 4×4, 1 big + 3 small, 1 big + 5 small, and auto. One-click **4 / 6 / 9 split** buttons are in the toolbar.
- **Drag to swap** tiles by their title bar, **double-click** a title bar to zoom a tile. Swapping, re-layout and zooming never reload the streams.
- **Control mode** (`C` or the 🎛 button) lets you use the YouTube player controls (quality, captions, …).
- **Member lists:** save named lists of channels and load them all with one click.
- **Share links:** copy a URL such as `?v=id1,id2&tw=name&layout=2x3` that rebuilds your wall.
- **Holodex live bar (optional):** add a [Holodex](https://holodex.net) API key in Settings to see who is live (default filter: "Among Us"), see viewer counts on each tile and add streams with one click. The key stays in your browser's localStorage.
- **English / 繁體中文** interface (globe button, top right). The language you used last is remembered.
- Your tiles, layout, member lists and settings are restored on your next visit (streams always start muted).

## Keyboard shortcuts

| Key | Action |
| --- | --- |
| `1`–`9` | Switch audio to tile N |
| `M` | Mute all |
| `C` | Global control mode |
| `F` | Fullscreen |
| `H` | Hide / show toolbar and title bars |
| `Esc` | Un-zoom / close dialogs |
| `?` | Show shortcuts |

## Run locally

```bash
cd src/StreamCCTV
dotnet run
```

Requires the .NET 8 SDK.

## Deploy to GitHub Pages

Pushing to `main` runs `.github/workflows/deploy.yml`. In the repository settings, set **Pages → Source** to **GitHub Actions**. The workflow publishes the app, rewrites `<base href>` to `/<repo-name>/`, copies `index.html` to `404.html` (SPA fallback) and adds `.nojekyll`.

## Notes

- Player iframes are created by `wwwroot/js/player.js` inside containers Blazor never touches, which is why tiles are not reloaded when the UI re-renders.
- Holodex access sits behind `IHolodexService`, so it can later be replaced by a backend proxy implementation.
- Settings → *Probe Holodex* shows the raw `/api/v2/live` JSON, handy for checking the DTOs in `Models/Holodex.cs`.
