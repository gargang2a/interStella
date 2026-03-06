# AGENTS.md

This file defines repository-wide rules for AI agents working in this Unity project.
More specific `AGENTS.md` files in deeper folders override this file within their scope.

## Project Context
- Unity 2022 project.
- Steam-based co-op space game.
- Core loop:
  - travel between stations using suit fuel
  - remain connected to teammates with tether constraints
  - scavenge scrap
  - repair station objectives
  - move to the next target
- Art direction is casual, but gameplay clarity and responsiveness matter more than visual complexity.
- Team development model:
  - feature branches
  - individual implementation
  - review before integration
- Default multiplayer assumption unless the repository clearly shows another stack:
  - FishNet gameplay networking
  - Steam lobby and relay flow
  - host-authoritative sessions for MVP

## Repository Goal
Optimize for one stable vertical slice before expanding feature breadth.
This repository should favor correctness, authority clarity, and team integration safety over wide but fragile content.

## Current Development Priorities
1. Player movement feel
2. Tether gameplay correctness
3. Network authority consistency
4. One complete playable loop over many partial systems
5. Hot-path performance
6. Visual polish after gameplay stability

## MVP Vertical Slice Definition
The MVP is not complete when many disconnected systems exist.
The MVP is complete when this loop works end to end:
- join session
- spawn players
- move between spaces with readable control
- experience tether constraint as a cooperative rule
- consume and observe fuel
- scavenge scrap
- perform repair interaction
- complete or fail the station objective cleanly

Do not widen scope unless the task explicitly asks for it.

## Non-Negotiable Architecture Rules
- Keep MonoBehaviour classes thin.
- Do not create or extend giant god classes.
- Do not grow a giant `PlayerController` that owns movement, fuel, tether, interaction, animation, UI, and netcode in one place.
- Prefer feature-based organization over dumping code into generic `Managers`, `Helpers`, or `Utilities` folders.
- Prefer composition over inheritance.
- Keep gameplay rule code separate from presentation code whenever practical.
- Presentation is not gameplay truth.
- Tether visuals are not gameplay truth.
- Transform sync is not a substitute for explicit gameplay authority.
- Use ScriptableObject for static definitions and tunables, not mutable shared runtime session state.

## Repository Structure Expectations
Prefer feature folders similar to:
- `Assets/Game/Features/Player`
- `Assets/Game/Features/Tether`
- `Assets/Game/Features/Fuel`
- `Assets/Game/Features/Scavenge`
- `Assets/Game/Features/Repair`
- `Assets/Game/Features/Stations`
- `Assets/Game/Netcode`
- `Assets/Game/Shared`
- `Assets/Game/Scenes/Testbeds`
- `Assets/Game/Scenes/VerticalSlice`

Rules:
- New code should go in the most specific feature folder that owns the behavior.
- Avoid cross-feature leakage.
- Shared code belongs in `Shared` only if it is genuinely used by multiple features.
- Do not create catch-all folders for convenience.

## Folder-Specific Overrides
The following local AGENTS files are expected to become stricter than the root rules:
- `Assets/Game/Features/Player/AGENTS.md`
- `Assets/Game/Features/Tether/AGENTS.md`
- `Assets/Game/Netcode/AGENTS.md`

When changing files inside those folders, follow the deeper file first.

## Networking Rules
- Input is local.
- Final gameplay truth is host-authoritative unless the repository explicitly implements another model.
- Every networked feature must make these explicit:
  1. authority owner
  2. ownership model
  3. durable replicated state
  4. transient events or RPCs
  5. correction or reconciliation behavior
  6. late join or reconnect behavior if supported
- Do not network full rope geometry as gameplay truth.
- Do not solve desync by adding more transform sync without first defining gameplay meaning.
- Prefer compact gameplay-state replication over syncing presentation detail.
- If a system does not support late join, reconnect, or host migration in MVP, state that clearly instead of partially faking support.

## Gameplay System Rules
### Player
- Preserve movement feel, but keep simulation and presentation separate.
- Separate input capture, simulation, authority correction, and feedback.

### Tether
- Tether is a gameplay constraint first and a visual effect second.
- Network tether meaning such as connection, length limit, tension, break, and forced constraint state.
- Do not treat rope rendering as the source of truth.

### Fuel
- Fuel is authoritative gameplay state.
- Local UI or feel may predict it only if correction is expected and supported.

