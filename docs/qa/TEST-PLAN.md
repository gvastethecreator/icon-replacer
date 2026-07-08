# QA Test Plan

Status: proposed
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
| Import | Multi-file picker with valid and invalid icons | Valid icons import, invalid icons return per-file failures |
| Import | Multi-file picker with duplicate content | Existing imported file reused, catalog count unchanged |
| Catalog | Reparse point inside `.icons` | Not followed |
| Menu snapshot | New `.icons\<category>\<icon>.ico` | Appears on next menu snapshot without manual rebuild |
| Menu snapshot | Large catalog over cap | Overflow counted and routed away from oversized menu |
| Setup readiness | Empty first run | Core ready, import action shown, shell decision shown |
| Setup readiness | Catalog warnings | Core still usable, warning action shown |
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
| Shortcut | Normal `.lnk` | Icon applied and restorable |
| Shortcut | Broken target `.lnk` | Icon can still change if `.lnk` writable |
| Shortcut | `.url` | Unsupported in V1 |
| Restore | Target exists | Previous state restored |
| Restore | Target moved/deleted | Preserved history with `target-missing`; restore not offered as safe |
| Restore | Record already restored | Rejected as not currently applied |
| History | Restorable filter | Shows only currently applied records with existing targets |
| History | Stale filter | Shows records with missing target or missing applied icon |
| Recent changes | Healthy applied record | Restore action enabled |
| Recent changes | Applied icon missing, target exists | Restore action enabled with warning |
| Recent changes | Target missing or already restored | Restore action disabled with reason |
| Diagnostics | WinUI templates present, `winapp` missing | Templates pass, `winapp` blocking, no install attempted |
| Diagnostics | Shell integration not configured | Warning shown, Explorer registration not attempted |
| Shell plan | Accepted V1 path | Modern MSIX plus `IExplorerCommand` selected, Classic HKCU marked fallback only |
| Shell plan | `winapp` missing | Required modern-path prerequisite shown as blocking, no install attempted |
| Diagnostics | Catalog warnings or stale history | Warnings/info shown with counts |
| Shell selection | Single local folder | `Change icon` enabled |
| Shell selection | Single local `.lnk` | `Change icon` enabled |
| Shell selection | Existing non-`.lnk` file | `Change icon` disabled with unsupported-target reason |
| Shell selection | Missing or remote target | `Change icon` disabled with specific reason |
| Shell selection | Multi-select | Disabled or explicitly unsupported in V1 |
| Picker request | Supported folder or `.lnk` | Single-select `.ico` picker request rooted at Icon Library |
| Picker request | Unsupported file or multi-select | Picker disabled with stable reason, no mutation |
| Launch request | Supported shell target | Stable `change-icon --target ... --target-kind ...` app arguments are generated |
| Launch request | Malformed or unsupported launch args | Rejected before picker or mutation logic |
| Shell | Right-click folder | `Change icon` appears in chosen integration path |
| Shell | Right-click `.lnk` | `Change icon` appears in chosen integration path |
| Install | Fresh install | Integration registered |
| Uninstall | Normal uninstall | Integration removed, `.icons` preserved |

## Accessibility Matrix

- Keyboard can reach every WinUI command.
- Icon-only buttons have accessible names.
- High contrast does not hide status or commands.
- 200% DPI does not clip main controls.
- Error messages persist in the app and are not toast-only.
- Icon catalog shows names, not thumbnails alone.

## Explorer Proof Matrix

Capture evidence for:

- context menu visible,
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

## Local Test Collections

Created under `C:\Users\cristian\.icons` from `D:\ICONS\Folder11-Ico\ico`:

- `Adobe Creative`: 30 icons.
- `Design 3D`: 30 icons.
- `Developer Tools`: 30 icons.
- `Media Audio Video`: 30 icons.
- `System Utilities`: 30 icons.
- `Gaming Hardware`: 30 icons.

CLI catalog proof sees 7 categories total including `Imported`, with 182 valid icons.

