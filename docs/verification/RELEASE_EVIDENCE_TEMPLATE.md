# Release evidence

Copy this file for each candidate. Keep credentials, private certificate material, proprietary icons, and sensitive filesystem screenshots outside the repository.

Release: `[tag or package version]`
Date: `[YYYY-MM-DD]`
Windows build: `[edition, version, build]`
Integration path: Packaged Windows 11 + classic Explorer menus
Candidate SHA-256: `[hash]`
Candidate installed: `[yes/no; package full name]`

## Commands

```powershell
dotnet build IconReplacer.slnx -c Release
dotnet test IconReplacer.slnx -c Release --no-build
dotnet run --no-build -c Release --project src\IconReplacer.Cli -- package-plan
.\BuildAndRun.ps1 src\IconReplacer.App\IconReplacer.App.csproj -SkipRun /p:Platform=x64 /p:Configuration=Release /p:BuildNativeShellExtension=false
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Build-NativeShellExtension.ps1 -Configuration Release -Platform x64
artifacts\native\x64\Release\IconReplacer.ShellExtension.Smoke.exe artifacts\native\x64\Release\IconReplacer.ShellExtension.dll
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Test-MsixLifecycle.ps1 -SnapshotOnly
# Approval required for the current installed-candidate upgrade:
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Test-MsixLifecycle.ps1 -UpgradeInstalledPackage -KeepInstalled -ApproveExplorerRegistration
powershell -NoProfile -ExecutionPolicy Bypass -File tests\ui\accessibility-ui.ps1 -AppPid <pid> -ArtifactDir artifacts\ui-tests\gallery-first\accessibility-100 -MinimumScalePercent 100
# Repeat on a 200% display with -MinimumScalePercent 200.
```

## Automated results

- Build: `[pass/fail]`
- Unit/integration tests: `[count passed/failed]`
- Native shell smoke: `[pass/fail; command count; query time; 8-id range]`
- Official icon assets: `[pass/fail]`
- Signed MSIX: `[version, size, signer, SHA-256]`
- Snapshot-only shell guard: `[unrelated-entry count and hash]`
- Guarded deployment: `[pass/fail]`
- Preservation: `[Icon Library, restore state, unrelated handlers]`

## Accessibility proof

- Gallery First contract: `[pass/fail]`
- Dynamic names: `[pass/fail]`
- Persistent announcements: `[pass/fail]`
- Keyboard and layout: `[pass/fail]`
- Current-candidate UIA run: `[scale and result]`
- 200% UIA run: `[pass/fail/pending]`
- High Contrast visual run: `[pass/fail/pending]`

## Manual Explorer proof

- Folder menu screenshot: `[pass/fail/pending]`
- `.lnk` menu screenshot: `[pass/fail/pending]`
- Picker launch: `[pass/fail/pending]`
- Folder before/after/restore: `[pass/fail/pending]`
- `.lnk` before/after/restore: `[pass/fail/pending]`
- Invalid icon: `[pass/fail/pending]`
- Permission failure: `[pass/fail/pending]`
- Empty catalog: `[pass/fail/pending]`
- Category catalog: `[pass/fail/pending]`

## Install/uninstall proof

- Package/manifest: `[path to signed MSIX and build evidence]`
- Install log: `[pass/fail/pending]`
- Uninstall log: `[pass/fail/pending]`
- Post-uninstall shell integration check: `[pass/fail/pending]`
- `%USERPROFILE%\.icons` preserved: `[pass/fail/pending]`

## Known limits

- Classic menu is capped at 8 collections, 30 icons per collection, and 242 command IDs.
- Visual Explorer registration and the complete accessibility matrix require manual approval/proof.

## Reality verdict

`[ready / not ready, with remaining gates]`