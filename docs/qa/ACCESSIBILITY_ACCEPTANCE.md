# Accessibility acceptance

Status: living acceptance contract
Date: 2026-07-07

## Keyboard

- Every command is reachable without a mouse.
- Focus order follows visual order.
- Dialogs return focus to the invoking control.
- Restore and error details are keyboard reachable.

## Names and semantics

- Icon-only controls have accessible names.
- Icon tiles expose both icon name and category.
- Errors are exposed as persistent UI, not toast-only.

## Visual adaptation

- High contrast keeps controls and status readable.
- 200% scaling does not clip primary commands or paths.
- Long paths wrap or elide with tooltip/details.

## CLI planning proof

```powershell
dotnet run --no-build --project src\IconReplacer.Cli -- accessibility-plan
```

The command lists keyboard, names/semantics, visual adaptation, and manual proof requirements plus the WinUI surfaces that must satisfy them. This is planning evidence only. It does not replace screenshots or keyboard/high-contrast proof.

## Proof

Capture screenshots or notes for:

- first run at normal scaling,
- first run at 200%,
- high contrast,
- keyboard-only import/restore path,
- error state.