# Prefer Modern Shell Integration For Product V1

Status: superseded by ADR-0011

This ADR recorded the first accepted direction: package identity, MSIX registration,
and a native `IExplorerCommand` extension for the Windows 11 context menu. It
treated classic per-user registry verbs as a prototype or fallback.

ADR-0011 supersedes that fallback model. V1 now packages both the modern and
classic Explorer integrations in the same signed per-user MSIX and retires the
raw `HKCU\Software\Classes` verb prototype.

## Considered Options

- Modern Shell Integration with package identity and `IExplorerCommand`.
- Classic Shell Integration through `HKCU\Software\Classes`.

## Consequences

- Modern integration better matches the requested right-click experience and dynamic Icon Library behavior.
- Modern integration increases implementation, packaging, signing, and QA complexity.
- Classic integration is faster but may appear under `Show more options` and cannot honestly satisfy dynamic submenus without fragile registry behavior.