## CLI Proof

Temporary proof targets under `artifacts\tmp` verified:

- `apply-folder` then `restore`: restored folder no longer had temporary `desktop.ini`.
- `apply-shortcut` then `restore`: restored shortcut returned to `C:\Users\cristian\.icons\Adobe Creative\adobe.ico,0` and preserved `--cli-proof` arguments.
- `apply <target> <icon.ico>` autodetection then `restore`: restored folder no longer had temporary `desktop.ini`.
- `restore <record-id>` runs through shared AppModel restore orchestration and updates the restore record status.
- `import <icon.ico> [display-name]`: imports through shared AppModel library orchestration and dedupes repeated content.
- `batch-import <icon.ico> [icon2.ico ...]`: reports per-file batch import results; real proof reused an existing imported `adobe.ico` twice without increasing catalog count.
- `status`: reports first-run readiness, core feature state, setup actions, and shell integration `NotConfigured`.
- `shell-plan`: reports the accepted Modern shell integration plan; real proof shows `winapp` missing as blocking and Explorer registration not configured as warning.
- `home [filter]`: reports WinUI-ready first-screen state; real proof shows core ready, 182 icons, 7 categories, 4 restore records, 2 stale records, and all app locations.
- `activate [change-icon --target <path> --target-kind <folder|shortcut>]`: reports packaged-app activation routing; real proof routes no args to Home and routes `change-icon` args to the launch/picker flow.
- `activate-preview <icon.ico> change-icon --target <path> --target-kind <folder|shortcut>`: reports post-picker readiness from activation args plus selected icon; real proof enables `C:\Users\cristian\.icons` with `adobe.ico`.
- `activate-apply <icon.ico> change-icon --target <path> --target-kind <folder|shortcut>`: applies the activated post-picker flow through AppModel; temporary proof changed and restored a folder target with `desktop.ini` removed after restore.
- `browse [search] [--category <name>] [--max <count>]`: reports WinUI-ready catalog browser state; real proof finds 39 `adobe` matches and 30 `Developer Tools` icons.
- `details <icon.ico>`: reports WinUI-ready icon detail state; real proof shows `adobe.ico` has 8 internal images and recommended 256x256.
- `preview-change <target> <icon.ico>`: reports target and icon readiness before mutation; real proof enables `C:\Users\cristian\.icons` with `adobe.ico` and rejects an `.ico` selected as the target with exit code 65 while still showing icon details.
- `recent [filter]`: reports WinUI-ready recent-change rows; real proof shows 4 rows, 0 restore-enabled, and 2 stale target-missing rows.
- `diagnostics`: reports WinUI-ready diagnostic checks; real proof shows 1 blocker for missing `winapp`, 1 warning for shell `NotConfigured`, and existing app locations.
- `paths`: reports app navigation targets for Icon Library, Imported, AppData, and restore state.
- `picker-request <folder-or-shortcut>`: reports the future direct `.ico` file-picker request; real proof enables `C:\Users\cristian\.icons`, uses the Icon Library as initial directory, and disables an `.ico` selected as target with exit code 65.
- `launch-request <folder-or-shortcut>`: reports the future Explorer-to-app launch request; real proof generates `change-icon --target C:\Users\cristian\.icons --target-kind folder` and disables an `.ico` selected as target with exit code 65.
- `menu`: reports `Change icon...`, 7 dynamic categories, and 182/182 visible icons from `.icons`.
- `target <folder-or-shortcut>`: reports whether the selected target can show `Change icon...`; real proof enables `C:\Users\cristian\.icons` and disables an `.ico` file as unsupported.
- `change <target> <icon.ico>`: runs the post-picker workflow; real proof rejects an unsupported selected `.ico` target before mutation.
- `history [filter]`: lists persisted records and supports `all`, `restorable`, `applied`, `restored`, and `stale`.
- `doctor`: reports 7 catalog categories, 182 icons, 0 catalog warnings, 4 restore records, 0 restorable records, and 2 missing targets.
