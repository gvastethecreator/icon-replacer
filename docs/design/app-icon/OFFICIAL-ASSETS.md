# Official App Icon Assets

Status: accepted
Date: 2026-07-14

## Canonical Sources

- `assets/icon.png`, 1587x1461, SHA-256 `B09AABDB6B370985C6463F2A93CBDB08AF47919AB9D4715FBC7550283B331A8F`.

This file is the official visual identity. Earlier files in this folder
remain historical design evidence and are not production sources.

## Generated Contract

Run `scripts/Build-AppIconAssets.ps1` after the canonical PNG changes. The
deterministic Pillow pipeline produces:

- a centered transparent square canvas that preserves the source proportions,
- `AppIcon.ico` with 16, 20, 24, 32, 40, 48, 64, 128, and 256 pixel frames,
- `AppIcon.png` at 512x512 for crisp WinUI
  title-bar and About rendering,
- MSIX square, target-size, Store, lock-screen, splash, and wide assets,
- identical `altform-unplated` and `altform-lightunplated` derivatives so all
  Windows surfaces preserve the same visual identity.

Generated files under `src/IconReplacer.App/Assets` must not be edited by hand.

## Runtime Routing

- WinUI uses the same 512x512 PNG in every theme for the custom title bar and
  About view. `AppWindow` and Explorer use the same multi-frame ICO.
- User-selected collection icons remain unchanged and continue to use their own
  `.ico` previews.
