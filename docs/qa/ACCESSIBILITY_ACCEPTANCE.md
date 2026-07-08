# Accessibility Acceptance

Status: proposed
Date: 2026-07-07

## Keyboard

- Every command is reachable without a mouse.
- Focus order follows visual order.
- Dialogs return focus to the invoking control.
- Restore and error details are keyboard reachable.

## Names and Semantics

- Icon-only controls have accessible names.
- Icon tiles expose both icon name and category.
- Errors are exposed as persistent UI, not toast-only.

## Visual Adaptation

- High contrast keeps controls and status readable.
- 200% scaling does not clip primary commands or paths.
- Long paths wrap or elide professionally with tooltip/details.

## CLI Planning Proof

Run before WinUI implementation:

```powershell
dotnet run --no-build --project src\IconReplacer.Cli -- accessibility-plan
```

The command should list keyboard, names/semantics, visual adaptation, and manual proof requirements plus the WinUI surfaces that must satisfy them. This is planning evidence only; it does not replace screenshots or keyboard/high-contrast proof once the WinUI app exists.

## Proof

Capture screenshots or notes for:

- first run at normal scaling,
- first run at 200%,
- high contrast,
- keyboard-only import/restore path,
- error state.
