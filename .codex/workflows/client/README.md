# Client Sync Workflow

`sync-interstella-client.ps1` keeps `C:\Unity\interStellaClient` aligned with the main project for multiplayer regression checks.

Usage:

```powershell
# Incremental sync (default)
powershell -ExecutionPolicy Bypass -File .\.codex\workflows\client\sync-interstella-client.ps1

# Dry run
powershell -ExecutionPolicy Bypass -File .\.codex\workflows\client\sync-interstella-client.ps1 -WhatIf

# Mirror mode (deletes extra files in target include paths)
powershell -ExecutionPolicy Bypass -File .\.codex\workflows\client\sync-interstella-client.ps1 -Mirror
```

Notes:
- Default include set syncs `Assets/Game`, package manifests, and key `ProjectSettings` files.
- Use `-Include` to override include paths.
