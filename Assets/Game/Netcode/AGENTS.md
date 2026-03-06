# AGENTS.md

This file augments the repository root `/AGENTS.md`.
Rules here are stricter for code under `Assets/Game/Netcode`.

## Scope
- Network authority and ownership rules
- Session-state replication
- RPC and event flow
- Spawn and despawn flow
- Connection lifecycle behavior
- Network-facing utilities and adapters
- Steam lobby or relay integration only where it touches gameplay networking boundaries

## Default Networking Assumption
Unless the repository clearly shows otherwise, assume:
- FishNet-based gameplay networking
- Steam lobbies for session and invite flow
- host-authoritative sessions for MVP
- no host migration in MVP unless explicitly implemented and tested

Do not silently introduce dedicated-server-only assumptions into MVP code.

## Primary Goal
Netcode must make gameplay truth explicit.
The purpose of this folder is not to hide complexity. The purpose is to centralize authority, replication, correction, and lifecycle rules so gameplay code stays understandable.

## Non-Negotiable Rules
- Every new networked feature must declare:
  1. authority owner
  2. ownership model
  3. durable replicated state
  4. transient events or RPCs
  5. correction or reconciliation behavior
  6. late join or reconnect behavior if supported
- Do not solve desync by blindly adding more transform sync.
- Do not replicate visual-only state as if it were gameplay truth.
- Do not mix transport concerns, Steam API plumbing, and gameplay rules in one class.
- Do not spread ownership decisions across random feature scripts with no central explanation.

## Replication Rules
- Prefer replicating gameplay meaning over raw visual detail.
- Durable state should use state replication primitives.
- Short-lived events should use explicit RPC or event messaging.
- Do not use reliable RPC spam every frame for state that should be modeled as replicated state.
- When a value affects gameplay decisions across peers, make sure there is one authoritative source.
- If the project uses FishNet:
  - prefer `Replicate` and `Reconcile` patterns for movement-critical logic
  - prefer SyncTypes or equivalent for durable state
  - do not use ad hoc observers-only visual replication as a substitute for gameplay truth

## Ownership and Authority Rules
- Host-authoritative means the host decides final gameplay truth.
- Clients may request actions, but requests are not final truth.
- Spawning, despawning, item ownership transfer, repair completion, and match progression must have explicit authority flow.
- If ownership changes at runtime, document the trigger and failure behavior.
- Do not assume a component's local instance is authoritative just because it is currently controlled by the player.

## Lifecycle Rules
- Spawns and despawns must be traceable.
- Avoid hidden side effects during `OnStartClient`, `OnStartServer`, `OnStopClient`, and related network lifecycle hooks.
- Handle scene load and unload intentionally.
- Preserve clear behavior for:
  - fresh join
  - disconnect
  - reconnect if supported
  - respawn
  - round reset or station reset
- If late join is not supported for a system in MVP, state that clearly instead of partially faking support.

## Feature Boundary Rules
- Keep gameplay features responsible for game rules.
- Keep netcode responsible for ownership, replication, correction, and lifecycle.
- Do not let feature folders invent parallel networking conventions.
- Cross-feature network rules should live here or in clearly shared abstractions, not in duplicate local helpers.

## Debugging Rules
- Network logs must help answer who owns what, who decided what, and when correction happened.
- Include authority or owner context in debug output when debugging network issues.
- Keep noisy logging behind debug flags or development-only tooling.
- Do not leave high-volume logs in hot paths.

## Performance Rules
- Network bandwidth is a gameplay budget.
- Be suspicious of per-frame reliable messages, oversized payloads, and unnecessary observer updates.
- Batch or compress only when the gameplay meaning stays clear.
- Do not hide an unclear authority model behind aggressive send frequency.

## Testing Expectations
Before considering a netcode change complete, validate at minimum:
- host behavior
- owning client behavior
- remote observer behavior
- spawn and despawn flow
- ownership transfer if the change touches items or interactions
- scene transition or reset behavior if affected
- delayed packet or mild jitter behavior in a controlled test when possible
- mismatch or correction behavior for movement-critical changes

## Review Checklist
- [ ] Authority and ownership are explicit.
- [ ] Durable state and transient events are separated.
- [ ] The change did not rely on transform sync as a shortcut.
- [ ] Feature code did not silently gain network authority decisions.
- [ ] Spawn, despawn, and reset behavior were considered.
- [ ] Logging and diagnostics remain usable.
- [ ] MVP assumptions such as no host migration remain clear.

## When In Doubt
- Prefer a narrower, explicit host-authoritative model over a broader but ambiguous networking scheme.
- Prefer documenting unsupported cases over pretending they work.
- Prefer compact gameplay-state replication over syncing presentation detail.
