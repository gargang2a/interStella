# Netcode Regression Workflow

`run-reconnect-regression.ps1` automates the reconnect slot-race verification:

1. Launch Client A and wait for connection readiness.
2. Force-stop Client A.
3. Launch Client B before server timeout cleanup completes.
4. Validate host `Editor.log` sequence:
   - `No available slot ... Queued for reassignment.`
   - `Released slot 1 ...`
   - `Assigned client <newId> to slot 1 (PlayerB).`

Usage:

```powershell
powershell -ExecutionPolicy Bypass -File .\.codex\workflows\netcode\run-reconnect-regression.ps1

# Optional: include reconnect-side interaction/repair checks after reassignment
powershell -ExecutionPolicy Bypass -File .\.codex\workflows\netcode\run-reconnect-regression.ps1 -ReconnectAutoInteractCount 1

# Slower machines: extend boot timeout
powershell -ExecutionPolicy Bypass -File .\.codex\workflows\netcode\run-reconnect-regression.ps1 -ClientBootTimeoutSec 360
```

One-command workflow (sync + reconnect regression):

```powershell
powershell -ExecutionPolicy Bypass -File .\.codex\workflows\netcode\run-e2e-sync-regression.ps1

# Retry reconnect regression on transient startup failures
powershell -ExecutionPolicy Bypass -File .\.codex\workflows\netcode\run-e2e-sync-regression.ps1 -RegressionMaxAttempts 3 -RetryDelaySec 10

# Steam strict end-to-end (sync -> interaction strict -> reconnect)
powershell -ExecutionPolicy Bypass -File .\.codex\workflows\netcode\run-e2e-sync-regression.ps1 -UseSteamBootstrap -StrictSteamRelay
```

The wrapper fails fast if host is not listening on UDP `7770`.

Interaction regression (owner boundary + delivery loop):

```powershell
powershell -ExecutionPolicy Bypass -File .\.codex\workflows\netcode\run-interaction-regression.ps1

# Increase post-interaction wait when scene/network is slower
powershell -ExecutionPolicy Bypass -File .\.codex\workflows\netcode\run-interaction-regression.ps1 -PostInteractWaitSec 40 -AutoInteractCount 2
```

Steam relay binder smoke (client bootstrap path):

```powershell
# Steam bootstrap + strict relay (no direct fallback expected)
powershell -ExecutionPolicy Bypass -File .\.codex\workflows\netcode\run-interaction-regression.ps1 `
  -UseSteamBootstrap `
  -StrictSteamRelay `
  -SteamInviteLobbyId "local-regression" `
  -SteamInviteHostId "127.0.0.1:7770" `
  -PostInteractWaitSec 55 `
  -AutoInteractCount 2
```

Validation targets:
- owner interaction request accepted with caller/owner parity
- at least one committed interaction
- at least one repair delivery accepted log
- when `-UseSteamBootstrap`:
  - client log contains Steam bootstrap + binder applied logs
  - strict mode (`-StrictSteamRelay`) has no direct fallback log
- durable vs transient markers:
  - host: fuel transient accept, tether durable publish/snapshot
  - client: fuel durable apply, tether durable apply
  - when reconnect auto-interact is enabled (`-ReconnectAutoInteractCount > 0`):
    - host: repair durable publish + delivery sequence checks
    - client: repair transient delivery event checks
  - no host-side delivery duplicate/reorder

Prerequisites:
- Host Unity editor for `C:\Unity\interStella` is running and already in Play mode.
- `C:\Unity\interStellaClient` exists.
- Unity editor path in script matches installed version.
- Run client sync before strict regression after code changes:
  - `powershell -ExecutionPolicy Bypass -File .\.codex\workflows\client\sync-interstella-client.ps1`

Camera mode smoke (Gate4) uses Unity debug menu actions (no separate PowerShell runner yet):
1. `Tools/InterStella/Debug/Camera/Set First Person`
2. `Tools/InterStella/Debug/Camera/Set Third Person`
3. `Tools/InterStella/Debug/Camera/Set Overview`
4. `Tools/InterStella/Debug/Camera/Run Mode Smoke (1-2-3)`

Pass evidence:
- Console includes `[InterStella][CameraSmoke] PASS ...`
- Optional screenshots:
  - `Assets/Screenshots/gate4_camera_firstperson_v2.png`
  - `Assets/Screenshots/gate4_camera_thirdperson_v2.png`
  - `Assets/Screenshots/gate4_camera_overview_v2.png`
