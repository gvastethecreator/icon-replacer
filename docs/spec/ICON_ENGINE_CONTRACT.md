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

Restore-state persistence must serialize cooperating app and CommandHost processes
for each canonical state-file path. Readers must observe complete snapshots only.
Writers must flush a same-directory temporary file before atomic replacement;
replacement failure must retain the previous valid state and clean temporary output
when possible.

Apply and restore operations must also serialize by canonical Target path across
the management app and CommandHost. The Target mutation, Explorer notification,
and corresponding restore-state update form one guarded operation. A restore by
record ID must re-read that record after acquiring the Target lock so a stale
concurrent request cannot restore the same record twice.

If a mutation fails after partial writes:

- roll back when safe,
- otherwise leave a diagnostic Restore Record,
- report the exact recovery status,
- never claim success solely because one step succeeded.

If the target icon changes but restore-state persistence fails, the product
operation must attempt automatic Target rollback while still holding the
per-Target lock. Successful rollback returns the underlying persistence error
and states that the Target was restored. If rollback also fails, the operation
reports `PartialFailure` with both save and rollback causes.

If disk restore succeeds but its `Restored` status cannot be persisted, the
product operation must reapply the recorded icon while still holding the
per-Target lock so disk state remains consistent with the stored `Applied`
record. Successful reapply returns the underlying persistence error. If reapply
also fails, the operation reports `PartialFailure` with both causes.

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
- support local folders, local directory junctions/symbolic links, and local `.lnk` shortcuts,
- preserve the selected directory-link path as the Target identity while rejecting links whose resolved chain is remote, missing, cyclic, or too deep,
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

`AppLaunchRequestService` preserves the packaged-app activation contract used by
CLI/model proof. The production Explorer path is the zero-window command host
defined by ADR-0009 and ADR-0011.

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
- enable the picker only for a single local folder, directory link, or `.lnk` shortcut,
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
- call `IconApplyService` only after a single local folder, directory link, or `.lnk` target is accepted,
- return both the accepted shell-selection evaluation and apply result for UI diagnostics.

`IconMenuApplyService` is the shared direct dynamic-menu workflow for a future submenu icon choice.

It must:

- validate the shell selection before scanning or applying menu icons,
- require the chosen icon path to match a valid entry in the current Icon Library catalog,
- reject external, invalid, unsupported, or missing menu icon paths before target mutation,
- call the shared post-picker change workflow after target and menu-icon readiness are accepted,
- return the selected menu item plus the normal apply/restore-record result for UI and shell diagnostics.

`IconMenuCommandService` is the shared shell-facing menu command descriptor source.

It must:

- derive commands only from `IconMenuService` snapshots,
- emit a stable `change-icon` command first,
- emit safe `icon:<hash>` ids for visible Icon Library entries,
- keep command argument templates bounded and explicit,
- include category names without flattening them into path parsing logic,
- add an `open-app` overflow command when menu caps omit icons,
- add an `open-app` recovery command when the menu is empty or the catalog is unavailable,
- preserve menu state, status text, and scan error details for diagnostics,
- avoid target mutation and Explorer registration.

`IconMenuCommandInvocationService` is the shared shell-facing invocation preview path.

It must:

- find command descriptors in the current `IconMenuCommandService` snapshot,
- reject unknown or stale command ids before invocation,
- validate target-required commands through `ShellSelectionService`,
- resolve `{target}` and `{target-kind}` placeholders only for supported local folder or `.lnk` selections,
- allow non-target commands such as overflow `open-app` without a selection,
- return final argument lists plus a display string for shell diagnostics,
- avoid applying icons, opening pickers, launching apps, or registering Explorer.

`ShellExtensionBridgeService` is the shared native shell-extension bridge contract before `IExplorerCommand` implementation.

It must:

- compose `ShellManifestContractService`, `IconMenuCommandService`, and `ShellSelectionService` into one snapshot,
- expose the stable bridge protocol version, Explorer command CLSID, native DLL path, threading model, required interfaces, and supported item types,
- include menu state, icon counts, omitted counts, target status, command counts, invocable command counts, and Explorer safety rules,
- resolve target-required command arguments only when the selected target is one supported local folder or `.lnk`,
- keep non-target recovery commands such as `open-app` invocable without a target,
- surface disabled reasons for no selection, unsupported targets, stale menu commands, unavailable catalogs, and other non-ready states,
- avoid applying icons, opening pickers, launching windows, installing packages, editing the registry, restarting Explorer, or mutating target/icon state.

`AppMenuApplyActivationService` is the shared packaged-app activation preview for direct dynamic-menu icon choices.

It must:

- parse `menu-apply <target> <icon-from-library.ico>` activation arguments,
- require the selected icon path to map to a current visible `IconMenuCommandService` apply command,
- validate the target through `IconMenuCommandInvocationService`,
- return normalized app arguments only when the target and menu icon are both ready,
- expose disabled reasons for unsupported targets, stale icons, or malformed arguments,
- avoid applying icons, importing icons, writing target metadata, creating restore history, or registering Explorer.

`ActivatedMenuApplyService` is the shared packaged-app direct submenu apply path.

It must:

- accept only valid `menu-apply` activations,
- reject malformed activations, unsupported targets, and icons outside the current Icon Library menu before mutation,
- delegate application to `IconMenuApplyService` so restore history and icon validation stay centralized,
- return the selected menu item plus the normal apply/restore-record result for UI, CLI, and shell diagnostics.

`ShellManifestContractService` is the shared packaged modern/classic shell manifest source.

It must:

- define `windows.comServer` and `windows.fileExplorerContextMenus` manifest categories,
- define `windows.fileExplorerClassicContextMenuHandler` registration for the classic path,
- keep one stable Explorer command CLSID for the native extension,
- use `IconReplacer.ShellExtension.dll` and `STA` for the COM class contract,
- expose required native interfaces: `IExplorerCommand` and `IExplorerCommandState`,
- register `Directory` and `.lnk` targets through `desktop5:ItemType` verbs,
- generate a parseable manifest fragment for packaging review,
- avoid installing packages, editing the registry, restarting Explorer, or registering shell integration.

`PackagingPlanService` is the shared install/uninstall readiness source before packaging.

It must:

- describe the selected per-user MSIX install mode,
- surface `winapp`, native build tools, package identity, native shell extension, dev-signing, installer-build, and install/uninstall proof gates,
- reuse the shell manifest contract instead of duplicating manifest metadata,
- require uninstall to remove shell integration,
- preserve `.icons` and restore history by default,
- avoid installing packages, trusting certificates, editing the registry, restarting Explorer, or deleting user data.

`AppDiagnosticsService` must include native build-tooling diagnostics when the caller supplies `NativeToolingSnapshot`, so setup surfaces can distinguish missing `winapp` from missing Visual Studio C++ build tools before native shell-extension work starts.

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

`CatalogWarningsService` is the shared catalog-warning review path for CLI and WinUI diagnostics/library surfaces.

It must:

- scan the Icon Library through the Core catalog service,
- expose total valid icon count and warning count,
- return one warning row per skipped icon with path, display name, optional category, error code, message, and detail,
- preserve root vs one-level category context,
- avoid mutating icon files or deleting invalid files.

`IconImportPickerRequestService` is the shared app import-picker request source.

It must:

- prepare the Icon Library and Imported folders before a valid import picker opens,
- request a multi-select `.ico` picker for app-level imports,
- use the Icon Library root as the initial directory,
- target `.icons\Imported` by default,
- create or reuse a sanitized one-level collection when importing into a selected collection,
- reject invalid collection names before opening a picker.

`IconCollectionService` is the shared one-level collection-management path for CLI and WinUI library surfaces.

It must:

- list current one-level Icon Library folders with valid icon counts,
- include the managed `Imported` collection in list results,
- create sanitized one-level collection folders inside the Icon Library,
- reject empty or path-escaping collection names,
- treat creating an existing collection as an idempotent success.

`IconCollectionImportService` is the shared collection-fill path for CLI and WinUI library-management surfaces.

It must:

- create or reuse a sanitized one-level collection,
- validate each selected local `.ico` independently,
- copy valid icons into the selected collection,
- dedupe repeated content inside that collection,
- return per-file imported, reused-existing, or failed status,
- return final Icon Library status even when some selected files fail.

## AppModel Setup Readiness Operation

`SetupReadinessService` is the shared first-run/status path for CLI and WinUI setup surfaces.

It must:

- ensure the Icon Library paths can be resolved and scanned,
- report whether core features are usable,
- report icon, category, warning, restore-history, and restorable counts,
- surface setup actions such as importing icons, reviewing catalog warnings, reviewing stale history, and resolving shell integration,
- surface package-plan blockers as setup actions when packaging/tooling inputs are available,
- keep shell integration status explicit; after `IR-000`, the default readiness is `NotConfigured` until Modern Explorer integration is installed.

`AppActionRequestService` is the shared setup/home action routing source for the future WinUI app.

It must:

- accept stable setup action ids from `SetupReadinessService`,
- reject unknown action ids,
- disable known actions that are not currently available in the app state,
- map import actions to an import workflow target,
- map catalog-warning actions to the icon-browser target,
- map restore-history actions to the correct history filter,
- map shell-integration actions to shell-plan or diagnostics targets,
- map package-plan actions to the package-plan route,
- avoid opening windows, pickers, shell locations, or mutating state.

