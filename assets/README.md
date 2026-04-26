# Runtime-generated assets

This prototype intentionally avoids committing binary art files.

Map tiles, unit icons, and resource icons are generated procedurally at runtime in:
- `scripts/presentation/MapView.cs`

This keeps the repository text-only and compatible with environments that do not support binary file patches.
