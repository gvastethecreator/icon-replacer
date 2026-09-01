# QA test plan

Status: living QA matrix
Date: 2026-07-07

## Quality Bar

Icon Replacer is not complete if it only changes one test folder on the happy path. It must prove restore, invalid input handling, permission failures, and Explorer-visible behavior.

## Functional Matrix

| Area | Scenario | Expected Result |
| --- | --- | --- |
| Icon validation | Valid single-image `.ico` | Accepted |
| Icon validation | Valid multi-size `.ico` | Accepted |
| Icon validation | `.png` renamed to `.ico` | Rejected, target unchanged |
| Icon validation | Truncated `.ico` | Rejected, target unchanged |
| Icon validation | Oversized `.ico` | Rejected or capped, clear error |
| Catalog | Empty `.icons` | Useful empty state, no dead menu |
| Catalog | `.icons\Work\blue.ico` | Category `Work`, item `blue` |
| Catalog | Duplicate imported icon | Dedupe result, no broken reference |
| Catalog | Duplicate content with different display name | Existing imported file returned, no duplicate content |
| Catalog warnings | Invalid root/category icons | Warning list shows path, category, and validation reason |
| Import | Multi-file picker with valid and invalid icons | Valid icons import, invalid icons return per-file failures |
| Import | Multi-file picker with duplicate content | Existing imported file reused, catalog count unchanged |
| Import picker | Default import action | Multi-select `.ico` picker rooted at Icon Library and targeting Imported |
| Import picker | Import into collection | Destination collection is created or reused before picker opens |
| Catalog | Reparse point inside `.icons` | Not followed |
| Menu snapshot | New `.icons\<category>\<icon>.ico` | Appears on next menu snapshot without manual rebuild |
| Menu snapshot | Empty library | Shows empty state plus import/open recovery action |
| Menu snapshot | Large catalog over cap | Overflow counted and routed away from oversized menu |
| Menu snapshot | Catalog scan failure | Shows unavailable state without losing `Change icon...` recovery |
| Menu commands | Visible menu entries | Stable command ids and argument templates are generated from the menu snapshot |
| Menu commands | Truncated, empty, or unavailable menu | `open-app` overflow/recovery command appears |
| Menu invocation | Supported target command | Final arguments are resolved from command descriptor and target |
| Menu invocation | Unsupported target command | Invocation disabled with the shell-selection reason |
| Menu invocation | Overflow open-app command | Invocation allowed without selected target |
| Setup readiness | Empty first run | Core ready, import action shown, shell decision shown |
| Setup readiness | Catalog warnings | Core still usable, warning action shown |
| App actions | Visible setup action | Resolves to a stable WinUI navigation/workflow intent |
| App actions | Known action no longer visible | Disabled with stale-action reason |
| App actions | Unknown action id | Rejected without navigation intent |
| Operation feedback | Apply/restore success | Success feedback includes user-facing title and history action |
| Operation feedback | Batch import partial failure | Warning feedback includes counts and review action |
| Operation feedback | Duplicate-only import | Info feedback reports reused existing icons |
| Home snapshot | Ready library with history | Shows readiness, actions, menu counts, history counts, and locations |
| Home snapshot | Stale history filter | Shows stale records without marking them restorable |
| App activation | No arguments | Routes to Home snapshot |
| App activation | `change-icon` arguments | Routes to validated launch/picker flow |
| App activation | Unknown verb or malformed arguments | Rejected before picker or mutation logic |
| Activated change flow | Valid activation plus valid selected icon | Preview succeeds without mutation; apply changes target and stores restore record |
| Activated change flow | Home activation or invalid selected icon | Rejected before target mutation |
| Icon browser | Search text | Shows matching icons and omitted counts when capped |
| Icon browser | Category filter | Shows only the selected category |
| Icon browser | Catalog warning | Surfaces invalid icon warnings |
| Icon details | Library icon selected | Shows category, internal image entries, recommended image |
| Icon details | External icon selected before import | Shows valid details and marks as external |
| Icon details | Invalid icon selected | Shows validation error, no preview claim |
| Change preview | Valid target plus valid icon | Reports can apply, target kind, and icon details without mutation |
| Change preview | Unsupported target plus valid icon | Reports disabled target and still describes the valid selected icon |
| Change preview | Valid target plus invalid icon | Reports invalid icon and leaves target/import/history unchanged |
| App locations | Open Library targets | Library, Imported, AppData, and restore state paths reported with existence |
| Folder | No existing `desktop.ini` | Icon applied and restorable |
| Folder | Existing `desktop.ini` | Unrelated keys preserved |
| Folder | Protected folder | Access denied, target unchanged |
| Folder | OneDrive local folder | Apply if writable, cache-delay message if needed |
| Folder | UNC/network path | Blocked or warned in V1 |
| Directory junction | Local directory target | Icon applied/restored through the selected junction; junction remains intact |
| Directory symbolic link | Local directory target | Icon applied/restored through the selected symlink; symlink remains intact |
| Directory symbolic link | Remote/UNC target | Rejected before icon import or target mutation |
| Shortcut | Normal `.lnk` | Icon applied and restorable |
| Shortcut | Broken target `.lnk` | Icon can still change if `.lnk` writable |
| Shortcut | `.url` | Unsupported in V1 |
| Restore | Target exists | Previous state restored |
| Restore | Target moved/deleted | Preserved history with `target-missing`; restore not offered as safe |
| Restore | Record already restored | Rejected as not currently applied |
| Restore state | App and CommandHost write concurrently | Every record is preserved in valid JSON |
| Restore state | Reads overlap concurrent writes | Every read receives one complete valid snapshot |
| Restore state | Atomic replacement fails | Previous valid state remains readable, temporary output is cleaned, and an actionable error is returned |
| Apply persistence | Restore record cannot be saved after folder mutation | Folder and attributes return to their original state; the persistence error is reported |
| Apply persistence | Restore record cannot be saved after shortcut mutation | Shortcut returns to its previous icon without changing other metadata |
| Apply persistence | Save and automatic rollback both fail | `PartialFailure` reports both causes and never claims the Target was restored |
| Restore persistence | `Restored` status cannot be saved for a folder | Recorded applied icon is reapplied and stored record remains `Applied` |
| Restore persistence | `Restored` status cannot be saved for a shortcut | Recorded shortcut icon is reapplied and other shortcut metadata remains unchanged |
| Restore persistence | Status save and icon reapply both fail | `PartialFailure` reports independent save and reapply causes |
| Target mutation | Many changes target one folder concurrently | Changes serialize, every apply succeeds, and reverse restore returns the original state |
| Target mutation | Many restores target one applied record concurrently | Exactly one restore succeeds; stale requests are rejected after the record is re-read under the Target lock |
| Restore preview | Applied record with target | Confirmation state is enabled without mutating target or history |
| Restore preview | Applied icon missing | Confirmation state stays enabled with a warning |
| Restore preview | Missing target or already restored record | Confirmation state is disabled with a stable reason |
| History | Restorable filter | Shows only currently applied records with existing targets |
| History | Stale filter | Shows records with missing target or missing applied icon |
| Recent changes | Healthy applied record | Restore action enabled |
| Recent changes | Applied icon missing, target exists | Restore action enabled with warning |
| Recent changes | Target missing or already restored | Restore action disabled with reason |
| Diagnostics | WinUI templates and `winapp` present | Tooling checks pass, no install attempted |
| Diagnostics | Native C++ build tools available through Build Tools | `native-build-tools` passes, helper script can expose `cl.exe` and MSBuild |
| Diagnostics | Shell integration not configured | Warning shown, Explorer registration not attempted |
| Shell plan | Accepted V1 path | Packaged modern `IExplorerCommand` plus packaged classic handler selected; raw HKCU verbs retired |
| Shell plan | Explorer registration not configured | Packaged implementation/proof gates shown without registering Explorer |
| Shell manifest | Packaged dual contract | `windows.comServer`, modern and classic context-menu categories, stable CLSID, `Directory`, and `.lnk` targets shown without registration |
| Shell bridge | Supported folder or `.lnk` | Manifest identity, target status, safety rules, and resolved command arguments shown without mutation |
| Shell bridge | Unsupported target | Target-required commands disabled with reason; non-target recovery commands remain invocable |
| Shell bridge app view | Supported folder or `.lnk` | `app-view --route shell-bridge --shell-target <target>` renders bridge status, command counts, target status, and safety rules without mutation |
| Package plan | Missing package prerequisites after tooling setup | Missing package identity, native extension, signing, and install package shown as blockers without install attempts |
| Package plan | Uninstall policy | Shell integration removal and `.icons` preservation shown as required policy |
| Release readiness | Missing release proof | Build/test/CLI/manual/package/accessibility/evidence gates shown as not release-ready without install attempts |
| Accessibility plan | Contract | Keyboard, names, high contrast, scaling, proof items, and WinUI surfaces shown |
| Navigation plan | Contract | Top-level routes, workflow routes, route commands, and setup-action targets shown |
| WinUI app shell | Packaged launch | `BuildAndRun.ps1` builds and launches through `winapp`, window title is `Icon Replacer` |
| WinUI app shell | AppModel-backed state | Home, icons, recent, diagnostics, and package routes show current AppModel counts and rows |
| App window | Startup | Home, change-icon activation, unsupported targets, malformed args, and diagnostic badges route correctly |
| App commands | Contract | Route commands expose stable ids, target routes/locations, enabled states, disabled reasons, restore confirm state, and change-icon preview/apply readiness |
| App command request | Navigation command | Resolves a target route without navigating |
| App command request | Open-location command | Resolves a safe app-location open request without opening Explorer |
| App command request | Disabled command | Returns disabled reason without workflow side effects |
| App view | Contract | Route content, command state, selected window route, readiness, icon details, import destination, collections, package/native-tool blockers, shell bridge status, restore workflow state, and change-icon workflow state compose into one renderable snapshot |
| Change icon workflow | Contract | Explorer activation waits for icon, previews selected icon, blocks invalid target/icon, and does not mutate before apply |
| Restore workflow | Contract | History loads, selected records preview, missing selection waits, and non-restorable records block without mutation |
| Diagnostics | Catalog warnings or stale history | Warnings/info shown with counts |
| Shell selection | Single local folder | `Change icon` enabled |
| Shell selection | Local directory junction or symbolic link | `Change icon` enabled as a Folder Target |
| Shell selection | Single local `.lnk` | `Change icon` enabled |
| Shell selection | Existing non-`.lnk` file | `Change icon` disabled with unsupported-target reason |
| Shell selection | Missing or remote target | `Change icon` disabled with specific reason |
| Shell selection | Multi-select | Disabled or explicitly unsupported in V1 |
| Picker request | Supported folder or `.lnk` | Single-select `.ico` picker request rooted at Icon Library |
| Picker request | Unsupported file or multi-select | Picker disabled with stable reason, no mutation |
| Launch request | Supported shell target | Stable `change-icon --target ... --target-kind ...` app arguments are generated |
| Launch request | Malformed or unsupported launch args | Rejected before picker or mutation logic |
| Shell | Right-click folder | `Change icon` appears in both packaged Explorer paths |
| Shell | Right-click local directory junction or symbolic link | Both direct and collection commands are enabled |
| Shell | Right-click `.lnk` | `Change icon` appears in both packaged Explorer paths |
| Classic shell | Root menu | Separator groups `Change icon...` and `Icon collections`; both show the application icon |
| Classic shell | Collections submenu opens | Every visible collection receives a representative `.ico` preview lazily |
| Classic shell | Icon submenu opens | Every visible icon receives its own `.ico` preview lazily and remains a one-click apply command |
| Classic shell | One invalid `.ico` preview | Other commands and previews remain available; Explorer receives no handler failure |
| Classic shell | Real local catalog | Initial query and lazy submenu preview timings are captured by native smoke proof |
| Install | Fresh install | Integration registered |
| Uninstall | Normal uninstall | Integration removed, `.icons` preserved |

