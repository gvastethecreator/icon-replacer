# Desktop.ini hardening policy

Status: living policy
Date: 2026-07-07

## Context

Windows security updates released on or after June 9, 2026 can cause Windows to ignore `desktop.ini` when the source is not trusted. That includes Mark-of-the-Web, WebDAV/HTTP locations, or network paths outside trusted zones.

## V1 policy

- Support local trusted folder targets.
- Block or warn on UNC, WebDAV, HTTP-backed, or untrusted remote targets.
- Do not run `Unblock-File` automatically.
- Do not change zone policy.
- Do not bypass Windows security hardening.
- Report when the disk mutation succeeded but Windows may ignore presentation due to trust policy.

## Acceptance

Remote/untrusted cases must produce an explicit result, not a silent success.