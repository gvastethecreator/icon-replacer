# Release Evidence

Release: 1.0.0.5 development candidate
Date: 2026-07-14
Windows build: Windows 25H2, 26200.8737
Integration path: Packaged Windows 11 + classic Explorer menus
Current candidate SHA-256: `07165F5CD50912C5DB00713652A22F3FC8253C57A80BC809DB713B5BF587CAE7`
Current candidate installed: yes; `IconReplacer_1.0.0.5_x64__2wx6x5nenbha0`

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

## Automated Results

- Build: pass, 0 warnings, 0 errors.
- Unit/integration tests: 289 passed, 0 failed, including adaptive DPI thumbnail decoding, rounded official display assets, apply/restore compensation and concurrency, GitHub release/update-state parsing, guarded deployment evidence, WinUI/shell source contracts, and directory-link coverage.
- Native shell smoke: pass, normal modern commands remain enabled, classic dispatch hides those modern roots to prevent duplicates, 187 commands, every collection/icon preview is a visible 32-bit ARGB DIB and passes `HBMMENU_CALLBACK` measure/draw, constrained 8-id range, and 150.156 ms current full-catalog query under the 250 ms budget.
- Official icon assets: pass, canonical light/dark source hashes locked; rounded 512 px display PNGs; nine ICO sizes each; deterministic regeneration; theme-aware WinUI and Explorer routing; 24/48 taskbar variants packaged.
- Signed MSIX: pass, version `1.0.0.5`, 48,592,977 bytes, valid `CN=IconReplacerDev` signature, SHA-256 `07165F5CD50912C5DB00713652A22F3FC8253C57A80BC809DB713B5BF587CAE7`.
- Snapshot-only shell guard: pass, 57 unrelated entries with normalized hash `A7C938435CB53F206BC26A2A9698273F36E8E7F03F427F0AF92BF9BD493B99C1`; the previous Icon Replacer candidate remains installed.
- Guarded deployment: pass, the exact signed `1.0.0.5` candidate upgraded the matching identity only after explicit approval and reloaded Explorer.
- Preservation: pass, 185 Icon Library files, restore state, and unrelated context-menu registrations retained their guarded baseline.

## Accessibility Proof

- Gallery First contract: pass for Library, Recent, Settings, and About surfaces.
- Dynamic names: source-contract pass for icon name plus collection and restore action plus target.
- Persistent announcements: source-contract pass for polite live regions and InfoBar close/update/reopen ordering.
- Keyboard and layout: repeatable UIA script prepared for toolbar, search, slider, category list, icon grid, splitter, Recent, Settings, and About/update state.
- 125% baseline run: previous installed candidate passed 6 checks and exposed 2 actionable failures; the brittle UIA property read and missing Settings button name are corrected in current source.
- Current-candidate 125% UIA run: 9/9 pass.
- 200% UIA run: pending execution on a 200% display.
- High Contrast visual run: pending manual theme proof.

## Manual Explorer Proof

- Folder menu screenshot: pass for one grouped command pair, both official app icons, separator, no duplicates, and unrelated entries present.
- `.lnk` menu screenshot: pending.
- Picker launch: pass; direct command opened the native `.ico` picker and was canceled without mutation.
- Folder before/after/restore: partial historical proof; final current-package matrix pending.
- `.lnk` before/after/restore: pending.
- Invalid icon: automated rollback proof passes; Explorer screenshot pending.
- Permission failure: automated rollback proof passes; Explorer screenshot pending.
- Empty catalog: automated model/native state proof available; Explorer screenshot pending.
- Category catalog: native callback measure/draw proof passes; current-package nested Explorer screenshot pending after interrupted UI proof.

## Install/Uninstall Proof

- Package/manifest: signed current MSIX and build evidence under `artifacts\package`.
- Current install log: pass for exact SHA-256 in `artifacts\package\lifecycle-install-evidence.json`.
- Current uninstall log: pending for the same reason.
- Post-uninstall shell integration check: pending for `1.0.0.5`; current guarded install evidence confirms the unrelated-handler baseline while this candidate is installed.
- `%USERPROFILE%\.icons` preserved: pass, 185 files in 7 collections.

## Known Limits

- Classic menu is capped at 8 collections, 30 icons per collection, and 242 command IDs.
- Visual Explorer registration and the complete accessibility matrix require manual approval/proof.

## Reality Verdict

Current recovery/safety slice: quality wins.

Release verdict: not ready. Current-package install/uninstall, accessibility, and
manual Explorer proof remain required.