## Accessibility Matrix

- Library navigation, search, Refresh, Import, Open Library, preview-size control,
  collection list, icon grid, and the Collections splitter are keyboard reachable.
- Icon-only controls have accessible names and help text where their range or
  behavior is not obvious.
- Every icon tile exposes both the icon name and collection; every Recent restore
  action identifies its target instead of repeating a generic button name.
- Updated operation feedback reopens the persistent InfoBar and is exposed as a
  polite live region so assistive technology receives the new message.
- High contrast keeps navigation, status, selection, commands, and focus visible
  through system brushes rather than fixed theme colors.
- At 200% display scaling, the adaptive toolbar moves to its compact layout,
  keeps every command visible, and does not overlap the responsive icon grid.
- Long Icon Library paths remain visible and have a complete accessible name in
  Settings.
- `accessibility-plan` lists the actual Gallery First Library, Recent, and
  Settings surfaces and their shared acceptance contract.

Automated accessibility proof for an already launched management app:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tests\ui\accessibility-ui.ps1 -AppPid <pid> -ArtifactDir artifacts\ui-tests\gallery-first\accessibility-100 -MinimumScalePercent 100
powershell -NoProfile -ExecutionPolicy Bypass -File tests\ui\accessibility-ui.ps1 -AppPid <pid> -ArtifactDir artifacts\ui-tests\gallery-first\accessibility-200 -MinimumScalePercent 200
```

The second run must be launched on a monitor configured to 200% scaling. High
contrast remains a manual visual run of the same script because changing the OS
theme is outside unattended test ownership.

## Explorer Proof Matrix

Capture evidence for:

- context menu visible,
- classic separator, both application icons, collection previews, and one complete icon submenu,
- classic handler stays at or below 242 command ids and respects a constrained 8-id range,
- install/uninstall leaves every unrelated context-menu registration unchanged and does not restart Explorer,
- picker opens directly,
- folder before/apply/restore,
- shortcut before/apply/restore,
- Explorer refresh behavior,
- invalid icon error,
- permission failure error.

## Security and Trust

- Do not execute icon files.
- Do not extract from EXE/DLL/ICL in V1.
- Do not remove Mark-of-the-Web or bypass Windows June 2026 `desktop.ini` hardening automatically.
- Do not modify remote/untrusted folders in V1.
- Do not require admin for normal use.

## CLI Proof

Temporary proof targets under `artifacts\tmp` verified:

- `apply-folder` then `restore`: restored folder no longer had temporary `desktop.ini`.
- `apply-shortcut` then `restore`: restored shortcut returned to `C:\Users\cristian\.icons\Adobe Creative\adobe.ico,0` and preserved `--cli-proof` arguments.
- `apply <target> <icon.ico>` autodetection then `restore`: restored folder no longer had temporary `desktop.ini`.
- `restore <record-id>` runs through shared AppModel restore orchestration and updates the restore record status.
- `import <icon.ico> [display-name]`: imports through shared AppModel library orchestration and dedupes repeated content.
- `import-picker-request [collection]`: reports the future WinUI import picker request; real proof should show multi-select `.ico` picker metadata and Imported or collection destination.
- `batch-import <icon.ico> [icon2.ico ...]`: reports per-file batch import results; real proof reused an existing imported `adobe.ico` twice without increasing catalog count.
- `collections`: reports the intended 7 one-level Icon Library collections including `Imported`, with 185 icons total and per-collection counts.
- `catalog-warnings`: reports the future catalog-warning review list; real proof currently shows 0 warnings.
- `collection-create <name>`: creates sanitized one-level collection folders idempotently; unit proof covers sanitized names, existing folders, and empty names.
- `collection-import <collection> <icon.ico> [icon2.ico ...]`: imports valid icons into a one-level collection with per-file results; unit proof covers duplicate reuse and invalid-file failures.
- `status`: reports first-run readiness, core feature state, package/setup actions, and shell integration `NotConfigured`.
- `shell-plan`: reports the accepted packaged modern/classic integration plan; raw HKCU verbs are retired and the normal post-test state is uninstalled.
- `home [filter]`: reports WinUI-ready first-screen state; current real proof uses 185 icons in 7 categories.
- `activate [change-icon --target <path> --target-kind <folder|shortcut>|menu-apply <target> <icon-from-library.ico>]`: reports packaged-app activation routing; real proof routes no args to Home, routes `change-icon` args to the launch/picker flow, and routes `menu-apply` args to a direct submenu apply preview.
- `activate-preview <icon.ico> change-icon --target <path> --target-kind <folder|shortcut>`: reports post-picker readiness from activation args plus selected icon; real proof enables `C:\Users\cristian\.icons` with `adobe.ico`.
- `activate-apply <icon.ico> change-icon --target <path> --target-kind <folder|shortcut>`: applies the activated post-picker flow through AppModel; temporary proof changed and restored a folder target with `desktop.ini` removed after restore.
- `activate-menu-apply <target> <icon-from-library.ico>`: applies a packaged-app direct submenu activation through AppModel; temporary proof should change and restore a folder target through the same menu icon validation as `menu-apply`.
- `browse [search] [--category <name>] [--max <count>]`: reports WinUI-ready catalog browser state; real proof finds 102 `adobe` matches and 30 `Developer Tools` icons.
- `details <icon.ico>`: reports WinUI-ready icon detail state; real proof shows `adobe.ico` has 8 internal images and recommended 256x256.
- `preview-change <target> <icon.ico>`: reports target and icon readiness before mutation; real proof enables `C:\Users\cristian\.icons` with `adobe.ico` and rejects an `.ico` selected as the target with exit code 65 while still showing icon details.
- `recent [filter]`: reports WinUI-ready recent-change rows; real proof shows 6 rows, 0 restore-enabled, and 2 stale target-missing rows.
- `restore-workflow [record-id] [filter]`: reports WinUI-ready restore route state; real proof without a record shows `NeedRecord`, and a restored record shows `Blocked` with selected-record preview details.
- `restore-preview <record-id>`: reports WinUI-ready restore confirmation state before mutation; non-restorable records return a disabled reason.
- `diagnostics`: reports WinUI-ready diagnostic checks; real proof shows 0 blockers, 1 warning for shell `NotConfigured`, and existing app locations.
- `release-readiness [proof flags]`: reports the release evidence gate; real proof should show missing package/signing/manual Explorer proof, missing accessibility proof, and missing release evidence as not release-ready without installing anything.
- `accessibility-plan`: reports the future WinUI accessibility acceptance contract; real proof shows 15 required items, 5 manual proof items, and 9 app surfaces.
- `navigation-plan`: reports the future WinUI route contract; real proof shows 13 routes, 5 top-level routes, and every setup action kind mapped to a registered route.
- `app-window [activation args]`: reports future main-window startup state; real proof selects `home` for normal startup while preserving diagnostics badges, and tests cover Explorer activation, unsupported targets, and malformed args.
- `app-commands [--route <route-id>] [--record <record-id>] [--filter <filter>] [--icon <icon.ico>] [--collection <name>] [activation args]`: reports future WinUI command state; real proof covers home navigation, diagnostics blocker commands, change-icon disabled/apply-ready states, and restore confirm/reason commands.
- `app-command-request <command-id> [--route <route-id>] ...`: reports future WinUI button intent; real proof covers navigation, open-location, refresh, workflow, disabled, and unknown-command paths without launching or mutating.
- `app-view [--route <route-id>] [--record <record-id>] [--filter <filter>] [--icon <icon.ico>] [--collection <name>] [--shell-target <target>] [--search <text>] [--category <name>] [--max <count>] [activation args]`: reports future WinUI route composition; real proof covers home, filtered browser, icon-details, import, collections, package-plan with native-tool blockers, shell-bridge, change-icon workflow, and restore workflow routes.
- `change-icon-workflow [--icon <icon.ico>] change-icon ...`: reports future Change Icon route state; real proof covers waiting for icon, ready-to-apply preview, and unsupported target blocking.
- `action-request <action-id>`: resolves current setup/home action ids into stable WinUI intents; real proof should cover `configure-shell-integration`, `review-package-plan`, and disabled stale actions.
- Operation feedback is unit-proven for apply, restore, import warnings, reused imports, and errors; CLI apply/restore titles now use the shared formatter.
- `paths`: reports app navigation targets for Icon Library, Imported, AppData, and restore state.
- `open-request <location>`: reports safe WinUI navigation requests for known app locations; real proof enables Icon Library, Imported, and Restore State.
- `picker-request <folder-or-shortcut>`: reports the future direct `.ico` file-picker request; real proof enables `C:\Users\cristian\.icons`, uses the Icon Library as initial directory, and disables an `.ico` selected as target with exit code 65.
- `launch-request <folder-or-shortcut>`: reports the future Explorer-to-app launch request; real proof generates `change-icon --target C:\Users\cristian\.icons --target-kind folder` and disables an `.ico` selected as target with exit code 65.
- `menu`: reports `Change icon...`, 7 dynamic categories, and 185/185 visible icons from `.icons`.
- `menu-commands`: reports stable shell-facing command descriptors for the same menu snapshot, including `change-icon` and `icon:<hash>` entries.
- `shell-bridge [target]`: reports the future native `IExplorerCommand` bridge contract; real proof should cover a supported folder and an unsupported `.ico` target without mutation.
- `menu-invoke-preview <command-id> [target]`: resolves a command descriptor against a target; real proof should cover `change-icon` on a supported folder and a disabled unsupported target.
- `menu-apply <target> <icon-from-library.ico>`: applies a selected dynamic-menu icon only when it belongs to the current Icon Library; temporary proof changed and restored a folder with `desktop.ini` removed after restore.
- `target <folder-or-shortcut>`: reports whether the selected target can show `Change icon...`; real proof enables `C:\Users\cristian\.icons` and disables an `.ico` file as unsupported.
- `change <target> <icon.ico>`: runs the post-picker workflow; real proof rejects an unsupported selected `.ico` target before mutation.
- `history [filter]`: lists persisted records and supports `all`, `restorable`, `applied`, `restored`, and `stale`.
- `doctor`: reports the current 7 catalog categories and 185 icons plus catalog and restore-health diagnostics.
