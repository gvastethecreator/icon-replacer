# Icon Engine Contract

Status: proposed
Date: 2026-07-07

## Scope

The Core Engine owns validation, cataloging, mutation, restore, and refresh hooks. UI and shell integration call this contract rather than duplicating mutation logic.

The AppModel layer owns product operations that combine Core Engine calls with restore-state persistence. WinUI, CLI, and shell entry points should call AppModel operations instead of separately deciding how to mutate targets and save history.

## `.ico` Contract

Accepted files:

- local path,
- `.ico` extension,
- valid ICO reserved/type/count header,
- directory entries inside file bounds,
- reasonable size cap,
- at least one useful image size.

Rejected files:

- zero-byte,
- truncated,
- renamed PNG or other format,
- oversized,
- UNC or remote in V1,
- reparse-point traversal outside the Icon Library.

Rejection guarantee: no Target metadata is changed.

## Folder Target Contract

Apply must:

1. validate and import icon,
2. create Restore Record,
3. read existing `desktop.ini` if present,
4. merge icon keys without deleting unrelated keys,
5. write Unicode `desktop.ini`,
6. set required attributes,
7. notify Explorer,
8. return both disk-state result and Explorer-refresh status.

Restore must revert only values and attributes touched by Icon Replacer.

Open proof decision: choose `IconResource=<path>,0` vs `IconFile` plus `IconIndex` based on implementation evidence. Until proven, tests must record which form Windows honors most reliably.

## Shortcut Target Contract

Apply must:

1. validate and import icon,
2. load `.lnk`,
3. capture previous icon path/index,
4. set new icon path/index,
5. save `.lnk`,
6. notify Explorer.

It must not change target path, arguments, working directory, hotkey, description, or other metadata.

## Atomicity and Partial Failure

Every mutation must create a Restore Record before changing the Target.

Every product apply operation must persist the Restore Record before it reports full success to a UI or shell surface.

Every product restore operation must update the Restore Record after disk restore before it reports full success.

If a mutation fails after partial writes:

- roll back when safe,
- otherwise leave a diagnostic Restore Record,
- report the exact recovery status,
- never claim success solely because one step succeeded.

If the target icon changes but restore-state persistence fails, the product operation reports `PartialFailure`.

If disk restore succeeds but restore-state persistence fails, the product operation reports `PartialFailure`.

## Result Model

Every operation should distinguish:

- success and Explorer refreshed,
- success on disk but Explorer cache may lag,
- unsupported target,
- invalid icon,
- permission denied,
- remote/untrusted target,
- partial failure with recovery data,
- restore failed because target moved or disappeared.

## AppModel Shell Selection Operation

`ShellSelectionService` is the shared pre-mutation decision path for CLI proof, WinUI diagnostics, and the future Explorer command.

It must:

- enable `Change icon...` only for exactly one selected target,
- support local folders and local `.lnk` shortcuts,
- reject multi-selection in V1,
- reject missing, invalid, remote, web-backed, and unsupported file targets before any mutation,
- return a stable status plus an `IconReplacerError` so UI and shell surfaces can show or disable commands consistently.

## AppModel Apply Operation

`IconApplyService` is the shared apply path for CLI, WinUI, and shell surfaces.

It must:

- autodetect an existing folder vs `.lnk` target when the caller does not provide shell selection metadata,
- accept explicit shell selection metadata when Explorer has already identified whether the target is a directory,
- reject unsupported existing files before mutation,
- call the matching Core Engine apply service,
- persist the Restore Record,
- return folder-specific details such as `desktop.ini` path and shortcut-specific details such as current icon location.

## AppModel Change Operation

`AppLaunchRequestService` is the shared Explorer-to-app handoff contract for the accepted Modern shell path.

It must:

- build stable app arguments for `Change icon...` using `change-icon --target <path> --target-kind <folder|shortcut>`,
- parse the same argument shape for packaged app activation,
- preserve Explorer-provided target kind metadata when available,
- reject unknown verbs, missing targets, unsupported target kinds, and unknown arguments before opening a picker,
- call the picker-request path so launch readiness and picker readiness stay aligned,
- return no launch arguments when the selected target is unsupported.

`IconPickerRequestService` is the shared file-picker request source for the future `Change icon...` command.

It must:

- validate the shell selection before opening any picker,
- enable the picker only for a single local folder or `.lnk` shortcut,
- prepare the Icon Library folders before a valid picker opens,
- request a single-select `.ico` picker,
- use the Icon Library root as the initial directory,
- return stable disabled states for unsupported targets and V1 multi-selection.

`IconChangePreviewService` is the shared pre-apply preview path for the future file-picker confirmation surface.

It must:

- evaluate the shell selection and selected icon without calling mutation services,
- validate both target support and icon details so UI can show independent target/icon status,
- return `CanApply=false` with a stable blocking error when either side is invalid,
- describe valid icons without importing or copying them,
- avoid writing `desktop.ini`, `.lnk` metadata, imported icon files, or restore history.

`IconChangeService` is the shared post-picker workflow for the future `Change icon...` command.

It must:

- validate the shell selection before validating or importing the chosen icon,
- reject unsupported selections before creating `.icons\Imported` files or restore history,
- call `IconApplyService` only after a single local folder or `.lnk` target is accepted,
- return both the accepted shell-selection evaluation and apply result for UI diagnostics.

## AppModel Icon Library Operation

