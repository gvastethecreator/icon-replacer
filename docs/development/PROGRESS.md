# Progress

Date: 2026-07-07

## Completed

### IR-001: Bootstrap Repo + Core/CLI/Test Harness

- Created `IconReplacer.slnx`.
- Created `src/IconReplacer.Core`.
- Created `src/IconReplacer.Cli`.
- Created `tests/IconReplacer.Core.Tests`.
- Added shared build props and `.gitignore`.
- Added domain primitives: targets, operation results, errors, Icon Library paths, and restore records.

Proof:

- `dotnet build IconReplacer.slnx`: pass.
- `dotnet test IconReplacer.slnx --no-build`: pass.

### IR-002: `.ico` Validation + Catalog `.icons`

- Added structural `.ico` validation.
- Added local/remote path policy.
- Added Icon Library scan.
- Added import to `.icons\Imported` with hash-based dedupe.
- Connected CLI `catalog` to the real scanner.

Proof:

- Tests cover valid `.ico`, renamed PNG, truncated directory, image outside file, no common size, oversized icon, unsupported extension, remote path, category scan, invalid warning, import copy, dedupe, and invalid import.
- `dotnet run --no-build --project src\IconReplacer.Cli -- catalog` sees `C:\Users\cristian\.icons`.

### IR-003: Folder Apply/Restore Engine

- Added `desktop.ini` parser/merger.
- Added folder apply using imported icon and `IconResource=<path>,0`.
- Added folder restore with previous icon keys and attributes.
- Added safe write preparation for hidden/system/read-only `desktop.ini`.
- Added Explorer refresh abstraction with no-op default.

Proof:

- Tests cover new `desktop.ini`, existing `desktop.ini`, preservation of unrelated keys, restore of previous `IconFile/IconIndex`, attribute restoration, missing-folder failure, and unsupported restore record.

## Test Icon Collections

Created 6 local collections under `C:\Users\cristian\.icons`, each with 30 icons copied from `D:\ICONS\Folder11-Ico\ico`:

- `Adobe Creative`
- `Design 3D`
- `Developer Tools`
- `Media Audio Video`
- `System Utilities`
- `Gaming Hardware`

CLI catalog proof now reports 7 categories including `Imported` and 182 icons after the latest real apply proof imported one additional icon.

### IR-004: Shortcut `.lnk` Apply/Restore Engine

- Added Windows Shell link COM client.
- Added shortcut apply using imported icon and `SetIconLocation`.
- Added shortcut restore using previous icon path/index.
- Preserved target path, arguments, working directory, description, and hotkey.

Proof:

- Tests create temporary `.lnk` files and verify apply/restore behavior.
- Tests prove metadata preservation around icon changes.

### IR-005: CLI Proof Path

- Connected `doctor`, `catalog`, `apply-folder`, `apply-shortcut`, and `restore`.
- Added JSON restore state at `%AppData%\Icon Replacer\state.json`.
- Added restore record store tests.
- Added `history`/`records` listing for persisted restore records.
- Enhanced `doctor` with catalog and restore-state counts.
- Proved CLI apply/restore on temporary folder and `.lnk` targets.

Proof:

- Folder CLI proof record: `da27c74b-3a2d-4f3f-8015-f4db720fafea`.
- Shortcut CLI proof record: `902e509d-f21e-47c9-8657-d6d75558e312`.
- Folder restore removed temporary `desktop.ini`.
- Shortcut restore returned icon to `C:\Users\cristian\.icons\Adobe Creative\adobe.ico,0` and preserved `--cli-proof` arguments.

### Pre-IR-006: Shell Selection Evaluation

- Added `ShellSelectionService` to decide whether a shell selection should enable `Change icon...`.
- Added explicit outcomes for supported, no selection, multi-selection unsupported, missing target, remote path unsupported, unsupported target, and invalid path.
- Added CLI `target` and `selection` aliases to inspect the same contract outside Explorer.
- Added `IconChangeService` for the post-picker workflow: shell selection plus chosen icon becomes one validated apply operation.
- Added CLI `change` to exercise that workflow without depending on Explorer registration.
- Kept Explorer registration blocked by IR-000; this slice only validates selection behavior.

Proof:

