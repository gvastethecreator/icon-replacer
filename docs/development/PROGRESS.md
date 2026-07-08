# Progress

Date: 2026-07-08

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
- Added Explorer refresh abstraction with a Windows `SHChangeNotify` default and no-op/recording test substitutes.

Proof:

- Tests cover new `desktop.ini`, existing `desktop.ini`, preservation of unrelated keys, restore of previous `IconFile/IconIndex`, attribute restoration, missing-folder failure, and unsupported restore record.

## Test Icon Collections

Created curated local collections under `C:\Users\cristian\.icons`, copied from `D:\ICONS\Folder11-Ico\ico`, for shell-menu and browser proof:

- `Adobe Creative`
- `Design 3D`
- `Developer Tools`
- `Media Audio Video`
- `System Utilities`
- `Gaming Hardware`
- `Test - Adobe Creative`
- `Test - Design and 3D`
- `Test - Developer Stack`
- `Test - Gaming Platforms`
- `Test - Media Studio`
- `Test - System and Office`
- `Folder11 - Adobe Creative Suite`
- `Folder11 - Design and 3D Studio`
- `Folder11 - Developer Web Stack`
- `Folder11 - Games and Platforms`
- `Folder11 - Media Streaming Studio`
- `Folder11 - System Office Utilities`

CLI catalog proof now reports 19 categories including `Imported` and 542 icons after the curated test collections were added.

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
- CLI `change` rejects an `.ico` file selected as the target with exit code 65 before mutation; `doctor` reports the current restore-history count without adding records.

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

### Pre-IR-006.5: Modern Shell Manifest Contract

- Added `ShellManifestContractService` and manifest contract snapshot models.
- The contract defines the future `windows.comServer` and `windows.fileExplorerContextMenus` manifest entries before packaging work starts.
- The contract keeps one stable Explorer command CLSID for `IconReplacer.ShellExtension.dll` with `STA` threading.
- The context-menu targets are `Directory` and `.lnk`, both routed to the same native command class.
- Connected CLI `shell-manifest` and `manifest-plan`.
- `shell-plan` now reports the manifest contract as a passed Modern-path prerequisite.

Proof:

- `ShellManifestContractServiceTests.GetContractDefinesModernShellManifestParts` passes.
- `ShellManifestContractServiceTests.GetContractRegistersDirectoryAndShortcutTargetsWithSameClsid` passes.
- `ShellManifestContractServiceTests.GetContractBuildsParseableManifestFragment` passes.
- CLI `shell-manifest` prints a parseable XML fragment with `windows.comServer`, `windows.fileExplorerContextMenus`, `Directory`, and `.lnk` entries without registering Explorer.

### Pre-IR-006.6: Native Shell Extension Bridge Contract

- Added `ShellExtensionBridgeService`, `ShellExtensionBridgeSnapshot`, and `ShellExtensionBridgeCommand`.
- The bridge composes shell manifest identity, menu command descriptors, shell target evaluation, resolved command arguments, disabled reasons, and Explorer safety rules in one snapshot.
- The bridge reports the stable protocol version, CLSID, `IconReplacer.ShellExtension.dll`, `STA`, required native interfaces, `Directory`/`.lnk` targets, menu state, command counts, invocable command counts, and target status.
- Connected CLI `shell-bridge` and `native-shell-bridge`.
- Kept the bridge non-mutating: it does not apply icons, open pickers, launch windows, install packages, register Explorer, or edit target metadata.

Proof:

- `ShellExtensionBridgeServiceTests.BuildBridgeForSupportedFolderResolvesChangeAndIconCommands` passes.
- `ShellExtensionBridgeServiceTests.BuildBridgeDisablesTargetCommandsForUnsupportedTargetButKeepsOpenApp` passes.
- `ShellExtensionBridgeServiceTests.BuildBridgeReportsMenuCapsAndLeavesTargetCommandsDisabledWithoutSelection` passes.
- `ShellExtensionBridgeServiceTests.BuildBridgeReportsUnavailableMenuWithoutThrowing` passes.
- CLI `shell-bridge C:\Users\cristian\.icons` reports manifest identity, target status `Supported`, safety rules, and resolved commands without mutation.

### Pre-IR-009: Packaging and Uninstall Readiness Plan

- Added `PackagingPlanService`, `PackagingPlanInputs`, and package-plan snapshot models.
- The plan keeps per-user MSIX as the selected install mode and reports missing packaging gates before install work starts.
- Packaging gates include `winapp`, native build tools, package identity, native shell extension, development signing, installer build, fresh install proof, and uninstall proof.
- The plan reuses the shell manifest contract so packaging metadata stays aligned with `shell-manifest`.
- Uninstall policy is explicit: remove shell integration, preserve `.icons`, and preserve restore history by default.
- Connected CLI `package-plan` and `packaging-plan`.

Proof:

- `PackagingPlanServiceTests.GetPlanReportsCurrentMissingPackagingGates` passes.
- `PackagingPlanServiceTests.GetPlanPassesBuildGatesWhenInputsAreReady` passes.
- `PackagingPlanServiceTests.GetPlanPreservesUserDataOnUninstallByDefault` passes.
- Initial CLI `package-plan` proof reported missing package/tooling gates and `.icons`/restore-history preservation; current package-plan status is tracked in the latest verification section below.

### Pre-IR-009.1: Native Build Tooling Packaging Gate

- Added `NativeToolingSnapshot` for native shell-extension build readiness.
- Extended `PackagingPlanInputs` so packaging checks can receive both WinUI tooling and native toolchain state.
- Extended `PackagingPlanService` with a required `native-build-tools` gate before `IconReplacer.ShellExtension.dll` can be considered buildable.
- Extended CLI packaging inputs to detect `cl.exe`, Visual Studio MSBuild, and CMake.
- Kept the gate informational when native tooling has not been checked, so unit-only AppModel callers are not forced to claim machine-local state.

Proof:

