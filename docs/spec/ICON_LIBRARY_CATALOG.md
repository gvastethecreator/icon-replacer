# Icon Library Catalog

Status: proposed
Date: 2026-07-07

## Location

The Icon Library is `%USERPROFILE%\.icons`.

## Import Rules

- Create the library on first run.
- Copy external selected icons to `.icons\Imported`.
- Dedupe by content hash before honoring a requested display name; the same icon bytes must resolve to one imported file even when the user supplies a different display name.
- Sanitize display names and file names.
- Never depend on the original selected external icon path after import.

The current CLI import proof is:

```powershell
dotnet run --no-build --project src\IconReplacer.Cli -- import <icon.ico> [display-name]
```

## Discovery Rules

- Read local `.ico` files.
- Treat one-level subfolders as Icon Categories.
- Ignore deeper nesting for context-menu display in V1.
- Do not follow reparse points or symlinks.
- Keep catalog scans bounded.

## Context Menu Rules

- Empty catalog shows useful actions instead of a dead submenu.
- Large catalogs are capped and route overflow to the WinUI app.
- Category names and icon names must remain readable after sanitization.
- Menu snapshots are rebuilt from the current `.icons` contents so new folders and icons appear on the next open/refresh without a manual rebuild.
- The first menu command is `Change icon...`; category entries are generated from one-level Icon Library folders.

## Current Menu Snapshot Contract

`IconMenuService` exposes:

- total and visible icon counts,
- root icons,
- non-empty one-level categories,
- per-category icon entries,
- catalog warnings,
- omitted icon counts when menu limits hide overflow.

The current CLI preview is:

```powershell
dotnet run --no-build --project src\IconReplacer.Cli -- menu
```
