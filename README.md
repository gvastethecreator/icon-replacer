# Icon Replacer

Icon Replacer is a Windows utility for changing folder and `.lnk` shortcut icons from File Explorer. It keeps a reusable local icon library and records enough state to restore each change.

![Icon Replacer gallery](docs/design/gallery-first-light.png)

## Features

- Windows 11 modern and classic File Explorer context-menu integration
- Gallery-first WinUI 3 management app
- Reversible folder and shortcut icon changes
- Local `%USERPROFILE%\.icons` library with collections and imports
- Bounded native shell menu with icon previews
- CLI and automated tests for the shared Core and AppModel behavior

## Status

Icon Replacer is under active development. Core icon mutation, restore history, the WinUI app, command host, and packaged Explorer handlers are implemented. Release validation still requires the remaining manual Explorer, accessibility, and clean-uninstall proof described in the [verification guide](docs/development/VERIFICATION.md).

The repository is not yet licensed for reuse. See [License](#license).

## Requirements

- Windows 10 version 1809 or later; Windows 11 for the modern Explorer menu
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Developer Mode and the WinApp CLI for packaged WinUI launch
- Visual Studio C++ Build Tools and CMake for the native shell extension
- Python 3 with Pillow only when regenerating app-icon assets

## Quick Start

Clone the repository and run the managed test suite:

```powershell
git clone https://github.com/gvastethecreator/icon-replacer.git
cd icon-replacer
dotnet test tests\IconReplacer.Core.Tests\IconReplacer.Core.Tests.csproj
```

Build and launch the packaged management app:

```powershell
.\BuildAndRun.ps1 src\IconReplacer.App\IconReplacer.App.csproj -Detach
```

Regenerate every app and package icon from the single canonical source:

```powershell
python -m pip install Pillow
.\scripts\Build-AppIconAssets.ps1
```

Do not run package-lifecycle commands that register Explorer handlers without reviewing the guarded workflow and explicitly approving the registration switch.

## Documentation

- [Documentation index](docs/INDEX.md)
- [Architecture](docs/architecture/ARCHITECTURE.md)
- [Architecture review](docs/architecture/architecture-review-2026-07-15.md)
- [Installation and packaging](docs/operations/INSTALLATION.md)
- [Testing](docs/qa/TEST-PLAN.md)
- [Security model](docs/security/SHELL_EXTENSION_SAFETY.md)
- [Contributing](CONTRIBUTING.md)
- [Security policy](SECURITY.md)

## License

No license has been selected yet. Copyright law therefore applies by default, and this repository does not currently grant permission to use, modify, or redistribute the code. A license must be chosen before a reusable public release.