- `PackagingPlanServiceTests.GetPlanReportsCurrentMissingPackagingGates` covers `native-build-tools` as blocking when native tooling is checked and missing.
- `PackagingPlanServiceTests.GetPlanPassesBuildGatesWhenInputsAreReady` covers ready native tooling.
- CLI `package-plan` reports `native-build-tools` as a separate gate; current setup proof passes this gate through Visual Studio Build Tools and CMake.

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
- Added `AppOperationFeedbackService` for shared success/warning/error feedback across CLI and future WinUI surfaces.
- Connected CLI apply/restore titles to the shared feedback formatter.
- Added `IconRestorePreviewService` as the shared non-mutating restore confirmation state for future WinUI recent-change actions.
- Connected CLI `restore-preview` and `preview-restore` to inspect restore readiness before applying a restore.
- Added `IconLibraryService` as the shared product operation for importing icons and returning library status.
- Connected CLI `import` to `IconLibraryService`.
- Added `IconImportPickerRequestService` for future WinUI multi-select import picker requests.
- Connected CLI `import-picker-request` and `import-picker`.
- Added `IconCollectionService` for listing and creating one-level Icon Library collections.
- Connected CLI `collections` and `collection-create` to `IconCollectionService`.
- Added `IconCollectionImportService` for importing picker selections into one-level collections.
- Connected CLI `collection-import` to `IconCollectionImportService`.
- Added `CatalogWarningsService` for future WinUI review of invalid/skipped catalog icons.
- Connected CLI `catalog-warnings` and `warnings` to preview the review list.
- Added `SetupReadinessService` for first-run readiness, setup actions, and shell integration status.
- Connected CLI `status` and `setup` to `SetupReadinessService`.
- Added `AppActionRequestService` to resolve setup/home action ids into stable WinUI workflow intents.
- Connected CLI `action-request` and `action` to preview setup/home action routing.
- Added `AppLocationService` for Icon Library, Imported, AppData, and restore-state navigation targets.
- Connected CLI `paths` and `locations` to `AppLocationService`.
- Added safe app-location open requests for future WinUI navigation buttons.
- Connected CLI `open-request` to preview known app-location launch intent without opening Explorer.
- Added `IconMenuService` as the shared dynamic menu snapshot for future shell and WinUI surfaces.
- Connected CLI `menu` to preview `Change icon...`, Icon Library categories, and icon entries.
- Added `IconMenuCommandService` to turn menu snapshots into stable shell-facing command descriptors.
- Connected CLI `menu-commands` and `shell-menu` to preview command ids and argument templates.
- Added `IconMenuCommandInvocationService` to resolve one menu command id against a selected target without invoking it.
- Connected CLI `menu-invoke-preview` and `shell-invoke-preview` to preview final arguments and disabled reasons.
- Added `IconMenuApplyService` as the shared direct dynamic-menu icon apply path.
- Connected CLI `menu-apply` to prove submenu icon application before Explorer registration.
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
- `AppOperationFeedbackServiceTests.FromApplyCreatesSuccessFeedbackWithHistoryAction` passes.
- `AppOperationFeedbackServiceTests.FromRestoreCreatesShortcutRestoreFeedback` passes.
- `AppOperationFeedbackServiceTests.FromBatchImportReportsWarningsWhenAnyFileFails` passes.
- `AppOperationFeedbackServiceTests.FromBatchImportReportsInfoWhenEverythingWasReused` passes.
- `AppOperationFeedbackServiceTests.FromErrorUsesErrorMessageAndDetail` passes.
- `IconRestorePreviewServiceTests.PreviewRestoreAllowsAppliedFolderWithoutMutatingRecord` passes.
- `IconRestorePreviewServiceTests.PreviewRestoreWarnsWhenAppliedIconIsMissingButTargetCanRestore` passes.
- `IconRestorePreviewServiceTests.PreviewRestoreDisablesAlreadyRestoredRecord` passes.
- `IconRestorePreviewServiceTests.PreviewRestoreReportsMissingTarget` passes.
- `IconRestorePreviewServiceTests.PreviewRestoreMissingRecordReturnsPathNotFound` passes.
- `IconLibraryServiceTests.ImportIconCopiesToImportedAndReturnsUpdatedStatus` passes.
- `IconLibraryServiceTests.ImportIconDedupesSameContent` passes.
- `IconLibraryServiceTests.ImportIconRejectsInvalidIcon` passes.
- `IconLibraryImporterTests.ImportDedupesByHashWhenPreferredNameDiffers` passes.
- `IconImportPickerRequestServiceTests.CreateRequestBuildsMultiIconImportPicker` passes.
- `IconImportPickerRequestServiceTests.CreateRequestCreatesCollectionDestinationWhenRequested` passes.
- `IconImportPickerRequestServiceTests.CreateRequestRejectsCollectionNameThatSanitizesToEmpty` passes.
- `SetupReadinessServiceTests.GetSnapshotCreatesLibraryAndReportsEmptyFirstRunActions` passes.
- `SetupReadinessServiceTests.GetSnapshotReportsReadyLibraryCounts` passes.
- `SetupReadinessServiceTests.GetSnapshotReportsCatalogWarningsWithoutBlockingCoreUse` passes.
- `AppActionRequestServiceTests.CreateRequestMapsImportIconsWhenLibraryIsEmpty` passes.
- `AppActionRequestServiceTests.CreateRequestMapsMissingTargetsToStaleHistory` passes.
- `AppActionRequestServiceTests.CreateRequestMapsConfigureShellIntegrationToShellPlan` passes.
- `AppActionRequestServiceTests.CreateRequestDisablesKnownActionThatIsNotCurrentlyAvailable` passes.
- `AppActionRequestServiceTests.CreateRequestRejectsUnknownAction` passes.
- `AppLocationServiceTests.GetLocationsEnsuresLibraryFolders` passes.
- `AppLocationServiceTests.GetLocationsReportsRestoreStateExistence` passes.
- `AppLocationServiceTests.GetLocationsReportsMissingRestoreStateWithoutCreatingFile` passes.
- `AppLocationServiceTests.CreateOpenRequestEnablesExistingDirectoryLocation` passes.
- `AppLocationServiceTests.CreateOpenRequestDisablesMissingRestoreStateFileWithoutCreatingIt` passes.
- `AppLocationServiceTests.CreateOpenRequestRejectsInvalidLocationKind` passes.
- `IconMenuServiceTests.BuildSnapshotGroupsRootAndCategoryIcons` passes.
- `IconMenuServiceTests.BuildSnapshotReflectsNewFoldersOnNextScan` passes.
- `IconMenuServiceTests.BuildSnapshotReportsOmittedItemsWhenMenuIsCapped` passes.
- `IconMenuCommandServiceTests.BuildCommandsIncludesChangeIconAndVisibleIconCommands` passes.
- `IconMenuCommandServiceTests.BuildCommandsCreatesStableSafeIconCommandIds` passes.
- `IconMenuCommandServiceTests.BuildCommandsAddsOpenAppOverflowCommandWhenMenuIsTruncated` passes.
- `IconMenuCommandInvocationServiceTests.PreviewInvocationResolvesChangeIconArgumentsForSupportedFolder` passes.
- `IconMenuCommandInvocationServiceTests.PreviewInvocationResolvesIconCommandArgumentsForSupportedShortcut` passes.
- `IconMenuCommandInvocationServiceTests.PreviewInvocationDisablesTargetCommandForUnsupportedSelection` passes.
- `IconMenuCommandInvocationServiceTests.PreviewInvocationAllowsOpenAppOverflowWithoutTarget` passes.
- `IconMenuCommandInvocationServiceTests.PreviewInvocationRejectsUnknownCommandId` passes.
- `IconMenuApplyServiceTests.ApplyMenuIconAppliesCatalogIconAndStoresRestoreRecord` passes.
- `IconMenuApplyServiceTests.ApplyMenuIconRejectsUnsupportedTargetBeforeScanningCatalog` passes.
- `IconMenuApplyServiceTests.ApplyMenuIconRejectsExternalIconBeforeMutation` passes.
- CLI `apply <target> <icon.ico>` applied and restored a temporary folder target; after restore, `desktop.ini` no longer exists.
- CLI `restore <record-id>` now runs through `IconRestoreService`; latest proof restored `7cde5348-3ada-4448-be43-11f740927a63` and removed `desktop.ini`.
- CLI `import <icon.ico> [display-name]` now imports through `IconLibraryService`; real proof re-imported `adobe.ico` with a different display name and returned existing `adobe-535ab004.ico` without increasing catalog count.
- CLI `status` reports core features ready, 542 icons, 19 categories, 6 restore records, shell integration `NotConfigured`, and setup actions for missing targets plus configuring shell integration.
- CLI `action-request configure-shell-integration` resolves to `ShowShellIntegrationPlan`.
- CLI `action-request review-missing-targets` resolves to `ShowRestoreHistory` with filter `Stale`.
- CLI `paths` reports existing Icon Library, Imported, AppData, and restore-state locations.
- CLI `open-request` enables real Icon Library, Imported, and Restore State paths with directory/file shell verbs.
- CLI `history` supports `all`, `restorable`, `applied`, `restored`, and `stale`; real proof shows 6 total records, 2 stale records, and 0 restorable records.
- CLI `menu` reports `Change icon...`, 19 categories, and 542/542 visible icons from `C:\Users\cristian\.icons`.
- CLI `menu-commands` reports `change-icon`, stable `icon:<hash>` commands, and argument templates for the same current menu snapshot.
- CLI `menu-invoke-preview change-icon C:\Users\cristian\.icons` reports final `change-icon --target ... --target-kind folder` arguments without mutation.
- CLI `menu-apply` changed and restored a temporary folder target using a current Icon Library catalog icon; after restore, `desktop.ini` no longer existed.
- `doctor` reports 19 categories, 542 icons, 0 warnings, 6 restore records, 0 restorable records, and 2 missing targets from earlier deleted temporary proof targets.

### Pre-IR-008.8: WinUI Home Snapshot

- Added `AppHomeService` and `AppHomeSnapshot` as the shared first-screen state for the future WinUI app.
- The snapshot composes setup readiness, dashboard counts, dynamic menu preview, filtered restore history, app locations, and refresh time.
- Connected CLI `home` and `app` to preview the same state without requiring WinUI runtime.
- Kept the app surface independent from WinUI controls while `winapp` is missing.

Proof:

- `AppHomeServiceTests.GetSnapshotComposesMainWindowState` passes.
- `AppHomeServiceTests.GetSnapshotHonorsHistoryFilterAndMenuCaps` passes.
- CLI `home` reports core features ready, shell integration `NotConfigured`, 542 icons, 19 categories, 542/542 menu icons, 6 restore records, 2 stale records, and app locations.
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
- CLI `activate` reports kind `Home`, core ready, shell integration `NotConfigured`, 542 icons, and 19 categories.
- CLI `activate change-icon --target C:\Users\cristian\.icons --target-kind folder` reports kind `ChangeIcon`, `Can continue: yes`, target kind `Folder`, and picker can open.

### Pre-IR-008.15: Activated Post-Picker Change Flow

- Added `ActivatedIconChangeService` to combine packaged app activation arguments with the `.ico` path returned by the picker.
- Added a non-mutating preview path for activated change flows.
- Added an apply path that uses `IconChangeService`, preserving import and restore-record behavior.
- Non-`change-icon` activations and invalid selected icons are rejected before mutation.
- Connected CLI `activate-preview` to preview post-picker readiness without writing target metadata.
- Connected CLI `activate-apply` to apply the activated post-picker flow through the same AppModel path.

Proof:

- `ActivatedIconChangeServiceTests.PreviewSelectedIconCombinesActivationAndIconDetailsWithoutApplying` passes.
- `ActivatedIconChangeServiceTests.ApplySelectedIconChangesFolderAndStoresRestoreRecord` passes.
- `ActivatedIconChangeServiceTests.PreviewSelectedIconRejectsHomeActivation` passes.
- `ActivatedIconChangeServiceTests.ApplySelectedIconRejectsHomeActivation` passes.
- `ActivatedIconChangeServiceTests.ApplySelectedIconRejectsUnsupportedTargetBeforeImport` passes.
- `ActivatedIconChangeServiceTests.ApplySelectedIconRejectsInvalidIconBeforeMutation` passes.
- CLI `activate-preview C:\Users\cristian\.icons\Adobe Creative\adobe.ico change-icon --target C:\Users\cristian\.icons --target-kind folder` reports `Can apply: yes`, target kind `Folder`, icon category `Adobe Creative`, and recommended image `256x256`.
- CLI `activate-apply` changed and restored a temporary folder target; after restore, `desktop.ini` no longer existed.

### Pre-IR-008.9: WinUI Icon Browser Snapshot

- Added `IconBrowserService` and browser snapshot models for the future WinUI catalog browser.
- Supports search text, category filter, max visible item cap, omitted counts, category summaries, and catalog warnings.
- Connected CLI `browse` and `icons` to preview the same browser state.

Proof:

- `IconBrowserServiceTests.BrowseFiltersBySearchAndCategory` passes.
- `IconBrowserServiceTests.BrowseCapsVisibleItems` passes.
- `IconBrowserServiceTests.BrowseReportsCatalogWarnings` passes.
- CLI `browse adobe --max 5` reports capped matching results from the current 542-icon library.
- CLI `browse --category "Developer Tools" --max 5` reports 5/30 shown from that category.
- CLI `browse --max 3` reports 3/542 shown and 539 omitted.

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
- CLI `recent` reports 6 real changes, 0 restore-enabled changes, and 6 disabled changes with reasons.
- CLI `recent stale` reports the 2 stale real changes with `Target missing` reasons.

