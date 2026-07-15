# Installation and Packaging

Status: implemented; installed candidate under manual acceptance
Date: 2026-07-14

## Preferred Product Install

Use per-user packaged installation for the accepted Windows 11 product path.

Expected pieces:

- WinUI 3 app package,
- package identity,
- native COM shell extension registration,
- `windows.fileExplorerContextMenus` manifest registration,
- `windows.fileExplorerClassicContextMenuHandler` manifest registration,
- dev signing or trusted local signing path.

Current AppModel/CLI planning proof:

```powershell
dotnet run --no-build --project src\IconReplacer.Cli -- package-plan
```

`package-plan` is read-only. It reports the install mode, native build-tooling, package/signing/native-extension gates, install/uninstall proof gates, and the policy that uninstall removes shell integration while preserving `.icons` and restore history by default.

## Retired Registry Prototype

Do not install raw context-menu verbs under `HKCU\Software\Classes`. The
classic Explorer path is part of the same MSIX as the modern path. Registration
and removal are owned by package lifecycle only.

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
- Shell integration is implemented as one packaged modern/classic pair with a zero-window command host.
- The signed x64 MSIX builds successfully. Lifecycle evidence is bound to the exact package SHA-256, so rebuilding the package reopens install/uninstall proof.
- Signed development candidate `1.0.0.5` is installed. The approved guarded upgrade reloaded Explorer and preserved all 185 Icon Library files, restore state, and the normalized unrelated context-menu baseline.

## Launch Rule

Use `BuildAndRun.ps1` or `winapp run` for packaged app launch. Do not run the packaged `.exe` directly.

Current debug launch proof:

```powershell
.\BuildAndRun.ps1 src\IconReplacer.App\IconReplacer.App.csproj -Detach
```

Latest management-app proof launched AUMID
`IconReplacer_2wx6x5nenbha0!IconReplacer.App` with window title `Icon Replacer`.
The Explorer commands no longer use packaged-app activation: `Change icon...`
opens only the native picker and collection choices apply through
`IconReplacer.CommandHost`. Signing, dynamic enumeration, and native preview
smoke are green. The exact signed `1.0.0.5` package has current install and
preservation evidence under `artifacts\package`; uninstall proof remains open.
The remaining release gates are the clean uninstall transaction and the manual
Explorer matrix for every target, direct apply, restore, and failure state.

## Guarded Lifecycle

Use the lifecycle script for package proof. It snapshots all non-Icon-Replacer
context-menu registrations, tracks the real Explorer process, verifies
StartAllBack coexistence, and automatically removes Icon Replacer after a
post-mutation failure.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Test-MsixLifecycle.ps1 -SnapshotOnly
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Test-MsixLifecycle.ps1 -UpgradeInstalledPackage -KeepInstalled -ApproveExplorerRegistration
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Test-MsixLifecycle.ps1 -ApproveExplorerRegistration
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Test-MsixLifecycle.ps1 -InteractiveProof -ApproveExplorerRegistration
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Test-MsixLifecycle.ps1 -RecoverInterruptedProof -ApproveExplorerRegistration
```

`-SnapshotOnly` records the complete normalized values and subkeys for every
unrelated handler without installing or removing a package. It also records the
candidate identity, signature and hash, installed package identity, and whether
the candidate is a fresh install, a newer signed upgrade, or blocked.

Every mutating run refuses to start unless `-ApproveExplorerRegistration` is
present. Use that switch only after the user has explicitly approved the
registration run. By default it refuses to replace an already installed Icon
Replacer package. The only exception is
`-UpgradeInstalledPackage -KeepInstalled`, which additionally requires a valid
signature, matching package identity, exactly one installed package, and a
strictly newer candidate version. This path lets Windows update the package
without a pre-install uninstall and leaves it installed for current-candidate
UIA and Explorer proof.
`-InteractiveProof` creates a timestamped session under
`artifacts\explorer-proof`, keeps the package installed while the checklist is
performed, then removes it and verifies the context-menu, Explorer process,
`.icons`, and restore-state baselines before returning. Because the manual
apply/restore matrix legitimately writes restore history, the runner captures
the original `state.json` content in memory and in a current-user
DPAPI-protected recovery file. It restores history after the app is stopped and
removes the protected file only after cleanup passes; the Icon Library remains
read-only throughout the proof. `-RecoverInterruptedProof` resumes cleanup from
the newest incomplete session without reinstalling the package. A specific
session can be selected with `-RecoverySessionPath <session.json>`.

For a clean-baseline release matrix, do not use `-KeepInstalled`; the
transactional `-InteractiveProof` path owns cleanup. The guarded upgrade path is
the deliberate exception: it requires `-KeepInstalled` so the approved update
is not silently converted into an uninstall proof.
