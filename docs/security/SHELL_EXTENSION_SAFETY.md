# Shell Extension Safety

Status: proposed
Date: 2026-07-07

## Rule

Explorer integration must be thin. The shell command should identify the selected Target, present bounded commands, and delegate mutation behavior to tested code.

## Allowed In Shell Path

- Determine whether selection is a supported folder or `.lnk`.
- Enumerate a bounded, local Icon Library catalog.
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

If shell integration cannot safely enumerate or execute, it should degrade to `Change icon...` or open the WinUI app rather than risk Explorer instability.