- `ShellSelectionServiceTests.EvaluatePathSupportsExistingFolder` passes.
- `ShellSelectionServiceTests.EvaluatePathSupportsExistingShortcut` passes.
- `ShellSelectionServiceTests.EvaluatePathRejectsExistingUnsupportedFile` passes.
- `ShellSelectionServiceTests.EvaluateRejectsMultipleSelection` passes.
- `ShellSelectionServiceTests.EvaluateRejectsRemoteSelection` passes.
- `ShellSelectionServiceTests.EvaluatePathRejectsMissingTarget` passes.
- `IconChangeServiceTests.ChangeIconAppliesFolderAndStoresRestoreRecord` passes.
- `IconChangeServiceTests.ChangeIconRejectsUnsupportedSelectionBeforeImport` passes.
- `IconChangeServiceTests.ChangeIconRejectsMultipleSelectionBeforeImport` passes.
- CLI `target C:\Users\cristian\.icons` reports `Supported` and `Can show Change icon: yes`.
- CLI `target C:\Users\cristian\.icons\Adobe Creative\adobe.ico` reports `UnsupportedTarget`, `Can show Change icon: no`, and exit code 65.
- CLI `change` rejects an `.ico` file selected as the target with exit code 65 before mutation; `doctor` still reports 4 restore records.

### Pre-IR-006.2: Change Preview Before Mutation

- Added `IconChangePreviewService` for the UI-ready pre-apply state after a target and icon are selected.
- Preview validates shell target support and selected icon details without calling mutation services.
- Preview returns target status, icon details or icon error, `CanApply`, and the blocking error when disabled.
- Connected CLI `preview-change` and `preview` to inspect the same state before Explorer or WinUI integration exists.
- Kept preview non-mutating: no `desktop.ini`, `.lnk` metadata, imported icon files, or restore history writes.

Proof:

- `IconChangePreviewServiceTests.PreviewChangeAllowsValidFolderAndIconWithoutApplying` passes.
- `IconChangePreviewServiceTests.PreviewChangeReportsUnsupportedTargetAndStillDescribesValidIcon` passes.
- `IconChangePreviewServiceTests.PreviewChangeReportsInvalidIconWithoutApplyingToValidTarget` passes.
- CLI `preview-change C:\Users\cristian\.icons C:\Users\cristian\.icons\Adobe Creative\adobe.ico` reports `Can apply: yes`, target kind `Folder`, icon category `Adobe Creative`, and recommended image `256x256`.
- CLI `preview-change C:\Users\cristian\.icons\Adobe Creative\adobe.ico C:\Users\cristian\.icons\Adobe Creative\adobe.ico` reports `Can apply: no`, `UnsupportedTarget`, valid icon details, and exit code 65.

### Pre-IR-006.3: Direct Icon Picker Request

- Added `IconPickerRequestService` for the future `Change icon...` direct file-picker flow.
- Picker requests reuse shell-selection validation and are enabled only for one supported local folder or `.lnk`.
- Valid requests prepare `.icons` and `.icons\Imported`, use the Icon Library as the initial directory, restrict file type to `.ico`, and remain single-select.
- Connected CLI `picker-request` and `picker` to preview the same request without opening a native picker.
- Kept invalid selections non-mutating: unsupported targets and multi-selection do not prepare picker folders or apply icons.

Proof:

- `IconPickerRequestServiceTests.CreateRequestBuildsSingleIconPickerForSupportedFolder` passes.
- `IconPickerRequestServiceTests.CreateRequestRejectsUnsupportedTargetWithoutPreparingPickerFolders` passes.
- `IconPickerRequestServiceTests.CreateRequestRejectsMultipleSelection` passes.
- CLI `picker-request C:\Users\cristian\.icons` reports `Can open picker: yes`, target kind `Folder`, initial directory `C:\Users\cristian\.icons`, extension `.ico`, and `Allow multiple: no`.
- CLI `picker-request C:\Users\cristian\.icons\Adobe Creative\adobe.ico` reports `Can open picker: no`, `UnsupportedTarget`, and exit code 65.

### Pre-IR-006.4: Explorer-To-App Launch Request