### Pre-IR-008.19: WinUI Restore Preview Snapshot

- Added `IconRestorePreviewService` and `IconRestorePreviewSnapshot` for the future restore confirmation UI.
- Preview reads one restore record, returns previous-state detail, restore action detail, warning text, `CanRestore`, and a blocking error when disabled.
- Missing applied icon files remain warnings because restore uses the saved previous-state snapshot.
- Missing targets and non-`Applied` records disable the action before mutation.
- Connected CLI `restore-preview` and `preview-restore`.

Proof:

- `IconRestorePreviewServiceTests.PreviewRestoreAllowsAppliedFolderWithoutMutatingRecord` passes.
- `IconRestorePreviewServiceTests.PreviewRestoreWarnsWhenAppliedIconIsMissingButTargetCanRestore` passes.
- `IconRestorePreviewServiceTests.PreviewRestoreDisablesAlreadyRestoredRecord` passes.
- `IconRestorePreviewServiceTests.PreviewRestoreReportsMissingTarget` passes.
- `IconRestorePreviewServiceTests.PreviewRestoreMissingRecordReturnsPathNotFound` passes.
- CLI `restore-preview <record-id>` reports a disabled reason for current non-restorable real history records; enabled and warning paths are covered by isolated tests.

### Pre-IR-008.20: WinUI App Action Request Snapshot

- Added `SetupActionIds` constants so setup/home actions do not rely on loose string duplication.
- Added `AppActionRequestService`, `AppActionRequestSnapshot`, and action-kind models.
- Current setup actions resolve to stable WinUI workflow or navigation intents.
- Known actions that are not currently visible are disabled with a stale-action reason.
- Unknown action ids are rejected before navigation or mutation.
- Connected CLI `action-request` and `action`.

Proof:

- `AppActionRequestServiceTests.CreateRequestMapsImportIconsWhenLibraryIsEmpty` passes.
- `AppActionRequestServiceTests.CreateRequestMapsMissingTargetsToStaleHistory` passes.
- `AppActionRequestServiceTests.CreateRequestMapsConfigureShellIntegrationToShellPlan` passes.
- `AppActionRequestServiceTests.CreateRequestDisablesKnownActionThatIsNotCurrentlyAvailable` passes.
- `AppActionRequestServiceTests.CreateRequestRejectsUnknownAction` passes.
- CLI `action-request configure-shell-integration` resolves to `ShowShellIntegrationPlan`.
- CLI `action-request review-missing-targets` resolves to stale restore history.

### Pre-IR-008.21: WinUI Operation Feedback

- Added `AppOperationFeedbackService`, feedback severity, and feedback snapshot models.
- Apply and restore feedback return success titles plus a history action target.
- Batch import feedback distinguishes success, duplicate-only info, and partial-failure warnings.
- Error feedback preserves the error message and detail for UI display.
- CLI apply/restore now use the shared feedback titles.

Proof:

- `AppOperationFeedbackServiceTests.FromApplyCreatesSuccessFeedbackWithHistoryAction` passes.
- `AppOperationFeedbackServiceTests.FromRestoreCreatesShortcutRestoreFeedback` passes.
- `AppOperationFeedbackServiceTests.FromBatchImportReportsWarningsWhenAnyFileFails` passes.
- `AppOperationFeedbackServiceTests.FromBatchImportReportsInfoWhenEverythingWasReused` passes.
- `AppOperationFeedbackServiceTests.FromErrorUsesErrorMessageAndDetail` passes.
- CLI apply/restore title behavior remains compatible because feedback titles match the existing user-facing copy; latest proof added restored record `ea44f456-d157-473a-9b52-94a7293306be`.

### Pre-IR-008.22: WinUI Import Picker Request

- Added `IconImportPickerRequestService` and `IconImportPickerRequestSnapshot`.
- Import picker requests prepare the Icon Library and Imported folders before opening.
- Default requests target `.icons\Imported`, use the Icon Library as the initial directory, allow multiple selection, and restrict to `.ico`.
- Collection-targeted requests create or reuse a sanitized one-level collection before opening the picker.
- Invalid collection names are rejected before picker launch.
- Connected CLI `import-picker-request` and `import-picker`.

Proof:

- `IconImportPickerRequestServiceTests.CreateRequestBuildsMultiIconImportPicker` passes.
- `IconImportPickerRequestServiceTests.CreateRequestCreatesCollectionDestinationWhenRequested` passes.
- `IconImportPickerRequestServiceTests.CreateRequestRejectsCollectionNameThatSanitizesToEmpty` passes.
- CLI `import-picker-request` reports multi-select `.ico` metadata targeting `Imported`.
- CLI `import-picker-request "Test - Adobe Creative"` reports the existing collection as destination.

### Pre-IR-007.2: Shell Menu Command Descriptors

- Added `IconMenuCommandService`, `IconMenuCommandSnapshot`, and command models for the future native shell extension.
- Command descriptors are derived from `IconMenuService` snapshots so menu caps, categories, and visibility stay consistent.
- The first command is the stable `change-icon` picker command.
- Visible Icon Library entries get stable safe `icon:<hash>` ids and `menu-apply` argument templates.
- Truncated menus add an `open-app` overflow command instead of silently hiding the route to omitted icons.
- Connected CLI `menu-commands` and `shell-menu`.

Proof:

- `IconMenuCommandServiceTests.BuildCommandsIncludesChangeIconAndVisibleIconCommands` passes.
- `IconMenuCommandServiceTests.BuildCommandsCreatesStableSafeIconCommandIds` passes.
- `IconMenuCommandServiceTests.BuildCommandsAddsOpenAppOverflowCommandWhenMenuIsTruncated` passes.
- CLI `menu-commands` reports 543 commands for the real 542-icon library: `change-icon` plus one command per visible icon.

### Pre-IR-007.3: Shell Menu Command Invocation Preview

- Added `IconMenuCommandInvocationService` and invocation snapshot models for the future native shell extension.
- Invocation preview finds command descriptors in the current menu snapshot and rejects unknown/stale command ids.
- Target-required commands reuse `ShellSelectionService` and resolve `{target}` plus `{target-kind}` only for supported selections.
- Overflow `open-app` commands can be invoked without a target.
- The service returns final argument lists and display strings but does not mutate targets, launch apps, open pickers, or register Explorer.
- Connected CLI `menu-invoke-preview` and `shell-invoke-preview`.

Proof:

- `IconMenuCommandInvocationServiceTests.PreviewInvocationResolvesChangeIconArgumentsForSupportedFolder` passes.
- `IconMenuCommandInvocationServiceTests.PreviewInvocationResolvesIconCommandArgumentsForSupportedShortcut` passes.
- `IconMenuCommandInvocationServiceTests.PreviewInvocationDisablesTargetCommandForUnsupportedSelection` passes.
- `IconMenuCommandInvocationServiceTests.PreviewInvocationAllowsOpenAppOverflowWithoutTarget` passes.
- `IconMenuCommandInvocationServiceTests.PreviewInvocationRejectsUnknownCommandId` passes.
- CLI `menu-invoke-preview change-icon C:\Users\cristian\.icons` resolves final launch arguments without mutation.
- CLI `menu-invoke-preview change-icon C:\Users\cristian\.icons\Adobe Creative\adobe.ico` disables invocation with `UnsupportedTarget`.

### IR-007.1: Menu Empty, Large-Catalog, and Failure States

- Added `IconMenuState` and status/recommended-action text to menu snapshots.
- Empty libraries now report a clear import/open recovery action while preserving `Change icon...`.
- Truncated catalogs report bounded menu state and continue to route overflow through `open-app`.
- Catalog scan failures produce an unavailable menu snapshot with error details instead of dropping all shell commands.
- Command descriptors now add `open-app` for empty and unavailable menus, not only truncated menus.

Proof:

- `IconMenuServiceTests.BuildSnapshotReportsWarningsWhenInvalidIconsAreHidden` passes.
- `IconMenuServiceTests.BuildSnapshotReturnsUnavailableStateWhenCatalogCannotBeScanned` passes.
- `IconMenuCommandServiceTests.BuildCommandsAddsOpenAppCommandForEmptyMenu` passes.
- `IconMenuCommandServiceTests.BuildCommandsDegradesToChangeIconAndOpenAppWhenCatalogFails` passes.
- CLI `menu` reports state `Ready` for the real 542-icon library.
- CLI `menu-commands` reports state `Ready` and 363 commands for the real library.

### Pre-IR-008.11: WinUI Diagnostics Snapshot

- Added `AppDiagnosticsService` and diagnostic check models for the future WinUI diagnostics surface.
- Diagnostics compose setup readiness, dashboard counts, app locations, shell integration state, and WinUI tooling readiness.
- Added `WinUiToolingSnapshot` so the app can distinguish WinUI templates from `winapp` CLI availability.
- Connected CLI `diagnostics` and `diag` to passive tooling checks; it reports blockers but does not install or run WinUI.

Proof:

- `AppDiagnosticsServiceTests.GetDiagnosticsReportsReadyCoreAndPendingShell` passes.
- `AppDiagnosticsServiceTests.GetDiagnosticsReportsEmptyLibraryAndMissingWinApp` passes.
- `AppDiagnosticsServiceTests.GetDiagnosticsReportsCatalogWarnings` passes.
- CLI `diagnostics` reports 1 blocker (`winapp` missing), 1 warning (`NotConfigured` shell integration), WinUI templates available, 542 icons, and existing app locations.
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
- `IconCollectionServiceTests.CreateCollectionSanitizesNameAndUpdatesCatalogStatus` passes.
- `IconCollectionServiceTests.CreateCollectionIsIdempotent` passes.
- `IconCollectionServiceTests.CreateCollectionRejectsEmptyName` passes.
- `IconCollectionServiceTests.ListCollectionsIncludesEmptyCollectionsAndImported` passes.
- `IconCollectionImportServiceTests.ImportIntoCollectionCreatesCollectionAndCopiesValidIcons` passes.
- `IconCollectionImportServiceTests.ImportIntoCollectionReusesDuplicateBytesWithinCollection` passes.
- `IconCollectionImportServiceTests.ImportIntoCollectionReturnsPerFileFailures` passes.
- `IconCollectionImportServiceTests.ImportIntoCollectionRejectsNullSourceList` passes.
- CLI `batch-import` with two copies of `adobe.ico` reports 2 reused existing, 0 imported, 0 failed.
- Catalog remains stable after duplicate batch proofs because repeated icons are reused by content hash.
- CLI `collections` reports 19 real collections, including `Imported`, with per-collection icon counts.

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

### Pre-IR-008.23: WinUI Catalog Warning Review

- Added `CatalogWarningsService` and warning snapshot models for future WinUI diagnostics/library cleanup surfaces.
- Warning rows include path, display name, category context, validation code, message, and detail.
- The service is read-only: it reports skipped icons from the catalog scan without deleting files, importing icons, or mutating restore state.
- Connected CLI `catalog-warnings` and `warnings`.

Proof:

- `CatalogWarningsServiceTests.GetWarningsListsInvalidRootAndCategoryIcons` passes.
- `CatalogWarningsServiceTests.GetWarningsReturnsEmptySnapshotForCleanCatalog` passes.
- CLI `catalog-warnings` reports 19 categories, 542 icons, and 0 warnings for the real `C:\Users\cristian\.icons` library.

### Pre-IR-008.24: WinUI Accessibility Acceptance Plan

- Added `AccessibilityPlanService`, accessibility requirement models, and surface models for the future WinUI acceptance contract.
- The plan covers keyboard reachability, focus return, accessible names and semantics, persistent errors, high contrast, 200% scaling, long-path handling, and manual proof requirements.
- The plan lists 8 future WinUI surfaces with primary commands so implementation and QA can verify the same contract.
- Connected CLI `accessibility-plan` and `a11y-plan` to preview the contract before WinUI exists.
- Kept the service read-only and independent from WinUI controls, windows, app mutation, and visual-proof claims.

Proof:

- `AccessibilityPlanServiceTests.GetPlanIncludesKeyboardSemanticAndVisualRequirements` passes.
- `AccessibilityPlanServiceTests.GetPlanRequiresManualProofForRelease` passes.
- `AccessibilityPlanServiceTests.GetPlanListsCoreWinUiSurfacesAndCommands` passes.
- CLI `accessibility-plan` reports 15 required items, 5 manual proof items, and 9 app surfaces.

### Pre-IR-008.25: WinUI Navigation Plan

- Added `AppNavigationService`, route id constants, navigation route models, and navigation plan snapshot models.
- The plan defines `home` as the default route and `diagnostics` as the fallback route.
- The plan lists 13 future WinUI routes: 5 top-level screens plus workflow routes for import, details, restore preview, shell setup, shell bridge, package setup, accessibility proof, and Explorer-launched change-icon flow.
- Every current `AppActionKind` maps to a registered route id.
- `AppActionRequestService` now uses shared route constants instead of duplicating route strings.
- Connected CLI `navigation-plan` and `nav-plan` to preview the route contract before WinUI exists.

Proof:

- `AppNavigationServiceTests.GetPlanListsTopLevelWinUiRoutes` passes.
- `AppNavigationServiceTests.GetPlanRegistersEveryAppActionTarget` passes.
- `AppNavigationServiceTests.GetPlanIncludesWorkflowRoutesForShellRestoreAndProof` passes.
- CLI `navigation-plan` reports 13 routes, 5 top-level routes, default `home`, fallback `diagnostics`, and action targets for every current `AppActionKind`.

### Pre-IR-008.26: WinUI Window Startup State

- Added `AppWindowService` and `AppWindowSnapshot` as the shared startup-state source for the future WinUI `MainWindow`.
- The snapshot composes navigation routes, activation routing, and diagnostics into one state object.
- Normal startup selects `home`; valid Explorer `change-icon` activation selects `change-icon`.
- Unsupported `change-icon` targets stay on the `change-icon` route with a disabled reason so the UI can explain the failure.
- Malformed activation arguments fall back to `diagnostics` without opening pickers or mutating files.
- Connected CLI `app-window` and `window` to preview the future main-window startup state.

Proof:

- `AppWindowServiceTests.GetWindowWithoutActivationArgumentsSelectsHome` passes.
- `AppWindowServiceTests.GetWindowWithChangeIconActivationSelectsChangeIconRoute` passes.
- `AppWindowServiceTests.GetWindowKeepsChangeIconRouteForUnsupportedTarget` passes.
- `AppWindowServiceTests.GetWindowFallsBackToDiagnosticsForMalformedActivation` passes.
- CLI `app-window` reports selected route `home`, 13 routes, 5 top-level routes, current diagnostic blocker/warning badges, shell `NotConfigured`, WinUI templates available, and `winapp` missing.

### Pre-IR-008.27: WinUI Command State

- Added `AppCommandService`, command descriptor models, command state snapshots, and command kind enum.
- The command state exposes stable command ids, labels, kind, primary/secondary status, enabled state, target route or app location, and disabled reasons.
- Home route commands navigate to import, icon browser, history, and diagnostics.
- Diagnostics route commands include refresh, app data, and blocker review.
- `change-icon` enables choosing an icon for a valid shell target but keeps preview/apply disabled until an icon is selected and readiness is valid.
- Connected CLI `app-commands` and `commands` to preview future WinUI command bars.

Proof:

- `AppCommandServiceTests.GetCommandsForHomeIncludesPrimaryNavigation` passes.
- `AppCommandServiceTests.GetCommandsForDiagnosticsReflectsBlockingCount` passes.
- `AppCommandServiceTests.GetCommandsForChangeIconDisablesApplyUntilIconSelected` passes.
- `AppCommandServiceTests.GetCommandsForChangeIconWorkflowWithSelectedIconEnablesPreviewAndApply` passes.
- `AppCommandServiceTests.GetCommandsForUnsupportedChangeIconDisablesChooseIconWithReason` passes.
- `AppCommandServiceTests.GetCommandsRejectsUnknownRoute` passes.
- CLI `app-commands` reports 4 enabled home navigation commands.
- CLI `app-commands --route diagnostics` reports diagnostics refresh, app-data opening, and blocker review.
- CLI `app-commands change-icon --target C:\Users\cristian\.icons --target-kind folder` reports `choose-icon` enabled and preview/apply disabled.

### Pre-IR-008.28: WinUI Route View Composition

- Added `AppRouteViewService`, route content kind, and route view snapshot models.
- The route view composes app-window state, registered route metadata, command state, and route content into one renderable snapshot.
- Ready content is provided for home, icon browser, history, diagnostics, shell plan, package plan, accessibility plan, and later route-specific workflow surfaces.
- Workflow routes that still lack their required selection/input remain non-mutating placeholders.
- Connected CLI `app-view` and `view` to preview the future renderable route state.

Proof:

- `AppRouteViewServiceTests.GetViewForHomeComposesHomeContentAndCommands` passes.
- `AppRouteViewServiceTests.GetViewForDiagnosticsReflectsToolingBlockers` passes.
- `AppRouteViewServiceTests.GetViewForPackagePlanUsesWindowTooling` passes.
- `AppRouteViewServiceTests.GetViewRejectsUnknownRoute` passes.
- CLI `app-view` reports home content ready with 542 icons, 19 categories, and 6 history records.
- CLI `app-view --route icon-browser` reports browser content ready with 200 visible icons from 542 matches.
- CLI `app-view --route package-plan` reports 6 package blockers and 2 warnings when full packaging inputs are supplied to the route view.
- CLI `app-view` initially kept workflow-only routes non-mutating until route-specific workflow content was added in later slices.

### Pre-IR-008.29: Change Icon Workflow State

- Added `AppChangeIconWorkflowService`, workflow step enum, and workflow snapshot models.
- Valid `change-icon` activation without a selected icon reports `NeedIcon`, picker readiness, target status, and picker initial directory.
- Valid activation with a selected `.ico` reports `ReadyToApply`, icon preview details, and apply readiness without mutation.
- Unsupported selected targets and invalid selected icons report `Blocked` with stable reasons.
- Connected CLI `change-icon-workflow` and `change-workflow` to preview the non-mutating workflow.

Proof:

- `AppChangeIconWorkflowServiceTests.GetWorkflowForValidActivationWaitsForIconSelection` passes.
- `AppChangeIconWorkflowServiceTests.GetWorkflowWithSelectedIconIsReadyToApplyWithoutMutating` passes.
- `AppChangeIconWorkflowServiceTests.GetWorkflowBlocksUnsupportedTargetBeforePicker` passes.
- `AppChangeIconWorkflowServiceTests.GetWorkflowWithInvalidIconKeepsTargetUnchanged` passes.
- `AppChangeIconWorkflowServiceTests.GetWorkflowRejectsHomeActivation` passes.
- CLI `change-icon-workflow change-icon --target C:\Users\cristian\.icons --target-kind folder` reports `NeedIcon` and picker readiness.
- CLI `change-icon-workflow --icon C:\Users\cristian\.icons\Adobe Creative\adobe.ico change-icon --target C:\Users\cristian\.icons --target-kind folder` reports `ReadyToApply` with selected icon details.
- CLI `change-icon-workflow change-icon --target C:\Users\cristian\.icons\Adobe Creative\adobe.ico --target-kind shortcut` reports `Blocked` with unsupported-target reason and exit code 65.