`AppNavigationService` is the shared route contract for the future WinUI app shell.

It must:

- list stable route ids for top-level screens and workflow surfaces,
- keep `home` as the default route and `diagnostics` as the fallback route,
- include library, collection, history, diagnostics, shell-plan, shell-bridge, package-plan, accessibility-plan, restore-preview, and change-icon workflows,
- expose primary commands for each route so UI layout and QA proof stay aligned,
- map every `AppActionKind` to a registered route id,
- avoid depending on WinUI controls, launching windows, opening pickers, or mutating state.

`AppWindowService` is the shared startup-state source for the future WinUI `MainWindow`.

It must:

- compose navigation routes, activation routing, and diagnostics into one startup snapshot,
- select `home` for normal startup and `change-icon` for valid Explorer-launched activations,
- keep unsupported `change-icon` targets on the `change-icon` route with a disabled state and reason,
- fall back to `diagnostics` for malformed activation arguments,
- expose diagnostic blocker and warning counts for badges,
- avoid launching WinUI, opening pickers, registering Explorer, or mutating target/icon state.

`AppCommandService` is the shared command-state source for future WinUI route buttons and command bars.

It must:

- derive command state from the current app-window route or any registered route id,
- expose stable command ids, labels, command kind, primary/secondary status, enabled state, target route or app location, and disabled reasons,
- keep home commands as navigation to import, browser, history, and diagnostics,
- expose diagnostics commands including refresh, app-data opening, and blocker review,
- keep `change-icon` apply/preview disabled until an icon is selected and target/icon readiness is valid,
- enable `change-icon` preview/apply commands only when the shared change workflow reports selected target and icon readiness,
- reject unknown route ids before UI rendering,
- avoid launching WinUI, opening pickers, opening folders, registering Explorer, or mutating target/icon state.

`AppCommandRequestService` is the shared command-button intent source for future WinUI.

It must:

- resolve one command id from the current route command state,
- return navigation targets for navigation commands,
- return `AppLocationOpenRequest` for open-location commands without opening Explorer,
- return refresh/workflow intents without executing them,
- return disabled reasons for unavailable commands,
- reject commands that are not registered for the selected route,
- avoid launching WinUI, opening folders, opening pickers, applying icons, restoring records, registering Explorer, or mutating state.

`AppRouteViewService` is the shared route-composition source for future WinUI rendering.

It must:

- compose app-window state, registered route metadata, command state, and route content into one snapshot,
- provide ready content for home, icon browser, icon-details with selection, import-icons, collections, history, diagnostics, shell plan, shell bridge, package plan, accessibility plan, restore-preview, and Explorer-launched change-icon routes,
- pass search text, category filters, and max visible count into the `icon-browser` route so future WinUI rendering and CLI proof use the same browser state,
- compose `icon-details` route content from the shared selected-icon details service, including display name, category, library membership, image count, and recommended image,
- compose `import-icons` route content from the shared import picker request, including destination collection, destination directory, `.ico` filter, and multi-select state,
- compose `package-plan` route content from full `PackagingPlanInputs` when available, so native build-tooling blockers and package blockers stay aligned with CLI packaging proof,
- compose `shell-bridge` route content from `ShellExtensionBridgeService`, including protocol, CLSID, menu state, target status, command counts, invocable counts, visible/omitted icon commands, and safety rules,
- keep `icon-details` as a non-ready placeholder until the user supplies a selected icon,
- compose `change-icon` route content from the shared workflow without opening pickers, applying icons, importing icons, or creating restore history,
- expose a concise summary and route-specific counts for CLI proof and UI smoke checks,
- reject unknown route ids before UI rendering,
- avoid launching WinUI, opening pickers, opening folders, registering Explorer, or mutating target/icon state.

## AppModel Operation Feedback

`AppOperationFeedbackService` is the shared user-facing feedback formatter for CLI and WinUI operation results.

It must:

- turn apply and restore results into success messages with history actions,
- turn import and batch-import results into success, info, or warning feedback based on reused and failed counts,
- turn errors into stable error feedback without hiding details,
- keep action targets as app navigation/workflow ids rather than shell commands,
- avoid mutating state or deciding whether an operation should run.

## AppModel Shell Integration Plan Operation

`ShellIntegrationPlanService` is the shared setup/diagnostics source for the accepted V1 shell architecture.

It must:

- report packaged modern `IExplorerCommand` plus packaged classic handler as the selected V1 path,
- report raw Classic HKCU verbs as retired,
- distinguish decision status from Explorer registration readiness,
- surface required V1 prerequisites such as WinUI templates, `winapp`, package identity, native shell extension, and Explorer registration,
- report missing `winapp` as blocking for the packaged path without attempting installation.