- Added `AppLaunchRequestService` for the future Modern shell extension to packaged app handoff.
- Launch requests build stable app arguments: `change-icon --target <path> --target-kind <folder|shortcut>`.
- The service also parses the same argument shape for packaged app activation.
- Launch readiness reuses picker-request readiness so Explorer target validation and app picker setup stay aligned.
- Connected CLI `launch-request` and `launch` to preview the future handoff without registering Explorer.

Proof:

- `AppLaunchRequestServiceTests.CreateChangeIconRequestBuildsStableArgumentsForFolder` passes.
- `AppLaunchRequestServiceTests.ParseArgumentsSupportsExplicitShortcutKind` passes.
- `AppLaunchRequestServiceTests.CreateChangeIconRequestRejectsUnsupportedTargetWithoutArguments` passes.
- `AppLaunchRequestServiceTests.ParseArgumentsRejectsMissingTarget` passes.
- CLI `launch-request C:\Users\cristian\.icons` reports `Can launch: yes`, target kind `Folder`, and app arguments `change-icon --target C:\Users\cristian\.icons --target-kind folder`.
- CLI `launch-request C:\Users\cristian\.icons\Adobe Creative\adobe.ico` reports `Can launch: no`, `UnsupportedTarget`, no app arguments, and exit code 65.

### IR-000: Modern Shell Integration Decision

- Accepted ADR-0002 for V1: Modern Shell Integration with MSIX package identity plus native `IExplorerCommand`.
- Kept Classic HKCU context-menu verbs as fallback/prototype only.
- Changed the default shell readiness from `DecisionPending` to `NotConfigured`.
- Added `ShellIntegrationPlanService` and a `shell-plan` CLI command so setup/diagnostics can explain the selected path and prerequisites.
- Kept Explorer registration blocked until package identity, native extension, install/uninstall proof, and missing WinUI tooling prerequisites are resolved.

Proof:

- `ShellIntegrationPlanServiceTests.GetPlanSelectsModernIntegrationForV1` passes.
- `ShellIntegrationPlanServiceTests.GetPlanReportsMissingWinAppAsBlockingForModernPath` passes.
- `ShellIntegrationPlanServiceTests.GetPlanReportsConfiguredExplorerRegistration` passes.
- CLI `shell-plan` reports decision final `yes`, selected mode `Modern MSIX + IExplorerCommand`, fallback `Classic HKCU context-menu verb`, readiness `NotConfigured`, `winapp` missing as blocking, and Explorer registration not configured as warning.

### Pre-IR-008: AppModel Dashboard Foundation

- Added `src/IconReplacer.AppModel`.
- Added setup/readiness snapshot model for first-run WinUI state.
- Added app-location target model for future Open Library and diagnostics actions.
- Added dashboard snapshot model for future WinUI surfaces.
- Dashboard summarizes Icon Library paths, category/icon counts, catalog warnings, restore record counts, restorable records, category summaries, and recent records.
- Dashboard classifies restore-record health: target exists, applied icon exists, and can restore.
- Connected CLI `doctor` to the same dashboard snapshot.
- Added `RestoreHistoryService` for filtered all/restorable/applied/restored/stale recent-change views.
- Connected CLI `history` and `records` to `RestoreHistoryService`.
- Added `IconApplyService` as the shared product operation for applying icons and saving restore history.
- Connected CLI `apply`, `apply-folder`, and `apply-shortcut` to `IconApplyService`.
- Added `IconRestoreService` as the shared product operation for restoring by record id and updating restore history.
- Connected CLI `restore` to `IconRestoreService`.
- Added `IconLibraryService` as the shared product operation for importing icons and returning library status.
- Connected CLI `import` to `IconLibraryService`.
- Added `SetupReadinessService` for first-run readiness, setup actions, and shell integration status.
- Connected CLI `status` and `setup` to `SetupReadinessService`.
- Added `AppLocationService` for Icon Library, Imported, AppData, and restore-state navigation targets.
- Connected CLI `paths` and `locations` to `AppLocationService`.
- Added `IconMenuService` as the shared dynamic menu snapshot for future shell and WinUI surfaces.
- Connected CLI `menu` to preview `Change icon...`, Icon Library categories, and icon entries.
- Hardened import dedupe so matching content returns an existing imported icon even when the preferred display name differs.

Proof:

- `DashboardServiceTests.GetSnapshotSummarizesCatalogAndRestoreRecords` passes.
- `DashboardServiceTests.GetSnapshotFlagsAppliedRecordsWithMissingAssets` passes.
- `RestoreHistoryServiceTests.GetHistorySummarizesAllRecords` passes.
- `RestoreHistoryServiceTests.GetHistoryFiltersRestorableRecords` passes.
- `RestoreHistoryServiceTests.GetHistoryFiltersStaleRecords` passes.
- `IconApplyServiceTests.ApplyDetectsFolderTargetAndStoresRestoreRecord` passes.
- `IconApplyServiceTests.ApplyRejectsUnsupportedExistingFile` passes.
- `IconRestoreServiceTests.RestoreByIdRestoresFolderAndUpdatesStore` passes.
- `IconRestoreServiceTests.RestoreRejectsRecordThatIsNotApplied` passes.
- `IconRestoreServiceTests.RestoreMissingRecordReturnsPathNotFound` passes.
- `IconLibraryServiceTests.ImportIconCopiesToImportedAndReturnsUpdatedStatus` passes.
- `IconLibraryServiceTests.ImportIconDedupesSameContent` passes.
- `IconLibraryServiceTests.ImportIconRejectsInvalidIcon` passes.
- `IconLibraryImporterTests.ImportDedupesByHashWhenPreferredNameDiffers` passes.
- `SetupReadinessServiceTests.GetSnapshotCreatesLibraryAndReportsEmptyFirstRunActions` passes.
- `SetupReadinessServiceTests.GetSnapshotReportsReadyLibraryCounts` passes.
- `SetupReadinessServiceTests.GetSnapshotReportsCatalogWarningsWithoutBlockingCoreUse` passes.
- `AppLocationServiceTests.GetLocationsEnsuresLibraryFolders` passes.
- `AppLocationServiceTests.GetLocationsReportsRestoreStateExistence` passes.
- `AppLocationServiceTests.GetLocationsReportsMissingRestoreStateWithoutCreatingFile` passes.
- `IconMenuServiceTests.BuildSnapshotGroupsRootAndCategoryIcons` passes.
- `IconMenuServiceTests.BuildSnapshotReflectsNewFoldersOnNextScan` passes.
- `IconMenuServiceTests.BuildSnapshotReportsOmittedItemsWhenMenuIsCapped` passes.
- CLI `apply <target> <icon.ico>` applied and restored a temporary folder target; after restore, `desktop.ini` no longer exists.
- CLI `restore <record-id>` now runs through `IconRestoreService`; latest proof restored `7cde5348-3ada-4448-be43-11f740927a63` and removed `desktop.ini`.
- CLI `import <icon.ico> [display-name]` now imports through `IconLibraryService`; real proof re-imported `adobe.ico` with a different display name and returned existing `adobe-535ab004.ico` without increasing catalog count.
- CLI `status` reports core features ready, 182 icons, 7 categories, 4 restore records, shell integration `NotConfigured`, and setup actions for missing targets plus configuring shell integration.
- CLI `paths` reports existing Icon Library, Imported, AppData, and restore-state locations.
- CLI `history` supports `all`, `restorable`, `applied`, `restored`, and `stale`; real proof shows 4 total records, 2 stale records, and 0 restorable records.
- CLI `menu` reports `Change icon...`, 7 categories, and 182/182 visible icons from `C:\Users\cristian\.icons`.
- `doctor` reports 7 categories, 182 icons, 0 warnings, 4 restore records, 0 restorable records, and 2 missing targets from earlier deleted temporary proof targets.

### Pre-IR-008.8: WinUI Home Snapshot

- Added `AppHomeService` and `AppHomeSnapshot` as the shared first-screen state for the future WinUI app.
- The snapshot composes setup readiness, dashboard counts, dynamic menu preview, filtered restore history, app locations, and refresh time.
- Connected CLI `home` and `app` to preview the same state without requiring WinUI runtime.
- Kept the app surface independent from WinUI controls while `winapp` is missing.

Proof:

- `AppHomeServiceTests.GetSnapshotComposesMainWindowState` passes.
- `AppHomeServiceTests.GetSnapshotHonorsHistoryFilterAndMenuCaps` passes.
- CLI `home` reports core features ready, shell integration `NotConfigured`, 182 icons, 7 categories, 182/182 menu icons, 4 restore records, 2 stale records, and app locations.
- CLI `home stale` reports the same home state with history filter `Stale`.