### Pre-IR-008.30: Restore Workflow State

- Added `AppRestoreWorkflowService`, workflow step enum, and workflow snapshot models.
- The workflow composes restore history with an optional selected record preview.
- No selected record reports `NeedRecord` so the future WinUI route can render history and wait for a selection.
- Applied records report `ReadyToRestore`; already restored, missing, or stale selected records report `Blocked` with stable reasons.
- Missing selected records return a blocked workflow snapshot while preserving loaded history.
- Connected CLI `restore-workflow` and `restore-flow` to preview the non-mutating workflow.

Proof:

- `AppRestoreWorkflowServiceTests.GetWorkflowWithoutSelectedRecordWaitsForSelection` passes.
- `AppRestoreWorkflowServiceTests.GetWorkflowWithAppliedRecordIsReadyToRestore` passes.
- `AppRestoreWorkflowServiceTests.GetWorkflowBlocksAlreadyRestoredRecord` passes.
- `AppRestoreWorkflowServiceTests.GetWorkflowBlocksMissingRecordAfterLoadingHistory` passes.
- `AppRestoreWorkflowServiceTests.GetWorkflowKeepsMissingAppliedIconRestorableWithWarning` passes.
- CLI `restore-workflow` reports `NeedRecord`, 6 real history records, 0 restorable records, and 2 stale records.
- CLI `restore-workflow ea44f456-d157-473a-9b52-94a7293306be` reports `Blocked`, selected-record preview details, and the already-restored reason without mutation.

### Pre-IR-008.31: Restore Route View Composition

- Extended route view snapshots with `RestoreWorkflow` content.
- `restore-preview` now renders `RestoreWorkflow` content instead of a generic workflow placeholder.
- Extended app command composition so `confirm-restore`, `cancel-restore`, and `review-disabled-reason` reflect selected restore workflow state.
- Added optional `--record` and `--filter` support to CLI `app-view` and `app-commands` for the restore route.
- Kept the route non-mutating: previewing route content and commands does not restore targets or update restore history.

Proof:

- `AppRouteViewServiceTests.GetViewForRestorePreviewWithoutRecordComposesNeedRecordWorkflow` passes.
- `AppRouteViewServiceTests.GetViewForRestorePreviewWithAppliedRecordEnablesConfirmRestore` passes.
- `AppCommandServiceTests.GetCommandsForReadyRestoreWorkflowEnablesConfirmRestore` passes.
- `AppCommandServiceTests.GetCommandsForBlockedRestoreWorkflowKeepsReasonActionAvailable` passes.
- CLI `app-view --route restore-preview` reports content kind `RestoreWorkflow`, content ready, and `NeedRecord`.
- CLI `app-view --route restore-preview --record ea44f456-d157-473a-9b52-94a7293306be` reports `Blocked`, selected-record preview details, and disabled restore action detail.
- CLI `app-commands --route restore-preview --record ea44f456-d157-473a-9b52-94a7293306be` disables `confirm-restore` and enables `review-disabled-reason`.

### Pre-IR-008.32: Collections Route View Composition

- Extended route view snapshots with collections content.
- `collections` now renders one-level Icon Library folders instead of a generic workflow placeholder.
- The route reports collection count, total icon count, and whether `Imported` is present.
- The route reuses the existing collection commands for create/import/open actions without creating folders or importing icons.

Proof:

- `AppRouteViewServiceTests.GetViewForCollectionsComposesCollectionContentAndCommands` passes.
- CLI `app-view --route collections` reports content kind `Collections`, content ready, 19 collections, 542 collection icons, and Imported present.

### Pre-IR-008.33: Change Icon Route View Composition

- Extended route view snapshots with `ChangeIconWorkflow` content.
- `change-icon` now renders Explorer-launched workflow content instead of a generic workflow placeholder when activation arguments are present.
- Extended app command composition so `choose-icon`, `preview-change`, and `apply-change` reflect the selected icon workflow state.
- Added optional `--icon` support to CLI `app-view` and `app-commands` so selected-icon readiness can be previewed without mutation.
- Kept the route non-mutating: previewing route content and commands does not import icons, write `desktop.ini`, edit `.lnk` files, or create restore history.

Proof:

- `AppRouteViewServiceTests.GetViewForChangeIconComposesNeedIconWorkflow` passes.
- `AppRouteViewServiceTests.GetViewForChangeIconWithSelectedIconEnablesApplyWithoutMutation` passes.
- `AppCommandServiceTests.GetCommandsForChangeIconWorkflowWithSelectedIconEnablesPreviewAndApply` passes.
- CLI `app-view change-icon --target C:\Users\cristian\.icons --target-kind folder` reports content kind `ChangeIconWorkflow`, content ready, `NeedIcon`, picker readiness, and disabled apply.
- CLI `app-view --icon C:\Users\cristian\.icons\Folder11 - Adobe Creative Suite\adobe.ico change-icon --target C:\Users\cristian\.icons --target-kind folder` reports `ReadyToApply`, preview readiness, and enabled apply without mutation.

### Pre-IR-008.34: Import Icons Route View Composition

- Extended route view snapshots with `ImportPickerRequest` content.
- `import-icons` now renders the app import picker destination instead of a generic workflow placeholder.
- The route reports picker title, initial directory, destination collection, destination directory, `.ico` filter, and multi-select state.
- Added optional `--collection` support to CLI `app-view` so collection import destinations can be previewed before opening a native picker.
- Kept the route non-mutating for icons: previewing route content does not copy selected icons, validate selected files, or create restore history.

Proof:

- `AppRouteViewServiceTests.GetViewForImportIconsComposesPickerRequestAndCommands` passes.
- `AppRouteViewServiceTests.GetViewForImportIconsWithCollectionUsesCollectionDestination` passes.
- CLI `app-view --route import-icons` reports content kind `ImportIcons`, content ready, Imported destination, multi-select enabled, and `.ico` filter.
- CLI `app-view --route import-icons --collection "Folder11 - Developer Web Stack"` reports that collection as the destination without copying icons.

### Pre-IR-008.35: Icon Details Route View Composition

- Extended route view snapshots with `IconDetails` content.
- `icon-details` now renders selected `.ico` metadata when `--icon` is supplied.
- The route reports display name, category, library membership, image count, and recommended image.
- Extended app command composition so `copy-icon-path` and `open-containing-folder` become enabled when selected-icon details exist.
- Kept the route non-mutating: previewing route content and commands does not import icons, open folders, apply icons, or create restore history.

Proof:

- `AppRouteViewServiceTests.GetViewForIconDetailsComposesSelectedIconDetailsAndCommands` passes.
- `AppRouteViewServiceTests.GetViewForIconDetailsWithoutSelectedIconReturnsPlaceholder` passes.
- `AppCommandServiceTests.GetCommandsForIconDetailsWithSelectedIconEnablesCopyAndOpenActions` passes.
- CLI `app-view --route icon-details --icon C:\Users\cristian\.icons\Folder11 - Adobe Creative Suite\adobe.ico` reports content kind `IconDetails`, category `Folder11 - Adobe Creative Suite`, 8 images, and recommended image `256x256`.
- CLI `app-view --route icon-details` reports a non-ready placeholder with disabled commands and the select-icon reason.

### Pre-IR-008.36: Filtered Icon Browser Route View Composition

- Extended `AppRouteViewService` so `icon-browser` route composition accepts shared `IconBrowserOptions`.
- Added optional `--search`, `--category`, and `--max` support to CLI `app-view`.
- The `icon-browser` route now reports the same browser state a WinUI view will need: search text, category, visible/matched/total counts, omitted count, and category count.
- Kept the route non-mutating: previewing filtered browser content only scans the Icon Library and does not import icons, open folders, apply icons, or create restore history.

Proof:

- `AppRouteViewServiceTests.GetViewForIconBrowserAppliesSearchAndCategoryOptions` passes.
- `AppRouteViewServiceTests.GetViewForIconBrowserReportsCappedVisibleItems` passes.
- CLI `app-view --route icon-browser --search adobe --max 5` reports filtered browser route content with search text and omitted count.
- CLI `app-view --route icon-browser --category "Folder11 - Developer Web Stack" --max 5` reports category-filtered route content without mutation.

### Pre-IR-008.37: Package Plan Setup Action Composition

- Extended setup readiness so package-plan blockers can surface as first-screen setup actions when packaging/tooling inputs are available.
- Added stable `review-package-plan` setup action id and routed it through `AppActionRequestService` to the `package-plan` route.
- Registered `AppActionKind.ShowPackagePlan` in the navigation plan.
- Connected CLI `status`, `home`, and `action-request` to pass detected WinUI tooling into setup readiness without installing or configuring anything.
- Kept Explorer integration gated: the action only navigates to planning/readiness state and does not register shell integration, build packages, or open installers.

Proof:

- `SetupReadinessServiceTests.GetSnapshotAddsPackagePlanActionWhenModernPackagePathIsBlocked` passes.
- `AppActionRequestServiceTests.CreateRequestMapsReviewPackagePlanToPackagePlanRoute` passes.
- `AppHomeServiceTests.GetSnapshotIncludesPackagePlanActionWhenPackagingInputsAreBlocked` passes.
- CLI `status` reports `review-package-plan` as a blocking action while `winapp` is missing.
- CLI `action-request review-package-plan` resolves to `ShowPackagePlan` and `package-plan`.

### Pre-IR-010: Release Readiness Evidence Gate

- Added `ReleaseReadinessService`, release readiness inputs, release readiness items, and release readiness snapshot models.
- The snapshot composes build proof, test proof, CLI proof, diagnostics, package plan, accessibility proof, manual Explorer proof, and release evidence packet gates.
- The service reuses diagnostics, package-plan, and accessibility-plan snapshots instead of duplicating those checks.
- Connected CLI `release-readiness` and `release-evidence` to preview the release gate without installing packages, registering Explorer, opening windows, or mutating icon state.
- Added proof flags for future captured evidence: `--build`, `--tests`, `--cli-proof`, `--manual-explorer-proof`, `--accessibility-proof`, `--release-evidence`, package identity/native extension/signing/installer/install/uninstall flags.

