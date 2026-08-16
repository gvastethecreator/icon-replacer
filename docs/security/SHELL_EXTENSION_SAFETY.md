# Shell Extension Safety

Status: implemented; monitored through native smoke proof
Date: 2026-07-14

## Rule

Explorer integration must be thin. The shell command should identify the selected Target, present bounded commands, and delegate mutation behavior to tested code.

## Allowed In Shell Path

- Determine whether selection is a supported folder or `.lnk`.
- Enumerate a bounded, local Icon Library catalog.
- Limit the classic menu to 8 collections, 30 icons per collection, and 242 command ids total.
- Decode the bounded catalog into 32-bit premultiplied-alpha menu bitmaps and attach them before `QueryContextMenu` returns.
- Open a native file picker for `.ico`.
- Launch or call tested mutation flow.
- Return clear failure status.

## Not Allowed In Shell Path

- deep recursive scanning,
- network scans,
- blocking operations with no timeout,
- parsing unbounded icon collections,
- arbitrary EXE/DLL icon extraction,
- heavy WinUI surfaces hosted inside Explorer,
- mutation logic duplicated from Core Engine.

## Failure Policy

If shell integration cannot safely enumerate or execute, it should degrade to `Change icon...` or a disabled overflow note rather than risk Explorer instability. Context-menu commands never open the management UI.

An unreadable individual `.ico` preview must not fail or hide the containing menu. The item remains usable with the application bitmap as fallback while other valid entries retain their real preview.

Concurrent command-host invocations serialize by canonical Target path with a
bounded wait. A busy Target returns a retryable error instead of interleaving
`desktop.ini` or `.lnk` writes; unrelated Targets remain independent.

If restore-history persistence fails after a delegated mutation, AppModel
attempts compensation before releasing the Target lock. A failed compensation
returns both causes as a partial failure instead of reporting success or hiding
the unresolved Target state.

The same rule applies after restore: if the `Restored` status cannot be saved,
the recorded applied icon is reapplied before the lock is released, keeping the
Target consistent with the still-`Applied` durable record.

## Preview Performance Evidence

Native smoke proof against the real `%USERPROFILE%\.icons` catalog on 2026-07-14 measured:

- complete classic menu query with 187 commands and all previews attached: 102.2-136.0 ms in three consecutive runs, with a 172.2 ms observed cold maximum,
- constrained coexistence query: 8 command ids.

The smoke test verifies every visible collection and icon already has a renderable 32-bit bitmap with nonzero alpha immediately after `QueryContextMenu`, without manually forwarding `WM_INITMENUPOPUP`. It enforces a 250 ms full-catalog query budget and proves the handler respects an Explorer range of only 8 command ids.

## Context-Menu Coexistence

`Test-MsixLifecycle.ps1` snapshots and hashes every non-Icon-Replacer context-menu registration before cleanup, after install, and after uninstall. It fails on any unrelated registry change or unexpected Explorer process restart. Every mutating invocation requires the explicit `-ApproveExplorerRegistration` switch. Fresh-install and interactive proof still require an uninstalled baseline. A separate `-UpgradeInstalledPackage -KeepInstalled` path accepts exactly one installed package only when snapshot preflight proves that the candidate is validly signed, identity-compatible, and strictly newer; it updates without the duplicate-prone pre-install uninstall. `-InteractiveProof` holds the approved clean-baseline manual session and owns uninstall plus baseline verification. Before registration it persists the restore-state baseline with current-user DPAPI protection, and `-RecoverInterruptedProof` can finish cleanup after a terminated shell without reinstalling. The recovery snapshot is stored under `artifacts\context-menu-recovery\20260713-185129`.