### Scavenge and Carry
- Item pickup, drop, carry, and ownership must remain authority-safe.
- Prefer data-driven item definitions over hardcoded item behavior where practical.

### Repair and Stations
- Repair logic should be modular and extensible.
- Avoid station-wide god scripts.
- Keep objective state readable and testable.

## Unity Editing Rules
- Make the smallest change that fully solves the task.
- Preserve prefab, scene, and serialized references.
- Do not rename, move, or split files unless the task requires it.
- Do not silently change serialized field names without noting migration risk.
- Avoid editing shared integration scenes unless the task explicitly requires integration.
- Prefer isolated test scenes for feature work before touching the shared playable scene.
- Do not introduce new packages, render pipeline changes, or project-wide framework shifts unless explicitly requested.

## Hot-Path Performance Rules
Treat the following as risk areas:
- `Update`, `LateUpdate`, and `FixedUpdate`
- movement and tether solvers
- repeated physics queries
- UI refresh loops
- per-frame allocation in gameplay code
- frequent spawn and despawn paths

Rules:
- Avoid LINQ, closures, boxing, and string formatting in hot paths.
- Avoid repeated `GetComponent`, `GameObject.Find`, and `FindObjectOfType` in gameplay loops.
- Cache references and layer masks when practical.
- Keep debug logging out of hot loops unless explicitly gated.

## Team Workflow Rules
- Prefer feature branches.
- Prefer isolated feature scenes or testbeds before integration.
- Minimize edits to shared scenes and shared prefabs.
- Make ownership boundaries readable so multiple developers can work in parallel.
- When a task crosses Player, Tether, and Netcode together, preserve feature boundaries instead of solving it in one convenience script.

## AI Change Policy
- Do not widen the task beyond what is needed.
- Do not perform opportunistic refactors across unrelated systems.
- Do not silently rewrite architecture because a cleaner pattern exists.
- Do not invent commands or workflows that are not already configured in the repository.
- When repository context is incomplete, proceed with the smallest reasonable assumption and state it in the final summary.
- Unsupported behavior should be stated explicitly, not hidden.

## Testing and Validation
Only run commands that already exist in the repository or are listed here.
If exact commands are not configured yet, do not invent them. State what must be validated manually.

If `UNITY_EDITOR_PATH` is configured, preferred headless test commands are:
- EditMode:
  - `"$UNITY_EDITOR_PATH" -batchmode -nographics -quit -projectPath . -runTests -testPlatform EditMode -testResults ./Logs/editmode-results.xml`
- PlayMode:
  - `"$UNITY_EDITOR_PATH" -batchmode -nographics -quit -projectPath . -runTests -testPlatform PlayMode -testResults ./Logs/playmode-results.xml`

Manual validation expectations by change type:
- movement changes: local feel, host/client consistency, tether interaction
- tether changes: idle vs moving, opposite movement, hard limit, carry interaction
- netcode changes: host, owner, remote observer, spawn and despawn, correction behavior
- repair or station changes: ownership safety, state consistency, reset behavior

Do not run a full player build unless the task explicitly asks for it.

## Code Style
- Private fields: `_camelCase`
- Local variables and parameters: `camelCase`
- Methods, classes, structs, enums, and properties: `PascalCase`
- Constants: `UPPER_SNAKE_CASE`
- Prefer clear names over abbreviations.
- Comments should explain why, not restate obvious code.

## Review Checklist
- [ ] The change supports the current vertical slice priorities.
- [ ] Authority and ownership are explicit where needed.
- [ ] No hidden scene dependency was introduced.
- [ ] Presentation was not used as gameplay truth.
- [ ] No avoidable per-frame allocation was introduced in hot paths.
- [ ] Prefab and serialized references remain safe.
- [ ] Host, owner, and remote behavior were considered where relevant.
- [ ] Shared scene edits were avoided unless necessary.

## Anti-Patterns To Reject
- giant `PlayerController`
- rope visuals driving gameplay truth
- transform sync used as the main gameplay solution
- per-frame reliable RPC spam
- hidden authority changes inside feature scripts
- shared mutable ScriptableObject session state
- generic manager classes absorbing unrelated features
- widening scope instead of finishing the loop

## When In Doubt
- Prefer explicit authority over clever synchronization.
- Prefer a stable vertical slice over broader incomplete systems.
- Prefer readable feature boundaries over convenience coupling.
- Prefer small safe changes over sweeping architecture edits.
