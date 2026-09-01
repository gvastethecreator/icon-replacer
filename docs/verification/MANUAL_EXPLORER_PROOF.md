# Manual Explorer proof

Status: in progress. Use the guarded lifecycle script. Do not register Explorer handlers without approval.

## Guarded proof session

For the current installed-candidate upgrade path:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Test-MsixLifecycle.ps1 -SnapshotOnly
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Test-MsixLifecycle.ps1 -UpgradeInstalledPackage -KeepInstalled -ApproveExplorerRegistration
```

The upgrade command is rejected without the approval switch. It requires exactly one identity-compatible older package and leaves the current candidate installed for About, accessibility, duplicate-menu, and preview verification.

Folder-menu checks for a current candidate:

- exactly one **Change icon...** and one **Icon collections** entry
- both entries show the official app icon and share one separator group
- unrelated classic-menu entries remain present
- **Change icon...** opens the native `.ico` picker directly
- canceling the picker does not mutate the target

For the final transactional install/uninstall matrix, start from zero installed Icon Replacer packages. The runner refuses an implicit replacement. After explicit approval, use the clean-baseline session below.

This matrix is approval-gated because it temporarily registers both packaged Explorer handlers:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Test-MsixLifecycle.ps1 -InteractiveProof -ApproveExplorerRegistration
```

The script snapshots unrelated handlers, the Explorer process, the Icon Library, and restore state before installation. It creates `artifacts\explorer-proof\<timestamp>\session.json`, prints the screenshot directory, and waits while the matrix below is completed. Pressing Enter closes the session, uninstalls Icon Replacer, and verifies the original baselines. Failures enter the same cleanup path and record whether cleanup was fully verified.

The original restore-state content is held in memory and also written to a current-user DPAPI-protected recovery file before registration. It is restored after the app stops so apply/restore test records do not pollute real history. The protected copy is removed after cleanup verifies successfully. Do not use `-KeepInstalled` for this matrix or import icons during the proof.

If PowerShell or the terminal is terminated before cleanup, do not launch a new proof. Recover the newest incomplete session without reinstalling anything:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Test-MsixLifecycle.ps1 -RecoverInterruptedProof -ApproveExplorerRegistration
```

Use `-RecoverySessionPath <session.json>` only when selecting a specific session. Recovery removes any package left by that session, restores protected history, and verifies the saved handler, Explorer, Icon Library, and restore-state baselines. It is idempotent for sessions already marked complete.

## Required screenshots

- Context menu on a folder.
- Context menu on a local directory junction.
- Context menu on a local directory symbolic link.
- Context menu on a `.lnk`.
- Classic menu separator grouping the two Icon Replacer commands.
- Application icon on both **Change icon...** and **Icon collections**.
- Representative preview beside every visible collection.
- Actual `.ico` preview beside every visible icon in one opened collection.
- **Change icon...** picker.
- Folder before apply.
- Folder after apply.
- Folder after restore.
- Directory junction after apply and after restore, with the junction still present.
- Directory symbolic link after apply and after restore, with the symlink still present.
- Shortcut before apply.
- Shortcut after apply.
- Shortcut after restore.
- Invalid icon error.
- Permission failure error.
- Empty Icon Library state.
- Icon category from `.icons\<category>`.

## Required notes

- Windows version/build.
- Confirm both packaged integration paths: Windows 11 modern menu and **Show more options** classic menu.
- Confirm the package was installed only for this approved proof and removed afterward.
- Confirm unrelated context-menu entries remained present before, during, and after the proof.
- Confirm direct-picker and collection commands work for both a local directory junction and a local directory symbolic link.
- Confirm apply/restore preserves each selected link and does not replace it with a normal directory.
- Whether Explorer updated immediately or cache lagged.
- Whether disk state and UI state disagreed.
- Any manual refresh needed.

## Acceptable cache behavior

It is acceptable for Explorer UI to lag if:

- the on-disk state is correct,
- the app reports the cache caveat,
- a refresh/reopen path resolves it,
- this behavior is captured in proof.