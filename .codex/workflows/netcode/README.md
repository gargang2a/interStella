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

# Stabilize reconnect-side auto-interact when reassignment is slower
powershell -ExecutionPolicy Bypass -File .\.codex\workflows\netcode\run-reconnect-regression.ps1 `
  -ReconnectAutoInteractCount 1 `
  -ReconnectAutoInteractMaxAttempts 120 `
  -ReconnectAutoInteractInitialDelaySec 10 `
  -ReconnectAutoInteractIntervalSec 0.5 `
  -UseSteamBootstrap `
  -StrictSteamRelay

# Slower machines: extend boot timeout
powershell -ExecutionPolicy Bypass -File .\.codex\workflows\netcode\run-reconnect-regression.ps1 -ClientBootTimeoutSec 360

# Retry client startup inside reconnect workflow (ConnectionFailed/timeout/exited)
powershell -ExecutionPolicy Bypass -File .\.codex\workflows\netcode\run-reconnect-regression.ps1 `
  -StartupRetryMaxAttempts 3 `
  -StartupRetryDelaySec 10
```

One-command workflow (sync + reconnect regression):

```powershell
powershell -ExecutionPolicy Bypass -File .\.codex\workflows\netcode\run-e2e-sync-regression.ps1

# Retry reconnect regression on transient startup failures
powershell -ExecutionPolicy Bypass -File .\.codex\workflows\netcode\run-e2e-sync-regression.ps1 -RegressionMaxAttempts 3 -RetryDelaySec 10

# Retry client startup inside interaction/reconnect phases
powershell -ExecutionPolicy Bypass -File .\.codex\workflows\netcode\run-e2e-sync-regression.ps1 `
  -StartupRetryMaxAttempts 3 `
  -StartupRetryDelaySec 10

# Steam strict end-to-end (sync -> interaction strict -> reconnect)
powershell -ExecutionPolicy Bypass -File .\.codex\workflows\netcode\run-e2e-sync-regression.ps1 -UseSteamBootstrap -StrictSteamRelay

# End-to-end with reconnect auto-interact verification enabled
powershell -ExecutionPolicy Bypass -File .\.codex\workflows\netcode\run-e2e-sync-regression.ps1 `
  -UseSteamBootstrap `
  -StrictSteamRelay `
  -ReconnectAutoInteractCount 1
```

The wrapper fails fast if host is not listening on UDP `7770`.

Interaction regression (owner boundary + delivery loop):

```powershell
powershell -ExecutionPolicy Bypass -File .\.codex\workflows\netcode\run-interaction-regression.ps1

# Increase post-interaction wait when scene/network is slower
powershell -ExecutionPolicy Bypass -File .\.codex\workflows\netcode\run-interaction-regression.ps1 -PostInteractWaitSec 40 -AutoInteractCount 2

# Retry client startup inside interaction workflow
powershell -ExecutionPolicy Bypass -File .\.codex\workflows\netcode\run-interaction-regression.ps1 `
  -StartupRetryMaxAttempts 3 `
  -StartupRetryDelaySec 10
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

Steam manual smoke helpers:
- Play mode menu actions:
  - Tools/InterStella/Debug/Steam/Log Session Snapshot
  - Tools/InterStella/Debug/Steam/Copy Join Launch Args
  - Tools/InterStella/Debug/Steam/Invite Configured Friend
- Client launcher helper:
  - powershell -ExecutionPolicy Bypass -File .\.codex\workflows\client\launch-steam-client.ps1 -UseClipboardJoinArgs -StrictSteamRelay -WaitForBoot
- Recommended manual flow:
  1. Host editor enters Play with Steam provider enabled.
  2. Run Log Session Snapshot and confirm a non-empty lobbyId plus initialized bootstrap.
  3. Run Copy Join Launch Args.
  4. Launch the clone client editor with launch-steam-client.ps1 -UseClipboardJoinArgs.
  5. If overlay invite is needed again, run Invite Configured Friend.

Steam build smoke helpers:
- Build from the open Unity editor when local ignored art/assets must be included:
  - Tools/InterStella/Build/Build Steam Smoke Windows64
- Batch build wrapper (requires the project to be closed and Unity Hub licensing args):
  - powershell -ExecutionPolicy Bypass -File .\.codex\workflows\netcode\build-steam-smoke.ps1
- OneDrive/shared folder publish helper:
  - powershell -ExecutionPolicy Bypass -File .\.codex\workflows\netcode\publish-steam-smoke-build.ps1
  - or double-click `.codex\workflows\netcode\publish-steam-smoke-build.bat`
- Current branch sync + batch build wrapper for desktop/laptop use:
  - powershell -ExecutionPolicy Bypass -File .\.codex\workflows\netcode\sync-and-build-steam-smoke.ps1
  - or double-click `.codex\workflows\netcode\sync-and-build-steam-smoke.bat`
- Built executable launcher:
  - Host:
    - powershell -ExecutionPolicy Bypass -File .\.codex\workflows\netcode\launch-steam-build-smoke.ps1 -Mode host -WaitForBoot
  - Client:
    - powershell -ExecutionPolicy Bypass -File .\.codex\workflows\netcode\launch-steam-build-smoke.ps1 -Mode client -JoinArgs "-interstella-provider steam +connect_lobby <lobbyId>" -StrictSteamRelay -WaitForBoot
- Recommended build smoke flow:
  1. Build once from the Unity menu in the main project.
  2. Launch the built host executable and wait for Steam lobby creation.
  3. Confirm `current-steam-lobby.txt` appears in the build folder and contains the latest `lobby_id`.
  4. Launch the built client executable. `RunClient.bat` will use the shared lobby file automatically when no lobby id argument is provided.
- Recommended dual-machine flow for one developer using desktop + laptop:
  1. Desktop: commit + push current branch.
  2. Laptop:
     - powershell -ExecutionPolicy Bypass -File .\.codex\workflows\netcode\sync-and-build-steam-smoke.ps1
     - or `.codex\workflows\netcode\sync-and-build-steam-smoke.bat`
  3. Confirm `Builds/SteamSmokeWindows64/build-info.txt` matches the expected branch/commit.
  4. Run:
     - `Builds/SteamSmokeWindows64/RunHost.bat`
     - `Builds/SteamSmokeWindows64/RunClient.bat <lobbyId>`

- Recommended desktop build -> OneDrive publish -> laptop run flow:
  1. Desktop: build from Unity menu.
  2. Desktop: run `.codex\workflows\netcode\publish-steam-smoke-build.bat`
  3. Desktop: run `RunHost.bat` from the OneDrive build folder and wait for `current-steam-lobby.txt` to refresh.
  4. Laptop: open OneDrive path `OneDrive\interStellaBuilds\SteamSmokeWindows64`
  5. Laptop: run `RunClient.bat`
