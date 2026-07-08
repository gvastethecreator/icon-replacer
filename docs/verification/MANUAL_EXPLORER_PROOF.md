# Manual Explorer Proof

Status: proposed
Date: 2026-07-07

## Required Screenshots

- Context menu on a folder.
- Context menu on a `.lnk`.
- `Change icon...` picker.
- Folder before apply.
- Folder after apply.
- Folder after restore.
- Shortcut before apply.
- Shortcut after apply.
- Shortcut after restore.
- Invalid icon error.
- Permission failure error.
- Empty Icon Library state.
- Icon Category from `.icons\<category>`.

## Required Notes

- Windows version/build.
- Chosen integration path: Modern or Classic.
- Whether Explorer updated immediately or cache lagged.
- Whether disk state and UI state disagreed.
- Any manual refresh needed.

## Acceptable Cache Behavior

It is acceptable for Explorer UI to lag if:

- the on-disk state is correct,
- the app reports the cache caveat,
- a refresh/reopen path resolves it,
- this behavior is captured in proof.