### Pre-IR-008.14: Packaged App Activation Snapshot

- Added `AppActivationService` and activation snapshot models for future packaged WinUI startup.
- Empty activation arguments route to the Home snapshot.
- `change-icon --target <path> --target-kind <folder|shortcut>` routes through `AppLaunchRequestService` and the validated picker request.
- Unsupported targets return a disabled `ChangeIcon` activation snapshot rather than opening a picker.
- Malformed activation arguments fail before picker or mutation logic.
- Connected CLI `activate` to preview the future packaged-app routing.

Proof:

- `AppActivationServiceTests.ActivateWithoutArgumentsReturnsHomeSnapshot` passes.
- `AppActivationServiceTests.ActivateWithChangeIconArgumentsReturnsLaunchRequest` passes.
- `AppActivationServiceTests.ActivateWithUnsupportedTargetReturnsDisabledChangeIconSnapshot` passes.
- `AppActivationServiceTests.ActivateRejectsUnknownVerb` passes.
- CLI `activate` reports kind `Home`, core ready, shell integration `NotConfigured`, 182 icons, and 7 categories.
- CLI `activate change-icon --target C:\Users\cristian\.icons --target-kind folder` reports kind `ChangeIcon`, `Can continue: yes`, target kind `Folder`, and picker can open.

### Pre-IR-008.15: Activated Post-Picker Change Flow

- Added `ActivatedIconChangeService` to combine packaged app activation arguments with the `.ico` path returned by the picker.
- Added a non-mutating preview path for activated change flows.
- Added an apply path that uses `IconChangeService`, preserving import and restore-record behavior.
- Non-`change-icon` activations and invalid selected icons are rejected before mutation.
- Connected CLI `activate-preview` to preview post-picker readiness without writing target metadata.

Proof:

- `ActivatedIconChangeServiceTests.PreviewSelectedIconCombinesActivationAndIconDetailsWithoutApplying` passes.
- `ActivatedIconChangeServiceTests.ApplySelectedIconChangesFolderAndStoresRestoreRecord` passes.
- `ActivatedIconChangeServiceTests.PreviewSelectedIconRejectsHomeActivation` passes.
- `ActivatedIconChangeServiceTests.ApplySelectedIconRejectsInvalidIconBeforeMutation` passes.
- CLI `activate-preview C:\Users\cristian\.icons\Adobe Creative\adobe.ico change-icon --target C:\Users\cristian\.icons --target-kind folder` reports `Can apply: yes`, target kind `Folder`, icon category `Adobe Creative`, and recommended image `256x256`.

### Pre-IR-008.9: WinUI Icon Browser Snapshot

- Added `IconBrowserService` and browser snapshot models for the future WinUI catalog browser.
- Supports search text, category filter, max visible item cap, omitted counts, category summaries, and catalog warnings.
- Connected CLI `browse` and `icons` to preview the same browser state.

Proof:

- `IconBrowserServiceTests.BrowseFiltersBySearchAndCategory` passes.
- `IconBrowserServiceTests.BrowseCapsVisibleItems` passes.
- `IconBrowserServiceTests.BrowseReportsCatalogWarnings` passes.
- CLI `browse adobe --max 5` reports 5/39 shown from 182 total icons.
- CLI `browse --category "Developer Tools" --max 5` reports 5/30 shown from that category.
- CLI `browse --max 3` reports 3/182 shown and 179 omitted.

### Pre-IR-008.10: WinUI Recent Changes Actions

- Added `RecentChangesService` and recent-change row models for the future WinUI recent changes surface.
- Each row exposes restore action state, enabled/disabled state, health text, and warning text.
- Missing applied icons produce an enabled-with-warning state because restore uses the saved previous-state snapshot.
- Connected CLI `recent` and `changes` to preview the same action rows.

Proof:

