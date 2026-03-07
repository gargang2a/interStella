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

# Slower machines: extend boot timeout
powershell -ExecutionPolicy Bypass -File .\.codex\workflows\netcode\run-reconnect-regression.ps1 -ClientBootTimeoutSec 360
```

One-command workflow (sync + reconnect regression):

```powershell
powershell -ExecutionPolicy Bypass -File .\.codex\workflows\netcode\run-e2e-sync-regression.ps1

# Retry reconnect regression on transient startup failures
powershell -ExecutionPolicy Bypass -File .\.codex\workflows\netcode\run-e2e-sync-regression.ps1 -RegressionMaxAttempts 3 -RetryDelaySec 10
```

The wrapper fails fast if host is not listening on UDP `7770`.

Interaction regression (owner boundary + delivery loop):

```powershell
powershell -ExecutionPolicy Bypass -File .\.codex\workflows\netcode\run-interaction-regression.ps1

# Increase post-interaction wait when scene/network is slower
powershell -ExecutionPolicy Bypass -File .\.codex\workflows\netcode\run-interaction-regression.ps1 -PostInteractWaitSec 40 -AutoInteractCount 2
```

Validation targets:
- owner interaction request accepted with caller/owner parity
- at least one committed interaction
- at least one repair delivery accepted log

Prerequisites:
- Host Unity editor for `C:\Unity\interStella` is running and already in Play mode.
- `C:\Unity\interStellaClient` exists.
- Unity editor path in script matches installed version.
