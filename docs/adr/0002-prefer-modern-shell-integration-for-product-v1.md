# Prefer Modern Shell Integration For Product V1

Status: accepted

Icon Replacer V1 will use package identity, MSIX registration, and a native `IExplorerCommand` extension for the real Windows 11 context-menu experience. Classic per-user registry verbs remain a prototype or fallback path, not the product V1 target.

## Considered Options

- Modern Shell Integration with package identity and `IExplorerCommand`.
- Classic Shell Integration through `HKCU\Software\Classes`.

## Consequences

- Modern integration better matches the requested right-click experience and dynamic Icon Library behavior.
- Modern integration increases implementation, packaging, signing, and QA complexity.
- Classic integration is faster but may appear under `Show more options` and cannot honestly satisfy dynamic submenus without fragile registry behavior.