`IconLibraryService` is the shared import/status path for CLI and WinUI library-management surfaces.

It must:

- ensure `%USERPROFILE%\.icons` and `.icons\Imported` exist,
- import local `.ico` files through the Core importer,
- reject invalid icon files before copy,
- dedupe by content hash even when a preferred display name differs,
- return updated catalog status after import.

Batch import must:

- process every selected file independently,
- return per-file imported, reused-existing, or failed status,
- preserve dedupe behavior across repeated selections,
- return final Icon Library status even when some selected files fail.

## AppModel Setup Readiness Operation

`SetupReadinessService` is the shared first-run/status path for CLI and WinUI setup surfaces.

It must:

- ensure the Icon Library paths can be resolved and scanned,
- report whether core features are usable,
- report icon, category, warning, restore-history, and restorable counts,
- surface setup actions such as importing icons, reviewing catalog warnings, reviewing stale history, and resolving shell integration,
- keep shell integration status explicit; after `IR-000`, the default readiness is `NotConfigured` until Modern Explorer integration is installed.

## AppModel Shell Integration Plan Operation

`ShellIntegrationPlanService` is the shared setup/diagnostics source for the accepted V1 shell architecture.

It must:

- report Modern MSIX plus native `IExplorerCommand` as the selected V1 path,
- report Classic HKCU verbs as fallback/prototype only,
- distinguish decision status from Explorer registration readiness,
- surface required V1 prerequisites such as WinUI templates, `winapp`, package identity, native shell extension, and Explorer registration,
- report missing `winapp` as blocking for the Modern path without attempting installation.

## AppModel Home Operation

`AppActivationService` is the shared startup routing source for the future packaged WinUI app.

It must:

- route empty activation arguments to the Home snapshot,
- route `change-icon --target <path> --target-kind <folder|shortcut>` arguments through `AppLaunchRequestService`,
- preserve malformed-argument failures as explicit activation errors,
- return disabled change-icon activation snapshots when a selected target is unsupported,
- avoid opening pickers or mutating targets during activation routing.

`ActivatedIconChangeService` is the shared post-picker orchestration path for a packaged app that was launched by `change-icon`.

It must:

- accept activation arguments plus the `.ico` path returned by the picker,
- reject non-`change-icon` activation before icon preview or mutation,
- preview selected icons without writing target metadata, imported icons, or restore history,
- apply selected icons through `IconChangeService` so restore history and import behavior stay consistent,
- reject invalid icons before target mutation.

`AppHomeService` is the shared first-screen state source for the future WinUI app.

It must:

- compose setup readiness, dashboard counts, menu preview, restore history, and app locations,
- support the same restore-history filters used by the history surface,
- expose whether core features are ready and whether attention is needed,
- avoid taking a dependency on WinUI controls or Explorer registration.

## AppModel Icon Browser Operation

`IconBrowserService` is the shared catalog browsing/search path for the future WinUI catalog surface.

It must:

- scan the Icon Library through the Core catalog service,
- support search text and category filters,
- cap visible results and report omitted counts,
- return category summaries and catalog warnings,
- avoid following shell-menu caps; the browser is a richer app surface than the context menu.

## AppModel Icon Details Operation

`IconDetailsService` is the shared selected-icon detail path for the future WinUI catalog/import preview surfaces.

It must:

- validate the selected `.ico`,
- report display name, category, library membership, file size, and internal ICO image entries,
- choose a recommended image from useful sizes when available,
- support valid external icons before import while still marking them as outside the Icon Library.

## AppModel Location Operation

`AppLocationService` is the shared location-target path for CLI and WinUI navigation/diagnostic surfaces.

It must:

- ensure Icon Library folders exist before returning Open Library targets,
- return Icon Library, Imported Icons, AppData, and Restore State locations,
- distinguish file vs directory targets,
- report whether each target currently exists without creating the restore-state file.

## AppModel Restore History Operation

`RestoreHistoryService` is the shared recent-change/history path for CLI and WinUI history surfaces.

It must:

- list restore records in descending mutation time,
- report total, applied, restored, restorable, and stale counts,
- expose filters for all, restorable, applied, restored, and stale records,
- classify stale records when the target or applied icon path is missing,
- preserve restored history instead of deleting it silently.

## AppModel Recent Changes Operation

`RecentChangesService` is the shared actionable-row path for the future WinUI recent changes surface.

It must:

- reuse restore-history filters,
- expose per-row restore enabled, disabled, or enabled-with-warning state,
- explain disabled restore actions with user-facing health text,
- keep missing applied-icon rows restorable with a warning when the target still exists, because restore uses the saved previous-state snapshot.

## AppModel Diagnostics Operation

`AppDiagnosticsService` is the shared diagnostics/readiness path for the future WinUI diagnostics surface.

It must:

- compose setup readiness, dashboard health, app locations, shell integration state, and WinUI tooling state,
- classify each check as pass, info, warning, or blocking,
- keep `winapp` and WinUI templates as separate checks,
- report missing `winapp` as a blocker for WinUI scaffolding/running without attempting installation.

## AppModel Restore Operation

`IconRestoreService` is the shared restore path for CLI and WinUI recent-change surfaces.

It must:

- restore by Restore Record id,
- reject missing Restore Records,
- reject records that are not currently `Applied`,
- call the matching Core Engine restore service,
- update the Restore Record status after successful disk restore,
- return folder-specific details such as `desktop.ini` path and shortcut-specific details such as current icon location.
