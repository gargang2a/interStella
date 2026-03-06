# AGENTS.md

This file augments the repository root `/AGENTS.md`.
Rules here are stricter for code under `Assets/Game/Features/Player`.

## Scope
- Player input collection
- Player locomotion and movement feel
- Suit fuel consumption hooks related to movement
- Player interaction entry points
- Local presentation hooks for camera, animation, and feedback
- Player-side bridge into netcode and tether systems

## Primary Goal
The player feature must preserve movement feel without blurring gameplay truth.
`Player` owns local responsiveness, but it does not own final authority over networked gameplay state.

## Non-Negotiable Boundaries
- Do not build or extend a giant `PlayerController` that owns movement, tether, fuel, interaction, UI, animation, and netcode in one class.
- Keep responsibilities split. Preferred shape:
  - `PlayerInputReader`
  - `PlayerMotor`
  - `PlayerFuel`
  - `PlayerInteraction`
  - `PlayerState`
  - `PlayerPresentation`
  - `PlayerNetworkBridge`
- `PlayerPresentation` may read gameplay state, but it must not become the source of truth.
- Camera effects, animation parameters, head bob, VFX, and audio feedback are presentation only.
- Do not let player movement code directly mutate station state, repair progress, or global match flow.

## Update Loop Discipline
- Capture raw input in `Update`.
- Run movement simulation in the designated simulation path only. Use `FixedUpdate` or the project tick model consistently.
- Run camera alignment and purely visual smoothing in `LateUpdate` when needed.
- Do not duplicate movement logic across `Update` and `FixedUpdate`.
- Do not hide gameplay corrections inside camera or animation code.

## Movement Rules
- Movement feel is important, but the simulation path must stay explicit.
- Separate these concerns:
  - input sampling
  - movement simulation
  - collision response
  - fuel consumption trigger points
  - presentation smoothing
  - network reconciliation
- Keep movement state explicit. Prefer named states or flags over implicit branching hidden across many methods.
- If movement rules depend on grounded, drifting, boosting, or constrained states, define those states in code rather than inferring them from animation or VFX.
- Do not solve bad feel by bypassing authority checks in gameplay code.

## Fuel Integration Rules
- Fuel is authoritative gameplay state.
- Local prediction for UI or moment-to-moment feel is acceptable only if reconciliation is expected and supported by the feature.
- Fuel consumption must happen from explicit causes such as:
  - thrust
  - boost
  - recovery maneuver
  - tool-assisted movement
- Do not bury fuel drains inside presentation code or animation events.
- When movement changes fuel behavior, update both the gameplay rule and the player-facing feedback path.

## Interaction Rules
- `Player` may initiate interaction requests, but validation must remain explicit and authority-safe.
- Do not put station-specific repair logic directly inside player classes.
- Carry, pickup, drop, and repair entry points should be thin and feature-specific.
- Interaction queries in hot paths must avoid avoidable allocations.
- Cache required references and layer masks. Do not spam `GetComponent`, `Find`, or alloc-heavy queries each frame.

## Tether Integration Rules
- `Player` must not redefine tether truth.
- The player feature may react to tether constraints, but it must not use rope visuals as gameplay input.
- If tether changes movement, the rule must be visible in the motor or constraint layer, not hidden in presentation.
- Do not apply competing local-only forces that fight host-authoritative tether correction.

## Networking Rules For Player Code
- Default assumption unless the repository clearly says otherwise: host-authoritative multiplayer.
- Player code must clearly separate:
  - local-only input and feedback
  - authoritative gameplay state
  - replicated observable state
  - reconciliation or correction handling
- Do not mix local prediction, RPC dispatch, and final authoritative correction in one unreadable method.
- If the project uses FishNet, movement-related changes should favor a clear `Replicate` and `Reconcile` pattern instead of ad hoc transform patching.
- Do not fix jitter by adding more transform sync unless the gameplay truth is already correct.

## Performance Rules
- This folder contains hot-path code. Treat every allocation as suspect.
- Avoid LINQ, closures, boxing, and string formatting in per-frame paths.
- Avoid repeated physics queries when a bounded query or cached result is sufficient.
- Keep debug logging out of hot loops unless explicitly gated behind a debug flag.
- Do not add expensive convenience abstractions inside the movement path without a clear reason.

## Testing Expectations
Before considering a player change complete, validate at minimum:
- local movement feel in an isolated test scene
- host and client movement consistency in a 2-player setup
- movement while tethered to another player
- movement while low on fuel or out of fuel
- movement while carrying scrap or interacting near a station
- spawn, respawn, and scene transition behavior if affected by the change

## Review Checklist
- [ ] Player responsibilities stayed split and readable.
- [ ] No giant `PlayerController` growth was introduced.
- [ ] Fuel remains authoritative.
- [ ] Presentation does not own gameplay truth.
- [ ] Movement and tether interaction were tested together.
- [ ] No avoidable per-frame allocation was introduced.
- [ ] Host, owning client, and remote client behavior were considered.

## When In Doubt
- Prefer a slightly simpler movement model with clear authority over a clever but unstable system.
- Prefer explicit state transitions over hidden side effects.
- Prefer isolated player test scenes before shared scene integration.
