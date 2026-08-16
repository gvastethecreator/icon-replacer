# Contributing

Icon Replacer is a Windows-focused .NET, WinUI, and native shell project. Keep changes small, preserve unrelated work, and include evidence for behavior that touches Explorer or the filesystem.

## Development setup

1. Install the prerequisites in [Installation and Packaging](docs/operations/INSTALLATION.md).
2. Restore and run the managed tests:

   ```powershell
   dotnet test tests\IconReplacer.Core.Tests\IconReplacer.Core.Tests.csproj
   ```

3. For native shell work, run the focused build and smoke test:

   ```powershell
   .\scripts\Build-NativeShellExtension.ps1 -Configuration Debug -Platform x64
   ```

4. Launch the packaged app through `BuildAndRun.ps1` or `winapp`; do not run the packaged executable directly.

## Pull requests

- Explain the user-visible behavior and risk.
- Add or update focused tests.
- Keep Explorer registration behind the approval-gated lifecycle script.
- Update architecture, ADR, security, or verification docs when their contracts change.
- Do not commit generated output under `artifacts/`, local reports, credentials, or personal test data.

## Reporting security issues

Do not open a public issue for a vulnerability. Follow [SECURITY.md](SECURITY.md).
