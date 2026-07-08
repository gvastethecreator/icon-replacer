# Shell Integration

Status: proposed
Date: 2026-07-07

## Current Decision

Modern Shell Integration is the accepted V1 product direction. Classic Shell Integration remains documented as a prototype or fallback path only.

## Modern Shell Integration

Expected implementation:

- packaged app with package identity,
- `windows.comServer` manifest registration,
- `windows.fileExplorerContextMenus` manifest registration,
- native `IExplorerCommand` command extension,
- registration for folders and `.lnk`,
- `Change icon...` as the first command,
- one-level Icon Library categories from the AppModel menu snapshot when feasible.

Done means the command appears in the Windows 11 context menu path and works for folder and `.lnk` targets.

## Classic Shell Integration

Expected implementation:

- per-user `HKCU\Software\Classes\Directory\shell\IconReplacer\command`,
- per-user `HKCU\Software\Classes\lnkfile\shell\IconReplacer\command`,
- quoted command path,
- launches CLI/app picker path,
- documented as classic/fallback behavior.

Done means exported registry keys show only Icon Replacer-owned entries and the command works from `Show more options` or the classic context menu path.

## Shared Requirements

- No admin required for normal use.
- No heavy work on Explorer UI paths.
- No deep catalog scan on every menu open without bounds; use `IconMenuService` limits.
- Clear unsupported-target behavior.
- Install/uninstall proof required.
