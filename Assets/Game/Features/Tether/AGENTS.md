# AGENTS.md

This file augments the repository root `/AGENTS.md`.
Rules here are stricter for code under `Assets/Game/Features/Tether`.

## Scope
- Tether gameplay rules
- Constraint and pull behavior between connected players
- Break, reconnect, and limit states if supported
- Tether-side state replication and reconciliation hooks
- Rope or line presentation only when it depends on tether gameplay data

## Primary Goal
Tether is a gameplay rule first and a visual effect second.
This folder must protect the game from turning rope visuals or unstable physics into gameplay truth.

## Canonical Tether Truth
Treat the following as the authoritative tether state, not rope segments:
- connected endpoints
- tether enabled or disabled state
- maximum allowed length
- slack or tension state
- pull or constraint state
- break state or failure state
- optional cooldown or reconnect gating if the design uses it

If a value does not affect gameplay meaning, do not network it as truth.

## Non-Negotiable Rules
- Do not treat `LineRenderer`, spline points, rope bones, or segment transforms as the source of truth.
- Do not network full rope segment simulation as the authoritative gameplay model.
- Do not hide tether rules inside camera shake, VFX, animation, or audio callbacks.
- Do not let both endpoints independently decide the final tether rule without an explicit authority model.
- Do not solve tether bugs by adding more visual interpolation while leaving gameplay state ambiguous.

## Architecture Guidance
Preferred separation:
- `TetherLink` or endpoint ownership data
- `TetherRule` or rule evaluation
- `TetherConstraintSolver` or force / clamp application
- `TetherNetworkState`
- `TetherView`
- optional `TetherDebugView`

Keep view code separate from gameplay constraint logic.
If an effect is purely cosmetic, it belongs in `TetherView` or presentation code.

## Constraint Rules
- Pick a small set of tether states and keep them explicit. Example:
  - slack
  - near limit
  - tension
  - hard limit
  - broken
- Define what the game does at each state. For example:
  - no effect
  - soft pull
  - constrained movement
  - forced correction
  - break event
- Do not mix multiple hidden force models at once.
- If using impulses, clamping, or assisted pull, document which rule is authoritative and when it applies.
- Apply tether gameplay effects in a deterministic and inspectable place, not across unrelated callbacks.

## Player Integration Rules
- Tether can influence player movement, but the coupling must remain readable.
- Tether should expose constraints or correction data, not reach deep into unrelated player systems.
- Do not let tether code directly own fuel, inventory, repair, UI, or station progression.
- If carrying scrap changes tether behavior, make that dependency explicit and testable.

## Networking Rules For Tether Code
- Default assumption unless the repository clearly says otherwise: host-authoritative multiplayer.
- Network gameplay meaning, not rope geometry.
- For any tether change, define all four:
  1. authority owner
  2. replicated durable state
  3. transient events such as break or snap feedback
  4. reconciliation or correction behavior
- If the project uses FishNet, keep tether replication focused on compact state and let each client render the rope locally.
- Do not send per-frame reliable messages for cosmetic rope updates.
- Remote clients may interpolate visuals, but interpolation must not change gameplay truth.

## Physics Stability Rules
- Avoid feedback loops where both connected players apply competing corrections every frame.
- Avoid unstable spring chains unless the project explicitly commits to them and accepts the cost.
- Prefer bounded and inspectable rules over emergent but fragile behavior.
- Guard against extreme values, NaN, runaway velocity, and correction explosions.
- When geometry or collision affects tether behavior, keep the rule deterministic enough to debug.

## Performance Rules
- Rope visuals are allowed to be approximate.
- Gameplay tether rules must remain cheap, predictable, and debuggable.
- Avoid per-segment allocations, excessive dynamic list growth, or repeated expensive physics queries in hot paths.
- Keep debug rendering and verbose diagnostics behind toggles.

## Testing Expectations
Before considering a tether change complete, validate at minimum:
- one player moves while the other stays idle
- both players move in opposite directions
- one player reaches the hard limit while the other keeps moving
- tether interaction while carrying scrap
- tether interaction under low fuel or interrupted thrust
- host view, owning player view, and remote player view consistency
- packet delay or jitter behavior in a controlled 2-player test when possible
- break or reconnect behavior if the feature supports it

## Review Checklist
- [ ] Rope visuals are not used as gameplay truth.
- [ ] Authority over tether state is explicit.
- [ ] The set of tether states is small and understandable.
- [ ] Tether rules are not hidden in VFX or camera code.
- [ ] No unstable correction loop was introduced.
- [ ] Network traffic stays focused on gameplay meaning.
- [ ] Player, fuel, and carry interactions were considered.

## When In Doubt
- Prefer simple, legible tether constraints over physically impressive but unstable rope simulation.
- Prefer local rope rendering plus compact authoritative state over syncing many rope segments.
- Prefer debugging the rule model first and polishing the visual model second.
