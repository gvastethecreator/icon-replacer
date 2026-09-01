# Installation and packaging

Status: implemented. The installed candidate still needs manual acceptance.

## Preferred product install

Use per-user packaged installation for the accepted Windows 11 product path.

Expected pieces:

- WinUI 3 app package
- package identity
- native COM shell extension registration
- `windows.fileExplorerContextMenus` manifest registration
- `windows.fileExplorerClassicContextMenuHandler` manifest registration
- development signing or a trusted local signing path

Current AppModel/CLI planning proof:

```powershell
dotnet run --no-build --project src\IconReplacer.Cli -- package-plan
```

`package-plan` is read-only. It reports the install mode, native build-tooling, package/signing/native-extension gates, install/uninstall proof gates, and the policy that uninstall removes shell integration while preserving `.icons` and restore history by default.

## Retired registry prototype

Do not install raw context-menu verbs under `HKCU\Software\Classes`. The classic Explorer path is part of the same MSIX as the modern path. Registration and removal are owned by package lifecycle only.

## Uninstall requirements

- Remove shell integration.
- Preserve `%USERPROFILE%\.icons` by default.
- Preserve or offer export of restore history unless the user explicitly deletes app data.
- Never delete user-selected original files.
- Never remove unrelated registry keys or `desktop.ini` settings.

## Developer prerequisites

For WinUI development, follow `winui-dev-workflow`.

If `dotnet`, WinUI templates, `winapp`, Developer Mode, or native C++ build tools are missing, stop and run the setup path rather than improvising workarounds.

Typical local setup:

- .NET 10 SDK
- WinUI templates
- `winapp` CLI (`winapp --version` to confirm)
- Visual Studio 2022 Build Tools with the Windows SDK and C++ workload
- CMake

If `cl.exe` or Visual Studio MSBuild are not on PATH in a normal shell, run `scripts\Initialize-NativeToolchain.ps1 -PassThru` before native shell-extension work.

```powershell
.\scripts\Build-NativeShellExtension.ps1 -Configuration Debug -Platform x64
```

That script builds `IconReplacer.ShellExtension.dll`. Shell integration is one packaged modern/classic pair with a zero-window command host.

The signed x64 MSIX builds successfully. Lifecycle evidence is bound to the exact package SHA-256, so rebuilding the package reopens install/uninstall proof.

## Launch rule

Use `BuildAndRun.ps1` or `winapp run` for packaged app launch. Do not run the packaged `.exe` directly.

```powershell
.\BuildAndRun.ps1 src\IconReplacer.App\IconReplacer.App.csproj -Detach
```

Explorer commands do not use packaged-app activation. **Change icon...** opens only the native picker. Collection choices apply through `IconReplacer.CommandHost`. Remaining release gates are the clean uninstall transaction and the manual Explorer matrix for every target, direct apply, restore, and failure state.

## Guarded lifecycle

Use the lifecycle script for package proof. It snapshots all non-Icon-Replacer context-menu registrations, tracks the real Explorer process, verifies StartAllBack coexistence, and automatically removes Icon Replacer after a post-mutation failure.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Test-MsixLifecycle.ps1 -SnapshotOnly
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Test-MsixLifecycle.ps1 -UpgradeInstalledPackage -KeepInstalled -ApproveExplorerRegistration
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Test-MsixLifecycle.ps1 -ApproveExplorerRegistration
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Test-MsixLifecycle.ps1 -InteractiveProof -ApproveExplorerRegistration
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Test-MsixLifecycle.ps1 -RecoverInterruptedProof -ApproveExplorerRegistration
```

`-SnapshotOnly` records the complete normalized values and subkeys for every unrelated handler without installing or removing a package. It also records the candidate identity, signature and hash, installed package identity, and whether the candidate is a fresh install, a newer signed upgrade, or blocked.

Every mutating run refuses to start unless `-ApproveExplorerRegistration` is present. Use that switch only after the user has explicitly approved the registration run. By default it refuses to replace an already installed Icon Replacer package. The only exception is `-UpgradeInstalledPackage -KeepInstalled`, which additionally requires a valid signature, matching package identity, exactly one installed package, and a strictly newer candidate version. This path lets Windows update the package without a pre-install uninstall and leaves it installed for current-candidate UIA and Explorer proof.

`-InteractiveProof` creates a timestamped session under `artifacts\explorer-proof`, keeps the package installed while the checklist is performed, then removes it and verifies the context-menu, Explorer process, `.icons`, and restore-state baselines before returning. Because the manual apply/restore matrix legitimately writes restore history, the runner captures the original `state.json` content in memory and in a current-user DPAPI-protected recovery file. It restores history after the app is stopped and removes the protected file only after cleanup passes. The Icon Library remains read-only throughout the proof.

`-RecoverInterruptedProof` resumes cleanup from the newest incomplete session without reinstalling the package. A specific session can be selected with `-RecoverySessionPath <session.json>`.

For a clean-baseline release matrix, do not use `-KeepInstalled`. The transactional `-InteractiveProof` path owns cleanup. The guarded upgrade path is the deliberate exception: it requires `-KeepInstalled` so the approved update is not silently converted into an uninstall proof.