Proof:

- `ReleaseReadinessServiceTests.GetReadinessReportsMissingReleaseEvidenceAndPackageBlockers` passes.
- `ReleaseReadinessServiceTests.GetReadinessPassesWhenAllRequiredProofAndPackageGatesAreProvided` passes.
- CLI `release-readiness` reports not ready while `winapp`, package identity, native extension, signing, installer, manual Explorer proof, accessibility proof, and release evidence are missing.

### Pre-IR-007.4: Packaged Direct Menu Apply Activation

- Added `AppMenuApplyActivationService` and `AppMenuApplyActivationSnapshot` for non-mutating packaged-app `menu-apply <target> <icon-from-library.ico>` activation previews.
- Added `ActivatedMenuApplyService` and `ActivatedMenuApplyResult` so a direct submenu activation can apply through the existing `IconMenuApplyService`.
- Extended `AppActivationService` with `AppActivationKind.MenuApply` while keeping `change-icon` and Home routing unchanged.
- Extended CLI `activate` to preview `menu-apply` activation arguments.
- Added CLI `activate-menu-apply` for packaged-app direct submenu apply proof before Explorer registration.
- Kept Explorer integration gated: this only parses, previews, and applies through existing AppModel services without registering context menus.

Proof:

- `AppMenuApplyActivationServiceTests.PreviewActivationResolvesDirectMenuApplyWithoutMutatingTarget` passes.
- `AppMenuApplyActivationServiceTests.PreviewActivationDisablesUnsupportedTargetWithoutMutation` passes.
- `AppMenuApplyActivationServiceTests.PreviewActivationDisablesIconOutsideCurrentMenu` passes.
- `ActivatedMenuApplyServiceTests.ApplyActivationAppliesLibraryIconAndStoresRestoreRecord` passes.
- `ActivatedMenuApplyServiceTests.ApplyActivationRejectsIconOutsideLibraryBeforeMutation` passes.
- `AppActivationServiceTests.ActivateWithMenuApplyArgumentsReturnsMenuApplyRequest` passes.

### Pre-IR-008.38: Shell Bridge Route View Composition

- Added `shell-bridge` as a registered AppModel route for the future WinUI setup surface.
- Added shell-bridge route commands for resolved-command review, safety-rule review, shell-plan navigation, and diagnostics.
- Extended `AppRouteViewService` snapshots with `ShellExtensionBridgeSnapshot` content.
- Extended CLI `app-view --route shell-bridge --shell-target <target>` to preview bridge protocol, CLSID, menu state, target status, command counts, invocable counts, omitted icon counts, and safety-rule count.
- Added the route to the accessibility surface list so keyboard/persistent-error proof covers the bridge panel.
- Kept Explorer registration gated: this only composes an app route over the existing bridge contract and does not register, launch, or mutate Explorer state.

Proof:

- `AppRouteViewServiceTests.GetViewForShellBridgeComposesSupportedTargetWithoutMutation` passes.
- `AppCommandServiceTests.GetCommandsForShellPlanLinksToShellBridge` passes.
- `AppCommandServiceTests.GetCommandsForShellBridgeIncludesReviewAndNavigation` passes.
- `AppNavigationServiceTests.GetPlanIncludesWorkflowRoutesForShellRestoreAndProof` covers the registered `shell-bridge` route.

### Pre-IR-008.39: Package Plan Route Native Tooling Composition

- Extended `AppRouteViewService.GetView` and `GetViewFromEnvironment` with optional `PackagingPlanInputs`.
- The Home and Package Plan route views now use full packaging inputs when available instead of deriving package state only from WinUI tooling.
- Extended CLI `app-view` to pass `DetectPackagingInputs()`, including native build-tool checks.
- Kept the fallback path for callers that only have `WinUiToolingSnapshot`.

Proof:

- `AppRouteViewServiceTests.GetViewForPackagePlanUsesPackagingInputsWhenProvided` passes and covers `native-build-tools` blocking.
- CLI `app-view --route package-plan` reports packaging blockers from full packaging inputs, including package identity, signing, and installer package when those proofs are missing.

### Pre-IR-008.40: Native Build Tooling Diagnostics

- Extended `AppDiagnosticsSnapshot` with `NativeToolingSnapshot`.
- Extended `AppDiagnosticsService` with a `native-build-tools` check.
- Extended CLI `diagnostics` to detect `cl.exe`, Visual Studio MSBuild, and CMake.
- Extended `ReleaseReadinessService` diagnostics to reuse native tooling from packaging inputs.
- Extended `AppRouteViewService` diagnostics route to consume native tooling from full packaging inputs when available.

Proof:

- `AppDiagnosticsServiceTests.GetDiagnosticsReportsEmptyLibraryAndMissingWinApp` covers `native-build-tools` blocking.
- `AppRouteViewServiceTests.GetViewForDiagnosticsReflectsToolingBlockers` covers diagnostics route blockers for missing `winapp` plus missing native build tools.
- CLI `diagnostics` reports native build-tool availability separately from WinUI tooling; current setup proof reports native build tools available.

### Pre-IR-008.41: WinUI Command Request Routing

- Added `AppCommandRequestService` and `AppCommandRequestSnapshot`.
- The service resolves enabled route commands into non-mutating WinUI intents: navigation target, location open request, refresh request, or workflow id.
- Disabled commands return a stable disabled request with the command's blocking reason.
- Connected CLI `app-command-request` and `command-request` to preview command behavior before WinUI exists.
- Kept requests passive: they do not open folders, refresh views, open pickers, apply icons, restore records, or navigate windows.

Proof:

- `AppCommandRequestServiceTests.CreateRequestResolvesNavigationCommand` passes.
- `AppCommandRequestServiceTests.CreateRequestResolvesOpenLocationCommandWithoutOpeningIt` passes.
- `AppCommandRequestServiceTests.CreateRequestReportsDisabledCommandReason` passes.
- `AppCommandRequestServiceTests.CreateRequestResolvesRefreshCommand` passes.
- `AppCommandRequestServiceTests.CreateRequestRejectsCommandNotOnRoute` passes.
- CLI `app-command-request import-icons` reports navigation to `import-icons`.
- CLI `app-command-request open-icon-library --route collections` reports an `IconLibrary` open request without opening Explorer.
- CLI `app-command-request open-icon-details --route icon-browser` reports a disabled workflow with `Select an icon before opening details.` and exit code 64.

### Tooling Setup: WinUI and Native Build Tools

- Added `C:\Users\cristian\AppData\Local\Microsoft\WindowsApps` to the user PATH so App Installer aliases such as `winget` and `winapp` are discoverable after shell refresh.
- Installed Windows App Development CLI `Microsoft.WinAppCli` 0.4.0.
- Refreshed `Microsoft.WindowsAppSDK.WinUI.CSharp.Templates`.
- Confirmed Developer Mode is already enabled.
- Confirmed Visual Studio Build Tools are already installed and added `scripts\Initialize-NativeToolchain.ps1` to load `VsDevCmd` for native shell-extension builds.
- Hardened CLI tooling detection so `diagnostics` and `package-plan` can find `winapp` in WindowsApps and native tools through Visual Studio Build Tools even when the current process PATH is stale.

Proof:

- `winapp --version` reports `0.4.0`.
- `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Initialize-NativeToolchain.ps1 -PassThru` reports `cl.exe`, `MSBuild.exe`, and `cmake.exe`.
- CLI `diagnostics` now reports 0 blockers, 1 warning, `winapp CLI: available`, and `Native build tools: available`.
- CLI `package-plan` now reports 3 product blockers and 2 proof warnings; `winapp`, `native-build-tools`, and `native-shell-extension` pass.

### IR-008.0: Packaged WinUI App Shell

- Created `src\IconReplacer.App` from the WinUI MVVM template and added it to `IconReplacer.slnx`.
- Added a project reference to `IconReplacer.AppModel` so the app reads the same snapshots already proven by CLI.
- Replaced the template counter page with a compact `NavigationView` shell for Home, Icons, Recent, Diagnostics, and Package.
- Added real AppModel-backed metrics, route rows, package gates, diagnostic checks, icon-browser rows, and recent history rows.
- Added `BuildAndRun.ps1` at the repo root for the standard WinUI build/run workflow.
- Aligned the package manifest with `IconReplacer` package name and `IconReplacer.App` application id while keeping Explorer registration out of the manifest for this slice.
- Initial shell slice kept mutation-heavy actions scoped out; Open Library and Refresh were active first, with import/restore wired in the next slice.

Proof:

- `dotnet build src\IconReplacer.App\IconReplacer.App.csproj -p:Platform=x64` passes.
- `dotnet build IconReplacer.slnx` passes with the WinUI app included.
- `dotnet test IconReplacer.slnx --no-build` passes, 235 tests.
- `.\BuildAndRun.ps1 src\IconReplacer.App\IconReplacer.App.csproj -Detach` builds and launches through `winapp`.
- Launch proof returned AUMID `IconReplacer_2wx6x5nenbha0!IconReplacer.App` and PID `41148`; `Get-Process` showed `IconReplacer.App` with window title `Icon Replacer`.

### IR-008.2 / IR-008.4: WinUI Import and Restore Actions

