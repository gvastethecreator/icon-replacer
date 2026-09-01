# Security policy

## Reporting a vulnerability

Use the repository's private GitHub Security Advisory flow. Include affected paths, reproduction steps, impact, and any safe proof you can share. Do not include credentials, personal data, or a working exploit in a public issue.

If private reporting is unavailable, open a public issue that asks the maintainer to enable a private channel. Do not disclose vulnerability details there.

## Scope

Security-sensitive areas include:

- File Explorer context-menu handlers and COM registration
- folder and shortcut mutations
- `desktop.ini` parsing and writes
- restore-state persistence and rollback
- icon parsing, imports, and path validation
- package install, upgrade, and uninstall lifecycle

The current development branch is the only supported target until a release policy is published.

## Safe testing

- Use temporary local targets and disposable icons.
- Do not test against network, cloud-backed, system, or other users' paths.
- Do not register or reload Explorer handlers without explicit approval.
- Preserve unrelated context-menu registrations and user icon data.