# Product Requirements

Status: proposed
Date: 2026-07-07

## Problem

Changing a folder or shortcut icon in Windows is slow, hidden in Properties dialogs, and hard to repeat across many folders. Users who keep icon collections should be able to apply them directly from File Explorer.

## Target User

A Windows user who organizes folders or desktop shortcuts visually and wants a fast, reversible, low-friction icon change workflow.

## Primary User Journey

1. User right-clicks a Folder Target or `.lnk` Shortcut Target.
2. User chooses `Change icon`.
3. A file picker opens directly for `.ico` files.
4. User selects an icon.
5. Icon Replacer copies the icon into the Icon Library.
6. Icon Replacer applies the icon to the Target.
7. The picker closes; the management app never opens.
8. Restore/undo becomes available in the explicitly launched management app.

## Secondary User Journey

1. User creates folders inside `%USERPROFILE%\.icons`.
2. User places `.ico` files in those folders.
3. Icon Replacer discovers those categories automatically.
4. The app and supported context-menu path show the categories and icon entries without a manual rebuild.
5. Choosing a context-menu icon applies it immediately without confirmation or a management window.

## Management App Journey

1. User launches Icon Replacer from its normal shortcut.
2. The first screen shows real previews for the Icon Library.
3. User filters by collection, searches, and changes preview size without a disk rescan.
4. User imports icons or opens the `.icons` folder.
5. User reviews recent changes and restores an eligible target.
6. User follows the system theme or selects Light or Dark.

## V1 Scope

- Folder Target icon changes through `desktop.ini`.
- `.lnk` Shortcut Target icon changes through Shell link APIs.
- Local `.ico` files only.
- `%USERPROFILE%\.icons` library creation and discovery.
- One-level Icon Categories.
- Import and dedupe into `.icons\Imported`.
- Restore Records for every Icon Mutation.
- Compact WinUI management app.
- Real-time visual icon previews with virtualized, asynchronous loading.
- Light, Dark, System, High Contrast, Mica, and restrained Acrylic support.
- CLI/test harness for proof and diagnostics.
- Clear errors for invalid icons, permissions, unsupported target types, and Explorer cache delays.

## Out Of Scope For V1

- PNG/SVG conversion.
- EXE/DLL/ICL icon extraction.
- `.url` internet shortcuts.
- Multi-select batch apply.
- Admin/elevated modification of protected folders.
- Remote, WebDAV, or untrusted network folder support.
- Arbitrarily deep category nesting.
- Enterprise all-users install.

## Acceptance Criteria

- A valid `.ico` can be applied to a normal folder and restored.
- A valid `.ico` can be applied through a local directory junction or symbolic link and restored while preserving the selected link.
- A valid `.ico` can be applied to a normal `.lnk` and restored.
- Existing unrelated `desktop.ini` settings survive folder icon changes.
- Invalid icon files leave the Target unchanged.
- Permission failures leave the Target unchanged and show a clear recovery message.
- The Icon Library is created automatically and can be opened from the app.
- `.icons\<category>\<name>.ico` appears as a category/item in app catalog and supported context-menu catalog.
- Every visible icon item in the management app has a real preview or a clear per-item load/error state.
- Route switches, search, collection filters, and theme changes do not run tooling probes or rescan the library.
- Context-menu commands never reveal the management window.
- The app remains usable with keyboard, high contrast, and 200% scaling.
- Uninstall removes shell integration and preserves the user's Icon Library unless explicitly told otherwise.

## Success Metric

A reviewer can complete the main folder, directory-link, and shortcut flows, then restore them without editing registry or `desktop.ini` manually.