- Re-ran the WinUI setup checks from `winui-setup`: `.NET SDK 10.0.300`, WinUI templates, Developer Mode, `winget`, and `winapp 0.4.0` are available after PATH refresh.
- Refreshed `Microsoft.WindowsAppSDK.WinUI.CSharp.Templates`; `winget upgrade --id Microsoft.WinAppCLI` currently reports no matching package id, but the installed `winapp` binary is functional at version `0.4.0`.
- Wired the WinUI `Import` command to a real native Windows `.ico` file dialog owned by `App.WindowHandle`, importing multi-select results into `.icons\Imported` through `IconLibraryService`.
- Added a `Choose icons` action on the Import route destination row.
- Added row-level `Restore` actions to Home and History recent-change lists. Non-restorable records show a disabled Restore button with the existing row status; restorable records call `IconRestoreService.Restore` and refresh History.
- Routed import/restore/error outcomes through `AppOperationFeedbackService` into the persistent `InfoBar`.
- Hardened Open Library feedback so shell-launch failures are reported inline instead of escaping the command.
- Extended the row template with semantic `Button` actions while keeping the `ListView` virtualization-friendly layout.

Proof:

- `dotnet build src\IconReplacer.App\IconReplacer.App.csproj -p:Platform=x64` passes.
- `dotnet build IconReplacer.slnx` passes.
- `dotnet test IconReplacer.slnx --no-build` passes, 235 tests.
- `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Initialize-NativeToolchain.ps1 -PassThru` reports `cl.exe`, `MSBuild.exe`, and `cmake.exe`.
- `.\BuildAndRun.ps1 src\IconReplacer.App\IconReplacer.App.csproj -Detach` builds and launches through `winapp`.
- Launch proof returned AUMID `IconReplacer_2wx6x5nenbha0!IconReplacer.App` and PID `38760`; `Get-Process` showed `IconReplacer.App` with window title `Icon Replacer` and `Responding: True`.
- `winapp ui inspect -w 25169266 --depth 6` found `AppNavigation`, `PageCommandBar`, `ImportIconsButton`, `OpenLibraryButton`, `StatusInfoBar`, `PrimaryRowsList`, and row-level `Restore` buttons.
- `winapp ui search "Import" -a IconReplacer.App` found `ImportIconsButton`.
- `winapp ui search "StatusInfoBar" -a IconReplacer.App` found `StatusInfoBar`.
- Pending `change-icon` activation proof opens the modal `Change icon` file dialog; final manual selection/apply proof remains part of Windows interaction evidence.

### IR-006.1: Native IExplorerCommand Build and Packaged Change Icon Activation

- Added `src\IconReplacer.ShellExtension`, a native x64 COM `IExplorerCommand` DLL with `DllGetClassObject` and `DllCanUnloadNow`.
- Added `scripts\Build-NativeShellExtension.ps1` and wired `IconReplacer.App.csproj` so the native DLL builds and copies into the packaged app layout as `IconReplacer.ShellExtension.dll`.
- Added real `windows.comServer` and `windows.fileExplorerContextMenus` entries to `Package.appxmanifest` under the packaged `Application` extension block.
- Registered `Directory` and `.lnk` manifest targets to the stable CLSID `B8F1A86D-4C52-4C53-BF72-30B59F7F0F7D`.
- Implemented packaged activation via a pending-activation file in `%AppData%\Icon Replacer\pending-activation.args`, then opens the app without fragile command-line activation.
- Added startup parsing for direct `change-icon` args and pending activation args.
- Replaced the activation picker with a native Windows file dialog owned by the WinUI window handle, and deferred the activation workflow until after `Loaded` so the modal dialog opens reliably.
- Verified that the pending `change-icon --target <folder> --target-kind folder` flow opens a modal `Change icon` dialog in the packaged app.
- Kept dynamic submenu enumeration and final Explorer registration proof open; the native command currently implements the first `Change icon...` path.

Proof:

- `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Build-NativeShellExtension.ps1 -Configuration Debug -Platform x64` builds `artifacts\native\x64\Debug\IconReplacer.ShellExtension.dll`.
- `dumpbin /exports artifacts\native\x64\Debug\IconReplacer.ShellExtension.dll` shows `DllCanUnloadNow` and `DllGetClassObject`.
- `dotnet build src\IconReplacer.App\IconReplacer.App.csproj -p:Platform=x64` builds the WinUI app and native shell extension together.
- `dotnet build IconReplacer.slnx` passes.
- `dotnet test IconReplacer.slnx --no-build` passes, 235 tests.
- `winapp run <app-output> --detach --json` with a pending `change-icon` activation launches AUMID `IconReplacer_2wx6x5nenbha0!IconReplacer.App`.
- `winapp ui list-windows -a IconReplacer.App` finds two windows: `Icon Replacer` and modal dialog `Change icon`.
- `dotnet run --no-build --project src\IconReplacer.Cli -- package-plan` now passes the `Native shell extension built` gate and reports 3 remaining blockers: package identity, development signing, and installer build.

## Latest Verification

```powershell
dotnet build IconReplacer.slnx
dotnet test IconReplacer.slnx --no-build
dotnet build src\IconReplacer.App\IconReplacer.App.csproj -p:Platform=x64
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Build-NativeShellExtension.ps1 -Configuration Debug -Platform x64
.\BuildAndRun.ps1 src\IconReplacer.App\IconReplacer.App.csproj -Detach
winapp ui list-windows -a IconReplacer.App
winapp ui inspect -w <hwnd> --depth 6
winapp ui search "Import" -a IconReplacer.App
winapp ui search "StatusInfoBar" -a IconReplacer.App
winapp ui list-windows -a IconReplacer.App # after pending change-icon activation, should include modal "Change icon"
winapp --version
dotnet new install Microsoft.WindowsAppSDK.WinUI.CSharp.Templates
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Initialize-NativeToolchain.ps1 -PassThru
dotnet run --no-build --project src\IconReplacer.Cli -- diagnostics
dotnet run --no-build --project src\IconReplacer.Cli -- shell-plan
dotnet run --no-build --project src\IconReplacer.Cli -- shell-manifest
dotnet run --no-build --project src\IconReplacer.Cli -- shell-bridge C:\Users\cristian\.icons
dotnet run --no-build --project src\IconReplacer.Cli -- package-plan
dotnet run --no-build --project src\IconReplacer.Cli -- release-readiness
dotnet run --no-build --project src\IconReplacer.Cli -- accessibility-plan
dotnet run --no-build --project src\IconReplacer.Cli -- navigation-plan
dotnet run --no-build --project src\IconReplacer.Cli -- app-window
dotnet run --no-build --project src\IconReplacer.Cli -- app-commands
dotnet run --no-build --project src\IconReplacer.Cli -- app-command-request import-icons
dotnet run --no-build --project src\IconReplacer.Cli -- app-command-request open-icon-library --route collections
dotnet run --no-build --project src\IconReplacer.Cli -- app-command-request open-icon-details --route icon-browser
dotnet run --no-build --project src\IconReplacer.Cli -- app-commands --icon "C:\Users\cristian\.icons\Folder11 - Adobe Creative Suite\adobe.ico" change-icon --target "C:\Users\cristian\.icons" --target-kind folder
dotnet run --no-build --project src\IconReplacer.Cli -- app-commands --route restore-preview --record <record-id>
dotnet run --no-build --project src\IconReplacer.Cli -- app-view
dotnet run --no-build --project src\IconReplacer.Cli -- app-view --route icon-browser --search adobe --max 5
dotnet run --no-build --project src\IconReplacer.Cli -- app-view --route icon-browser --category "Folder11 - Developer Web Stack" --max 5
dotnet run --no-build --project src\IconReplacer.Cli -- app-view --route icon-details --icon "C:\Users\cristian\.icons\Folder11 - Adobe Creative Suite\adobe.ico"
dotnet run --no-build --project src\IconReplacer.Cli -- app-view --route icon-details
dotnet run --no-build --project src\IconReplacer.Cli -- app-view change-icon --target "C:\Users\cristian\.icons" --target-kind folder
dotnet run --no-build --project src\IconReplacer.Cli -- app-view --icon "C:\Users\cristian\.icons\Folder11 - Adobe Creative Suite\adobe.ico" change-icon --target "C:\Users\cristian\.icons" --target-kind folder
dotnet run --no-build --project src\IconReplacer.Cli -- app-view --route import-icons
dotnet run --no-build --project src\IconReplacer.Cli -- app-view --route import-icons --collection "Folder11 - Developer Web Stack"
dotnet run --no-build --project src\IconReplacer.Cli -- app-view --route collections
dotnet run --no-build --project src\IconReplacer.Cli -- app-view --route package-plan
dotnet run --no-build --project src\IconReplacer.Cli -- app-view --route shell-bridge --shell-target C:\Users\cristian\.icons
dotnet run --no-build --project src\IconReplacer.Cli -- app-view --route restore-preview
dotnet run --no-build --project src\IconReplacer.Cli -- app-view --route restore-preview --record <record-id>
dotnet run --no-build --project src\IconReplacer.Cli -- change-icon-workflow change-icon --target "C:\Users\cristian\.icons" --target-kind folder
dotnet run --no-build --project src\IconReplacer.Cli -- action-request configure-shell-integration
dotnet run --no-build --project src\IconReplacer.Cli -- action-request review-package-plan
dotnet run --no-build --project src\IconReplacer.Cli -- activate
dotnet run --no-build --project src\IconReplacer.Cli -- activate change-icon --target "C:\Users\cristian\.icons" --target-kind folder
dotnet run --no-build --project src\IconReplacer.Cli -- activate menu-apply "C:\Users\cristian\.icons" "C:\Users\cristian\.icons\Folder11 - Adobe Creative Suite\adobe.ico"
dotnet run --no-build --project src\IconReplacer.Cli -- activate-preview "C:\Users\cristian\.icons\Adobe Creative\adobe.ico" change-icon --target "C:\Users\cristian\.icons" --target-kind folder
dotnet run --no-build --project src\IconReplacer.Cli -- activate-apply <icon.ico> change-icon --target <temporary-folder> --target-kind folder
dotnet run --no-build --project src\IconReplacer.Cli -- activate-menu-apply <temporary-folder> <icon-from-library.ico>
dotnet run --no-build --project src\IconReplacer.Cli -- home
dotnet run --no-build --project src\IconReplacer.Cli -- home stale
dotnet run --no-build --project src\IconReplacer.Cli -- browse adobe --max 5
dotnet run --no-build --project src\IconReplacer.Cli -- browse --category "Developer Tools" --max 5
dotnet run --no-build --project src\IconReplacer.Cli -- details "C:\Users\cristian\.icons\Adobe Creative\adobe.ico"
dotnet run --no-build --project src\IconReplacer.Cli -- preview-change "C:\Users\cristian\.icons" "C:\Users\cristian\.icons\Adobe Creative\adobe.ico"
dotnet run --no-build --project src\IconReplacer.Cli -- recent
dotnet run --no-build --project src\IconReplacer.Cli -- recent stale
dotnet run --no-build --project src\IconReplacer.Cli -- restore-workflow
dotnet run --no-build --project src\IconReplacer.Cli -- restore-workflow <record-id>
dotnet run --no-build --project src\IconReplacer.Cli -- restore-preview <record-id>
dotnet run --no-build --project src\IconReplacer.Cli -- batch-import "C:\Users\cristian\.icons\Adobe Creative\adobe.ico" "C:\Users\cristian\.icons\Adobe Creative\adobe.ico"
dotnet run --no-build --project src\IconReplacer.Cli -- import-picker-request
dotnet run --no-build --project src\IconReplacer.Cli -- catalog-warnings
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
dotnet run --no-build --project src\IconReplacer.Cli -- menu-commands
dotnet run --no-build --project src\IconReplacer.Cli -- menu-invoke-preview change-icon C:\Users\cristian\.icons
```

