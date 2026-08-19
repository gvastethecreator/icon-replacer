# Icon Replacer certification notes

Copy and adapt this document immediately before Partner Center submission. Replace bracketed values and update the date.

## Notes for certification

**Notes date:** `[YYYY-MM-DD]`  
**Product:** Icon Replacer  
**Store ID:** `[STORE ID]`  
**Package version:** `[MAJOR.MINOR.BUILD.REVISION]`  
**Submission commit:** `[GIT SHA]`  
**Architecture:** x64

Icon Replacer is a local-first Windows desktop utility for changing and restoring icons on folders and `.lnk` shortcuts. The package includes a WinUI management application, a command host, a native packaged COM server, Windows 11 File Explorer commands, and a classic Explorer context-menu handler.

No Icon Replacer account or test credentials are required.

### Recommended disposable test assets

Create these items in a normal user-owned test folder:

```text
C:\Users\<tester>\Desktop\IconReplacer-Certification-Test\
  Folder A\
  Folder B\
  Test Shortcut.lnk
  Test Icon.ico
```

Use a valid local `.ico` containing common Windows sizes. Do not use Program Files, Windows system directories, a network share, a reparse point, a cloud placeholder, or customer data for the basic path.

### Basic app test path

1. Install the submitted x64 package.
2. Launch **Icon Replacer** from Start.
3. Import `Test Icon.ico` into the local Icon Library.
4. Confirm the imported icon appears and can be filtered/searched.
5. Open the context menu on `Folder A`.
6. On Windows 11, confirm the modern **Change icon...** and collections commands appear.
7. Open **Show more options** or use Windows 10 and confirm the classic context-menu handler appears.
8. Apply `Test Icon.ico` to `Folder A`.
9. Confirm Explorer refreshes and a restore record is created.
10. Open **Recent** in Icon Replacer and restore the previous folder icon.
11. Repeat with `Test Shortcut.lnk`.
12. Confirm the original folder, shortcut target, and source `.ico` remain present and unchanged.

### Modern versus classic Explorer menus

- Windows 11 provides packaged modern commands through `windows.fileExplorerContextMenus`.
- Windows 10 and Windows 11 **Show more options** use the packaged classic context-menu handler.
- Explorer may require a short refresh after install, update, or uninstall. Restart Explorer only after giving package registration time to settle.

### Local data behavior

- Imported `.ico` files and collections are stored under `%USERPROFILE%\.icons`.
- Restore records contain local target and icon references needed to reverse a supported change.
- Folder changes may update `desktop.ini` and Windows-required file attributes.
- Shortcut changes use Windows shell interfaces to update `.lnk` icon metadata.
- Icon Replacer is designed to preserve unrelated `desktop.ini` content.
- Original folders, shortcuts, shortcut targets, and source icons are not deleted.
- Normal use does not automatically elevate.

### Expected safe failures

The application should report a clear error and avoid partial mutation when:

- the selected icon is malformed or unsupported;
- the target is protected or not writable;
- the target resolves through a blocked reparse/untrusted path;
- Windows security policy ignores an untrusted `desktop.ini` source;
- a referenced icon or target no longer exists.

Icon Replacer does not automatically unblock untrusted files or bypass Windows policy.

### Update behavior

The submitted Store package is compiled with:

```text
ICON_REPLACER_DISTRIBUTION_CHANNEL=store
```

Microsoft Store manages package updates. The in-app update service returns a Store-managed message without sending an HTTP request to GitHub Releases.

The GitHub release candidate is a separate distribution channel and is not the package submitted here.

### Update and uninstall notice

Because Explorer loads the packaged native shell extension, update and uninstall testing should include both of these states:

1. Explorer running with the extension previously invoked.
2. Explorer restarted before the operation.

After uninstall:

- modern and classic menu entries should disappear;
- packaged COM activation should no longer resolve;
- Explorer should remain stable;
- original targets should remain present;
- `%USERPROFILE%\.icons` follows the documented preservation policy and may remain so applied icon references are not broken.

### Support and privacy

- Privacy policy: `[PUBLIC PRIVACY POLICY URL]`
- Product website: `[PUBLIC PRODUCT URL]`
- Support: `[PUBLIC SUPPORT URL OR EMAIL]`

## Restricted capability: `runFullTrust`

Icon Replacer is a native desktop application with packaged Explorer integration. It requires `runFullTrust` to perform these user-requested desktop operations:

1. Read and validate local `.ico` files explicitly selected by the user.
2. Read and update folder `desktop.ini` content and required file attributes for Windows custom-folder icons.
3. Read and update `.lnk` shortcut icon metadata through Windows shell interfaces.
4. Create local restore records before supported mutations and restore earlier icon references.
5. Operate a packaged native COM server used by Windows File Explorer context menus.
6. Receive the folder or shortcut selection supplied by Explorer for an explicit Icon Replacer command.
7. Delegate bounded operations to the packaged command host and management application.
8. Notify Explorer that icon metadata changed so the shell can refresh.
9. Read and write the per-user `%USERPROFILE%\.icons` library.

Icon Replacer does not use `runFullTrust` to elevate silently, bypass access controls, inject into Explorer, read unrelated file contents, monitor arbitrary Explorer selections, copy credentials, upload local paths or icon files, or disable Windows trust protections.

## Packaged COM and Explorer declarations

The package registers these stable classes:

| Purpose | CLSID |
| --- | --- |
| Change icon command | `B8F1A86D-4C52-4C53-BF72-30B59F7F0F7D` |
| Collections command | `B8F1A86D-4C52-4C53-BF72-30B59F7F0F7E` |
| Classic context menu | `B8F1A86D-4C52-4C53-BF72-30B59F7F0F7F` |

The extension targets folders (`Directory`) and Windows shortcut files (`.lnk`).

## Reviewer troubleshooting

### Context-menu commands are not visible immediately

Wait briefly for package registration, then close and reopen the target Explorer window. On Windows 11 check both the modern menu and **Show more options**. Restart Explorer only if necessary.

### The icon change succeeds but Explorer shows the old image

Explorer caches icons. Close and reopen the folder, refresh Explorer, or restart Explorer. The operation result and restore history should still reflect the completed change.

### A folder icon is ignored

Use a normal local, writable folder and a trusted local `.ico`. Windows may ignore `desktop.ini` from untrusted sources according to current platform policy. Icon Replacer intentionally does not bypass that policy.

### A protected path fails

This is expected. Use a folder under the current user's profile. Normal operation does not request automatic elevation.

### Check for updates does not contact GitHub

This is expected for the Microsoft Store build. Package updates are managed by Microsoft Store.
