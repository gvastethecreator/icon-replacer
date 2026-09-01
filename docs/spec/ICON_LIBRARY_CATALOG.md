# Icon Library Catalog

Status: living contract
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

The current collection-management proof is:

```powershell
dotnet run --no-build --project src\IconReplacer.Cli -- import-picker-request [collection]
dotnet run --no-build --project src\IconReplacer.Cli -- collections
dotnet run --no-build --project src\IconReplacer.Cli -- collection-create <name>
dotnet run --no-build --project src\IconReplacer.Cli -- collection-import <collection> <icon.ico> [icon2.ico ...]
```

## Discovery Rules

- Read local `.ico` files.
- Treat one-level subfolders as Icon Categories.
- Create one-level subfolders through a sanitized collection-create path when driven by app UI.
- Import-picker requests are multi-select `.ico` requests rooted at the Icon Library.
- Import selected local `.ico` files into a one-level collection through the collection-import path when the user is managing the library.
- Ignore deeper nesting for context-menu display in V1.
- Do not follow reparse points or symlinks.
- Keep catalog scans bounded.

## Context Menu Rules

- Empty catalog shows useful actions instead of a dead submenu.
- Large catalogs are capped and route overflow to the WinUI app.
- Category names and icon names must remain readable after sanitization.
- Menu snapshots are rebuilt from the current `.icons` contents so new folders and icons appear on the next open/refresh without a manual rebuild.
- The first menu command is `Change icon...`; category entries are generated from one-level Icon Library folders.
- Shell command descriptors use stable command ids and bounded argument templates derived from the same menu snapshot.
- Shell command descriptors degrade to `Change icon...` plus `open-app` when the catalog is empty or unavailable.

## Current Menu Snapshot Contract

`IconMenuService` exposes:

- total and visible icon counts,
- root icons,
- non-empty one-level categories,
- per-category icon entries,
- catalog warnings,
- explicit menu state: ready, empty, truncated, warning, or unavailable,
- status and recommended-action text for WinUI/shell fallback surfaces,
- omitted icon counts when menu limits hide overflow.

The current CLI preview is:

```powershell
dotnet run --no-build --project src\IconReplacer.Cli -- menu
dotnet run --no-build --project src\IconReplacer.Cli -- menu-commands
dotnet run --no-build --project src\IconReplacer.Cli -- menu-invoke-preview <command-id> [target]
```

`menu-commands` exposes the future shell-facing command list:

- `change-icon` opens the picker flow for the selected target,
- `icon:<hash>` commands apply current Icon Library entries through `menu-apply`,
- `open-app` appears when the menu snapshot is truncated, empty, or unavailable so overflow and recovery route to the app.

`menu-invoke-preview` resolves one current command descriptor against an optional selected target. Target-required commands return final resolved arguments only when the target is a supported local folder or `.lnk`; unsupported selections return disabled reasons without mutation.

The direct submenu apply proof is:

```powershell
dotnet run --no-build --project src\IconReplacer.Cli -- menu-apply <target> <icon-from-library.ico>
dotnet run --no-build --project src\IconReplacer.Cli -- activate menu-apply <target> <icon-from-library.ico>
dotnet run --no-build --project src\IconReplacer.Cli -- activate-menu-apply <target> <icon-from-library.ico>
```

`menu-apply` rejects icons outside the current Icon Library catalog and then delegates mutation to the shared change workflow. `activate menu-apply` previews the same direct submenu activation shape for the packaged app without mutation, and `activate-menu-apply` applies that activation through the same validated path.
