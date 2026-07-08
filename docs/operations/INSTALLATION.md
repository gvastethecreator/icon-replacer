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

Current AppModel/CLI planning proof:

```powershell
dotnet run --no-build --project src\IconReplacer.Cli -- package-plan
```

`package-plan` is read-only. It reports the install mode, native build-tooling, package/signing/native-extension gates, install/uninstall proof gates, and the policy that uninstall removes shell integration while preserving `.icons` and restore history by default.

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

If `dotnet`, WinUI templates, `winapp`, Developer Mode, or native C++ build tools are missing, stop and run the setup path rather than improvising workarounds.

Current preflight on 2026-07-08:

- .NET SDK 10.0.300 is available.
- WinUI templates are available and refreshed.
- `winapp` 0.4.0 is available after `/winui-setup`.
- `winget upgrade --id Microsoft.WinAppCLI --exact` currently reports no matching upgrade package, so verify the installed `winapp --version` directly.
- Visual Studio Build Tools are installed. If `cl.exe` or Visual Studio MSBuild are not on PATH in a normal shell, run `scripts\Initialize-NativeToolchain.ps1 -PassThru` before native shell-extension work.
- CMake is available.
- `scripts\Build-NativeShellExtension.ps1 -Configuration Debug -Platform x64` builds `IconReplacer.ShellExtension.dll`.
- Shell integration decision is accepted as Modern MSIX plus native `IExplorerCommand`; Explorer registration is not configured yet.

## Launch Rule

Use `BuildAndRun.ps1` or `winapp run` for packaged app launch. Do not run the packaged `.exe` directly.

Current debug launch proof:

```powershell
.\BuildAndRun.ps1 src\IconReplacer.App\IconReplacer.App.csproj -Detach
```

Latest proof launched AUMID `IconReplacer_2wx6x5nenbha0!IconReplacer.App` with window title `Icon Replacer`. Pending `change-icon` activation also opens a modal `Change icon` file dialog in the packaged app. This is debug launch proof only; release install, signing, uninstall, dynamic submenu, and Explorer registration proof remain open.