Result:

- Build: pass, 0 warnings, 0 errors.
- Tests: pass, 235 total.
- WinUI app build: pass, `IconReplacer.App` builds for x64.
- WinUI app launch: pass, packaged app launched through `winapp` with AUMID `IconReplacer_2wx6x5nenbha0!IconReplacer.App`, PID `38760`, and window title `Icon Replacer`.
- WinUI UIA inspection: pass, `ImportIconsButton`, `StatusInfoBar`, `PrimaryRowsList`, and row-level `Restore` buttons are present in the running app.
- WinUI setup: pass, `winapp` 0.4.0 is installed and WinUI templates are refreshed; `winget` does not currently find package id `Microsoft.WinAppCLI` for upgrade, so the installed CLI binary was verified directly.
- WinUI import/restore wiring: build/UIA pass; import uses a native Windows file dialog owned by the WinUI window.
- Packaged Change Icon activation: pass, pending `change-icon` activation launches the packaged app and opens a modal `Change icon` file dialog.
- Native tooling setup: pass, Visual Studio Build Tools expose `cl.exe` and MSBuild through `scripts\Initialize-NativeToolchain.ps1`; CMake is available.
- Diagnostics: pass, reports 0 blockers, `winapp` and native build tools available, and shell integration `NotConfigured` as a warning.
- Shell plan: pass, reports Modern MSIX plus `IExplorerCommand` as the accepted V1 path, manifest contract ready, Classic HKCU as fallback, and Explorer registration not configured.
- Shell manifest: pass, reports stable CLSID `B8F1A86D-4C52-4C53-BF72-30B59F7F0F7D`, `IconReplacer.ShellExtension.dll`, `STA`, `Directory`, `.lnk`, and a parseable manifest fragment without registration.
- Shell bridge: pass, reports native bridge protocol, manifest identity, target status, safety rules, command counts, invocable counts, resolved arguments, and disabled reasons without mutation.
- Package plan: pass, reports per-user MSIX mode, 3 current product blockers, 2 proof warnings, native shell extension built, uninstall shell-removal policy, and `.icons`/restore-history preservation without installing anything.
- Release readiness: pass, reports not ready with blocking evidence gates for missing proof, diagnostics/package blockers, accessibility proof, manual Explorer proof, and release evidence without installing anything.
- Accessibility plan: pass, reports keyboard, names/semantics, visual adaptation, manual proof gates, and 9 future WinUI surfaces without launching WinUI.
- Navigation plan: pass, reports default/fallback routes, 13 registered routes, 5 top-level routes, workflow routes, selection requirements, and all current setup-action route targets.
- App window: pass, selects `home` for normal startup while preserving diagnostic blocker/warning badges; tests cover valid `change-icon` activation, unsupported targets, and malformed activation fallback.
- App commands: pass, reports route command ids, targets, enabled/disabled state, and disabled reasons for home, diagnostics, `change-icon`, selected-icon readiness, and restore-preview flows.
- App command requests: pass, resolves navigation, location open, refresh, workflow, disabled, and unknown-command paths without executing UI actions.
- App view: pass, composes window state, route metadata, commands, and ready content for home/filtered browser/icon-details/import/collections/package/shell-bridge/restore/change-icon routes; `package-plan` reports full packaging blockers including native build tools, and `shell-bridge` reports protocol, CLSID, target status, command counts, invocable counts, and safety-rule count without registration or mutation.
- Change icon workflow: pass, reports `NeedIcon`, `ReadyToApply`, and `Blocked` states without importing, writing target metadata, or creating restore history.
- Restore workflow: pass, reports `NeedRecord`, selected-record preview, `ReadyToRestore` in isolated tests, and `Blocked` for non-restorable records without restoring targets or updating restore history.
- Action request: pass, setup/home action ids resolve to stable workflow/navigation intents; `review-package-plan` targets `package-plan`, and disabled stale-action reasons stay visible.
- Operation feedback: pass, shared feedback covers apply, restore, batch import warning/info, and error messages.
- Import picker request: pass, app import picker metadata resolves for Imported and existing collections; `app-view --route import-icons` renders the same destination contract.
- Activation: pass, no args route to Home; `change-icon` args route to the launch/picker flow for `C:\Users\cristian\.icons`; `menu-apply` args route to a direct submenu apply preview without mutation.
- Activated preview: pass, activation args plus real `adobe.ico` report `Can apply: yes` without mutation.
- Activated apply: pass, temporary folder target changed and restored; after restore, `desktop.ini` no longer existed.
- Activated menu apply: pass, unit proof applies a current Icon Library menu icon through packaged-app `menu-apply` activation and rejects external icons before mutation.
- Home: pass, core ready, package-plan blocker action visible, 542 icons, 19 categories, 6 restore records, 2 stale records, shell integration `NotConfigured`.
- Browse: pass, search/category filters and capped results work against the real 542-icon library.
- Details: pass, real `adobe.ico` shows 8 internal images and recommended 256x256 image; `app-view --route icon-details --icon ...` renders the same selected-icon metadata without mutation.
- Preview change: pass, real `.icons` folder plus `adobe.ico` reports `Can apply: yes`; selecting an `.ico` as target reports `UnsupportedTarget`, keeps icon details visible, and exits 65.
- Recent: pass, 6 real changes shown, 0 restore-enabled, 2 stale with explicit missing-target reasons.
- Restore preview: pass, isolated tests cover enabled/warning/disabled states; CLI preview reports disabled reasons for current non-restorable real records.
- Batch import: pass, duplicate selected icons reuse existing imported file and catalog remains stable.
- Catalog warnings: pass, real library shows 0 warnings; tests cover invalid root/category icons.
- Collection import: pass, unit proof covers valid import, duplicate reuse, invalid file results, and null input rejection.
- Collections: pass, 19 real one-level collections listed including `Imported`; app-view collections route reports 542 collection icons without mutation.
- Catalog: pass, 19 categories, 542 icons.
- Target: pass, supported folder enables `Change icon...`; unsupported `.ico` target disables it with exit code 65.
- Picker request: pass, supported folder creates a single-select `.ico` picker request rooted at `C:\Users\cristian\.icons`; unsupported `.ico` target disables the picker with exit code 65.
- Launch request: pass, supported folder creates `change-icon --target C:\Users\cristian\.icons --target-kind folder`; unsupported `.ico` target disables launch with exit code 65.
- Change: pass, unsupported selected target is rejected before mutation.
- Status: pass, core features ready and shell integration `NotConfigured`.
- Paths: pass, all current app locations reported.
- Open request: pass, known app locations return safe open/open-file requests.
- Menu: pass, state `Ready`, `Change icon...`, 19 categories, 542/542 visible icons; unit proof covers empty, warning, truncated, and unavailable states.
- Menu commands: pass, state `Ready`, 543 shell-facing descriptors generated from the real menu snapshot; unit proof covers `open-app` recovery for empty/unavailable menus.
- Menu invocation preview: pass, `change-icon` resolves final arguments for a supported folder and disables unsupported `.ico` targets.
- Menu apply: pass, current Icon Library icon changed and restored a temporary folder target; after restore, `desktop.ini` no longer existed.
- Doctor: pass, dashboard counts and restore-health counts shown.
- History: pass, 6 restored proof records shown; stale filter shows 2 deleted proof targets, restorable filter shows 0 records.

## Next

Modern shell integration is selected, the WinUI/native toolchain is ready, the native `IExplorerCommand` DLL builds, the package manifest contains the COM/context-menu declarations, and pending `Change icon` activation opens the native file dialog. Continue by adding package identity/signing/installer proof, then capture install, uninstall, dynamic submenu, and manual Explorer proof before registration is considered complete.

## Open Gate

Explorer registration remains blocked until the selected Modern MSIX plus native `IExplorerCommand` path has package identity, signing, installer, install/uninstall, dynamic submenu, and manual Explorer proof. Do not register final shell integration yet.