- `RecentChangesServiceTests.GetRecentChangesEnablesHealthyAppliedRecord` passes.
- `RecentChangesServiceTests.GetRecentChangesEnablesWithWarningWhenAppliedIconIsMissing` passes.
- `RecentChangesServiceTests.GetRecentChangesDisablesMissingTargetAndAlreadyRestoredRecords` passes.
- `RecentChangesServiceTests.GetRecentChangesHonorsHistoryFilter` passes.
- CLI `recent` reports 4 real changes, 0 restore-enabled changes, and 4 disabled changes with reasons.
- CLI `recent stale` reports the 2 stale real changes with `Target missing` reasons.

### Pre-IR-008.11: WinUI Diagnostics Snapshot

- Added `AppDiagnosticsService` and diagnostic check models for the future WinUI diagnostics surface.
- Diagnostics compose setup readiness, dashboard counts, app locations, shell integration state, and WinUI tooling readiness.
- Added `WinUiToolingSnapshot` so the app can distinguish WinUI templates from `winapp` CLI availability.
- Connected CLI `diagnostics` and `diag` to passive tooling checks; it reports blockers but does not install or run WinUI.

Proof:

- `AppDiagnosticsServiceTests.GetDiagnosticsReportsReadyCoreAndPendingShell` passes.
- `AppDiagnosticsServiceTests.GetDiagnosticsReportsEmptyLibraryAndMissingWinApp` passes.
- `AppDiagnosticsServiceTests.GetDiagnosticsReportsCatalogWarnings` passes.
- CLI `diagnostics` reports 1 blocker (`winapp` missing), 1 warning (`NotConfigured` shell integration), WinUI templates available, 182 icons, and existing app locations.
- CLI `diagnostics` exits with code 2 while `winapp` is missing.

### Pre-IR-008.12: WinUI Batch Import Results

- Added batch import models for the future WinUI multi-file picker flow.
- Added `IconLibraryService.ImportIcons` and `ImportIconsFromEnvironment`.
- Batch import returns per-file status: imported, reused existing, or failed.
- Dedupe is reported as `ReusedExisting` without increasing catalog counts.
- Connected CLI `batch-import` and `import-many`.

Proof:

- `IconLibraryServiceTests.ImportIconsReturnsPerFileResults` passes.
- `IconLibraryServiceTests.ImportIconsMarksDuplicatesAsReused` passes.
- `IconLibraryServiceTests.ImportIconsRejectsNullSourceList` passes.
- CLI `batch-import` with two copies of `adobe.ico` reports 2 reused existing, 0 imported, 0 failed.
- Catalog remains 182 icons after the duplicate batch proof.

### Pre-IR-008.13: WinUI Icon Details Snapshot

- Added `IconDetailsService` and detail models for the future WinUI icon detail/preview panel.
- Details include display name, category, library membership, byte length, internal ICO image entries, and recommended image.
- Works for both Icon Library entries and valid external `.ico` files before import.
- Connected CLI `details` and `icon-details`.

Proof:

- `IconDetailsServiceTests.GetDetailsDescribesCatalogIcon` passes.
- `IconDetailsServiceTests.GetDetailsDescribesExternalValidIcon` passes.
- `IconDetailsServiceTests.GetDetailsRejectsInvalidIcon` passes.
- CLI `details C:\Users\cristian\.icons\Adobe Creative\adobe.ico` reports category `Adobe Creative`, 8 images, and recommended image `256x256`.

## Latest Verification

