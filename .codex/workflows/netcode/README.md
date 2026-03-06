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

Prerequisites:
- Host Unity editor for `C:\Unity\interStella` is running and already in Play mode.
- `C:\Unity\interStellaClient` exists.
- Unity editor path in script matches installed version.
