# Installation and Packaging

Status: proposed
Date: 2026-07-07

## Preferred Product Install

Use per-user packaged installation for the accepted Windows 11 product path.

Expected pieces:

- WinUI 3 app package,
- package identity,
- native COM shell extension registration,
- `windows.fileExplorerContextMenus` manifest registration,
- dev signing or trusted local signing path.

## Prototype/Fallback Install

If Classic Shell Integration is used as a prototype or fallback, use per-user registry keys under `HKCU\Software\Classes`.

Expected properties:

- no admin required,
- command paths quoted,
- only Icon Replacer-owned keys created,
- export `.reg` proof during QA,
- clear app status that this is classic integration.

## Uninstall Requirements

- Remove shell integration.
- Preserve `%USERPROFILE%\.icons` by default.
- Preserve or offer export of restore history unless user explicitly deletes app data.
- Never delete user-selected original files.
- Never remove unrelated registry keys or `desktop.ini` settings.

## Developer Prerequisites

For WinUI development, follow `winui-dev-workflow`.

If `dotnet`, WinUI templates, `winapp`, or Developer Mode are missing, stop and run the setup path rather than improvising workarounds.

Current preflight on 2026-07-07:

- .NET SDK 10.0.300 is available.
- WinUI templates are available.
- `winapp` was not found, so WinUI scaffolding/running should wait for `/winui-setup`.
- Shell integration decision is accepted as Modern MSIX plus native `IExplorerCommand`; Explorer registration is not configured yet.

## Launch Rule

Use `BuildAndRun.ps1` or `winapp run` for packaged app launch. Do not run the packaged `.exe` directly.
