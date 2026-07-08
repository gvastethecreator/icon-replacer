# Block Or Warn On Untrusted Desktop.ini Targets

Status: accepted

V1 will not bypass Windows `desktop.ini` trust hardening. Remote, WebDAV, HTTP-backed, Mark-of-the-Web, or otherwise untrusted Folder Targets are blocked or warned with a clear result instead of silently claiming success.

## Consequences

- No automatic `Unblock-File`.
- No zone policy changes.
- Local trusted folders are the supported V1 path.
- Negative-path proof is required for remote or untrusted scenarios.

