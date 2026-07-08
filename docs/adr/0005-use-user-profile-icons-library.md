# Use User Profile Icon Library

Status: accepted

Icon Replacer uses `%USERPROFILE%\.icons` as the user-owned Icon Library. Selected external icons are copied into this library so applied icons do not depend on fragile original file locations.

## Consequences

- First run creates the Icon Library.
- Imported icons go under `.icons\Imported`.
- One-level subfolders are canonical Icon Categories.
- The library is preserved on uninstall unless the user explicitly deletes it.

