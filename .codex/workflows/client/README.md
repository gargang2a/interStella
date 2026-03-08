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

Steam manual smoke client launch:

```powershell
# Use lobby id directly
powershell -ExecutionPolicy Bypass -File .\.codex\workflows\client\launch-steam-client.ps1 `
  -LobbyId 109775241234567890 `
  -StrictSteamRelay `
  -WaitForBoot

# Use join args copied from the Unity debug menu clipboard
powershell -ExecutionPolicy Bypass -File .\.codex\workflows\client\launch-steam-client.ps1 `
  -UseClipboardJoinArgs `
  -StrictSteamRelay `
  -WaitForBoot

# Preview the exact Unity launch command without starting the client editor
powershell -ExecutionPolicy Bypass -File .\.codex\workflows\client\launch-steam-client.ps1 `
  -LobbyId 109775241234567890 `
  -WhatIfLaunch
```

Notes:
- If `-HubSessionId` or `-AccessToken` are omitted, the script resolves them from the running host Unity editor process for `C:\Unity\interStella`.
- `-WaitForBoot` watches the spawned client log for early Steam/client startup signals and reports timeout/exit/failure reasons.