## AppModel Release Readiness Operation

`ReleaseReadinessService` is the shared release-evidence gate for CLI and future diagnostics/setup surfaces.

It must:

- compose build, test, CLI proof, diagnostics, package, accessibility, manual Explorer, and release evidence gates,
- keep the accepted packaged modern/classic integration path explicit,
- reuse package-plan, diagnostics, and accessibility snapshots rather than duplicating those checks,
- expose evidence commands or document paths for every release item,
- mark the release not ready while blockers or warnings remain,
- avoid installing packages, registering Explorer, opening windows, mutating target/icon state, or claiming manual proof before it is captured.

## AppModel Home Operation

`AppActivationService` is the shared startup routing source for the future packaged WinUI app.

It must:

- route empty activation arguments to the Home snapshot,
- route `change-icon --target <path> --target-kind <folder|shortcut>` arguments through `AppLaunchRequestService`,
- route `menu-apply <target> <icon-from-library.ico>` arguments through `AppMenuApplyActivationService`,
- preserve malformed-argument failures as explicit activation errors,
- return disabled change-icon activation snapshots when a selected target is unsupported,
- return disabled menu-apply activation snapshots when the target or menu icon is unsupported,
- avoid opening pickers or mutating targets during activation routing.

`ActivatedIconChangeService` is the shared post-picker orchestration path for a packaged app that was launched by `change-icon`.

It must:

- accept activation arguments plus the `.ico` path returned by the picker,
- reject non-`change-icon` activation before icon preview or mutation,
- preview selected icons without writing target metadata, imported icons, or restore history,
- apply selected icons through `IconChangeService` so restore history and import behavior stay consistent,
- reject invalid icons before target mutation.

`AppChangeIconWorkflowService` is the shared non-mutating route state source for the future WinUI `change-icon` workflow.

It must:

- require a `change-icon` packaged-app activation,
- expose whether the route is waiting for an icon, ready to apply, or blocked,
- surface target readiness and picker readiness before a file picker opens,
- accept an optional selected `.ico` path and use the activated preview path to validate target plus icon together,
- keep invalid icon and unsupported target states visible with stable disabled reasons,
- avoid importing icons, writing target metadata, creating restore history, launching pickers, or registering Explorer.

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
- create safe open requests only for known app locations,
- disable open requests for missing files without creating them,
- expose directory vs file shell verbs so UI can choose a launcher behavior without duplicating path logic.

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

## AppModel Restore Workflow Operation

`AppRestoreWorkflowService` is the shared non-mutating route state source for the future WinUI restore workflow.

It must:

- compose restore history and the selected restore record into one screen snapshot,
- expose whether the route needs a record selection, is ready to restore, or is blocked,
- preview selected records through `IconRestorePreviewService`,
- keep missing, already-restored, stale, and warning states visible with stable reasons,
- return missing selected records as a blocked workflow state while preserving the loaded history,
- feed `AppRouteViewService` and `AppCommandService` so the restore route can render history, selected-record details, confirm/cancel commands, and disabled reasons from one state source,
- avoid restoring targets, updating restore records, launching pickers, or registering Explorer.

## AppModel Diagnostics Operation

`AppDiagnosticsService` is the shared diagnostics/readiness path for the future WinUI diagnostics surface.

It must:

- compose setup readiness, dashboard health, app locations, shell integration state, and WinUI tooling state,
- classify each check as pass, info, warning, or blocking,
- keep `winapp` and WinUI templates as separate checks,
- report missing `winapp` as a blocker for WinUI scaffolding/running without attempting installation.

## AppModel Accessibility Plan Operation

`AccessibilityPlanService` is the shared accessibility acceptance source for future WinUI implementation and CLI planning proof.

It must:

- list V1 keyboard reachability and focus-return requirements,
- list accessible-name and semantic requirements for icon-only controls, icon tiles, and persistent errors,
- list visual adaptation requirements for high contrast, 200% scaling, and long paths,
- list manual proof items that still require screenshots or notes after WinUI exists,
- list the WinUI surfaces and primary commands covered by the acceptance plan,
- avoid depending on WinUI controls, opening windows, mutating state, or claiming visual proof before the UI is implemented.

## AppModel Restore Operation

`IconRestoreService` is the shared restore path for CLI and WinUI recent-change surfaces.

It must:

- restore by Restore Record id,
- reject missing Restore Records,
- reject records that are not currently `Applied`,
- call the matching Core Engine restore service,
- update the Restore Record status after successful disk restore,
- return folder-specific details such as `desktop.ini` path and shortcut-specific details such as current icon location.
