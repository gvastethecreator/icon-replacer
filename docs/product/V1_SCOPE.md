# V1 Scope

Status: proposed
Date: 2026-07-07

## Included

- Folder Target icon changes.
- Local directory junction and symbolic-link targets, using the selected link as the Folder Target identity.
- `.lnk` Shortcut Target icon changes.
- Local `.ico` files.
- `%USERPROFILE%\.icons` Icon Library.
- One-level Icon Categories.
- Import into `.icons\Imported`.
- Restore Records for every Icon Mutation.
- Core Engine, CLI proof harness, WinUI utility app, and chosen shell integration.
- Zero-management-window Explorer picker and direct submenu workflows.
- Gallery First management UI with real icon previews and System/Light/Dark themes.
- Clear errors and target-unchanged behavior for invalid input and permission failures.

## Deferred

- PNG/SVG conversion.
- EXE/DLL/ICL extraction.
- `.url` shortcuts.
- Multi-select batch behavior.
- Deep nested context-menu categories.
- Remote, WebDAV, or untrusted network folders.
- All-users enterprise installation.
- Automatic elevation.

## V1 Reality Gate

V1 is not complete unless folder, local directory-link, and `.lnk` apply/restore are proven and the chosen shell integration appears for those targets.