```powershell
dotnet build IconReplacer.slnx
dotnet test IconReplacer.slnx --no-build
dotnet run --no-build --project src\IconReplacer.Cli -- diagnostics
dotnet run --no-build --project src\IconReplacer.Cli -- shell-plan
dotnet run --no-build --project src\IconReplacer.Cli -- activate
dotnet run --no-build --project src\IconReplacer.Cli -- activate change-icon --target "C:\Users\cristian\.icons" --target-kind folder
dotnet run --no-build --project src\IconReplacer.Cli -- activate-preview "C:\Users\cristian\.icons\Adobe Creative\adobe.ico" change-icon --target "C:\Users\cristian\.icons" --target-kind folder
dotnet run --no-build --project src\IconReplacer.Cli -- home
dotnet run --no-build --project src\IconReplacer.Cli -- home stale
dotnet run --no-build --project src\IconReplacer.Cli -- browse adobe --max 5
dotnet run --no-build --project src\IconReplacer.Cli -- browse --category "Developer Tools" --max 5
dotnet run --no-build --project src\IconReplacer.Cli -- details "C:\Users\cristian\.icons\Adobe Creative\adobe.ico"
dotnet run --no-build --project src\IconReplacer.Cli -- preview-change "C:\Users\cristian\.icons" "C:\Users\cristian\.icons\Adobe Creative\adobe.ico"
dotnet run --no-build --project src\IconReplacer.Cli -- recent
dotnet run --no-build --project src\IconReplacer.Cli -- recent stale
dotnet run --no-build --project src\IconReplacer.Cli -- batch-import "C:\Users\cristian\.icons\Adobe Creative\adobe.ico" "C:\Users\cristian\.icons\Adobe Creative\adobe.ico"
dotnet run --no-build --project src\IconReplacer.Cli -- catalog
dotnet run --no-build --project src\IconReplacer.Cli -- target C:\Users\cristian\.icons
dotnet run --no-build --project src\IconReplacer.Cli -- picker-request C:\Users\cristian\.icons
dotnet run --no-build --project src\IconReplacer.Cli -- launch-request C:\Users\cristian\.icons
dotnet run --no-build --project src\IconReplacer.Cli -- change <target> <icon.ico>
dotnet run --no-build --project src\IconReplacer.Cli -- status
dotnet run --no-build --project src\IconReplacer.Cli -- paths
dotnet run --no-build --project src\IconReplacer.Cli -- doctor
dotnet run --no-build --project src\IconReplacer.Cli -- history
dotnet run --no-build --project src\IconReplacer.Cli -- history stale
```

Result:

- Build: pass, 0 warnings, 0 errors.
- Tests: pass, 112 total.
- Diagnostics: pass, reports `winapp` missing as a blocking issue and shell integration `NotConfigured` as a warning.
- Shell plan: pass, reports Modern MSIX plus `IExplorerCommand` as the accepted V1 path, Classic HKCU as fallback, `winapp` missing as blocking, and Explorer registration not configured.
- Activation: pass, no args route to Home; `change-icon` args route to the launch/picker flow for `C:\Users\cristian\.icons`.
- Activated preview: pass, activation args plus real `adobe.ico` report `Can apply: yes` without mutation.
- Home: pass, core ready, 182 icons, 7 categories, 4 restore records, 2 stale records, shell integration `NotConfigured`.
- Browse: pass, search/category filters and capped results work against the real 182-icon library.
- Details: pass, real `adobe.ico` shows 8 internal images and recommended 256x256 image.
- Preview change: pass, real `.icons` folder plus `adobe.ico` reports `Can apply: yes`; selecting an `.ico` as target reports `UnsupportedTarget`, keeps icon details visible, and exits 65.
- Recent: pass, 4 real changes shown, 0 restore-enabled, 2 stale with explicit missing-target reasons.
- Batch import: pass, duplicate selected icons reuse existing imported file and catalog remains 182 icons.
- Catalog: pass, 7 categories, 182 icons.
- Target: pass, supported folder enables `Change icon...`; unsupported `.ico` target disables it with exit code 65.
- Picker request: pass, supported folder creates a single-select `.ico` picker request rooted at `C:\Users\cristian\.icons`; unsupported `.ico` target disables the picker with exit code 65.
- Launch request: pass, supported folder creates `change-icon --target C:\Users\cristian\.icons --target-kind folder`; unsupported `.ico` target disables launch with exit code 65.
- Change: pass, unsupported selected target is rejected before mutation.
- Status: pass, core features ready and shell integration `NotConfigured`.
- Paths: pass, all current app locations reported.
- Menu: pass, `Change icon...`, 7 categories, 182/182 visible icons.
- Doctor: pass, dashboard counts and restore-health counts shown.
- History: pass, 4 restored proof records shown; stale filter shows 2 deleted proof targets, restorable filter shows 0 records.

## Next

Modern shell integration is selected, but WinUI scaffolding/running still needs `winapp`; `dotnet new list winui` is available, but `winapp` is currently missing. Continue with non-WinUI AppModel/CLI work or run `/winui-setup` before scaffolding the packaged WinUI/native shell path.

## Open Gate

Explorer registration remains blocked until the selected Modern MSIX plus native `IExplorerCommand` path is built and verified. Do not register final shell integration yet.
