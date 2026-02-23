# CurlingSimulator — Agent Reference

Arcade-style 3D curling simulator built in **Unity 6 (6000.3.5f1)** with **URP**.
PC-only (mouse + keyboard). 10 ends, standard curling rules.

---

## Quick Facts

| Item | Value |
|---|---|
| Engine | Unity 6000.3.5f1 |
| Render pipeline | URP 17.2.0 |
| Target platform | Windows PC (mouse + keyboard) |
| Key packages | Cinemachine 3.1.3, TextMeshPro 5.0.0 |
| Working directory | `c:\Users\murph\source\repos\CurlingSimulator` |
| GitHub | https://github.com/murphp55/CurlingSimulator |
| All scripts | `Assets/Scripts/` (organized by namespace folder) |

---

## Project Status

All eight development stages are complete and committed. The project can be
opened in Unity Hub and played immediately.

| Stage | Content | Status |
|---|---|---|
| 1 | Physics core (StoneSimConfig, StoneController, CollisionResolver, StoneSimulator) | ✅ Done |
| 2 | ScoringSystem | ✅ Done |
| 3 | GameManager + PlayerInputProvider + InputRouter | ✅ Done |
| 4 | Sweep mechanics (integrated in PlayerInputProvider) | ✅ Done |
| 5 | Cinemachine CameraDirector | ✅ Done |
| 6 | AI system (Easy / Medium / Hard) | ✅ Done |
| 7 | UI (HUDController, Scoreboard, MainMenuController, AudioManager) | ✅ Done |
| 8 | URP art pass (Visuals, AudioBridge, ArtSetupWizard, SceneBuilder) | ✅ Done |

**What is still needed (manual Unity work only — no C# to write):**

- Assign actual audio clips to `AudioManager` in the Inspector (throw release,
  collision, stone-at-rest, end-score, sweep loop).
- Assign `StoneSimConfig.asset` to all components that expect it in the Inspector
  (should already be wired by SceneBuilder, verify in Play mode).
- Tune `StoneSimConfig` physics values in the Inspector to taste (drag sliders
  and observe behaviour in Play mode).
- Apply a URP render pipeline asset and post-process volume (bloom, colour grading)
  for the final arcade look.
- Replace placeholder cylinder meshes on stone prefabs with real granite meshes
  if desired.

---

## Opening the Project

```
1. Clone https://github.com/murphp55/CurlingSimulator
2. Open Unity Hub → Add → browse to the cloned folder
3. Unity Hub will detect Unity 6000.3.5f1 from ProjectSettings/ProjectVersion.txt
4. First import resolves URP, Cinemachine, TMP (~2-3 min)
5. Open Assets/Scenes/MainGame.unity → press Play
```

---

## Architecture Overview

```
PlayerInputProvider ──┐
                       ├──► IInputProvider ──► GameManager (state machine)
AIInputProvider    ────┘                            │
                                                    ▼
                                           StoneSimulator
                                           (launches, sweeps, collides)
                                                    │
                                    ┌───────────────┤
                                    ▼               ▼
                            StoneController   CollisionResolver
                            (per-stone        (analytic elastic
                             kinematic loop)   collision, static)
                                                    │
                                                    ▼
                                            ScoringSystem (static)
                                                    │
                                                    ▼
                                            GameManager fires
                                            OnEndScored / OnMatchOver
```

### Core Contracts

- **`IInputProvider`** — THE multiplayer seam. `GameManager` never calls
  `UnityEngine.Input` directly. Never add direct input polling outside this
  interface.
- **`ThrowData` / `SweepData`** — `[Serializable]` structs passed through
  `IInputProvider`. Keep them serializable (no MonoBehaviour refs inside them).
- **`StoneSimConfig`** — single `ScriptableObject` for all physics constants.
  Always read from here, never hardcode magic numbers in game code.
- **`GamePhase`** (enum) drives everything. All systems listen to
  `GameManager.OnPhaseChanged`; they never set the phase themselves.

---

## Namespace Map

| Folder under Assets/Scripts/ | Namespace |
|---|---|
| Core/ | CurlingSimulator.Core |
| Input/ | CurlingSimulator.Input |
| Simulation/ | CurlingSimulator.Simulation |
| AI/ | CurlingSimulator.AI |
| Camera/ | CurlingSimulator.Camera |
| UI/ | CurlingSimulator.UI |
| Audio/ | CurlingSimulator.Audio |
| Network/ | CurlingSimulator.Network |
| Visuals/ | CurlingSimulator.Visuals |

> **Watch out**: `CurlingSimulator.Camera` is a namespace — it conflicts with
> `UnityEngine.Camera` (the type). Always use the fully qualified type name
> `UnityEngine.Camera` in any file that also references `CameraDirector`, and
> use `CurlingSimulator.Camera.CameraDirector` in `AddComponent` /
> `GetComponent` calls instead of a `using` directive.

---

## File Reference

### Core/

| File | Class | Purpose |
|---|---|---|
| GameManager.cs | `GameManager` | Singleton state machine. Drives the entire match loop. Wires IInputProvider instances and fires all game events. |
| GamePhase.cs | `GamePhase` (enum) | `MatchSetup, ThrowAim, ThrowRelease, StoneInMotion, EndThrowEvaluation, EndScoring, EndTransition, MatchOver` |
| InputRouter.cs | `InputRouter` | Scene-start wiring: creates PlayerInputProvider or AIInputProvider based on MatchConfig and hands them to GameManager. |
| MatchConfig.cs | `MatchConfig`, `AIDifficulty` | Serializable match settings (10 ends, player team, hammer, difficulty). Passed from MainMenu to MainGame via static `PendingConfig`. |
| MatchState.cs | `MatchState` | Live match state: current end, throw index, scores `[0]=Red [1]=Yellow`, stone list. |
| StoneState.cs | `StoneState` (struct) | Per-stone snapshot: position, velocity, angular progress, owner, index, `IsMoving`, `IsInPlay`. |
| EndScoreResult.cs | `EndScoreResult` | Result of scoring an end: scoring team, points, final stone positions. |
| TeamId.cs | `TeamId` (enum) | `None, Red, Yellow` |
| CurlDirection.cs | `CurlDirection` (enum) | `InTurn` (clockwise), `OutTurn` (counter-clockwise) |

### Input/

| File | Class | Purpose |
|---|---|---|
| IInputProvider.cs | `IInputProvider` | Interface: fires `OnThrowCommitted(ThrowData)` and `OnSweepUpdate(SweepData)`. All input goes through this. |
| PlayerInputProvider.cs | `PlayerInputProvider` | Mouse input. Push-forward Y → power, horizontal drag → aim angle, release LMB → throw, rapid left-right → sweep, Tab → curl toggle. Fires `OnAimUpdated` for AimIndicator. |
| ThrowData.cs | `ThrowData` (struct) | Serializable: `float Power, float AimAngleDeg, CurlDirection Curl, TeamId Thrower, int StoneIndex, double Timestamp` |
| SweepData.cs | `SweepData` (struct) | Per-frame: `float Intensity` (0–1), `float DeltaTime`, `double Timestamp` |

### Simulation/

| File | Class | Purpose |
|---|---|---|
| StoneSimConfig.cs | `StoneSimConfig` : `ScriptableObject` | All physics constants. Lives at `Assets/Settings/StoneSimConfig.asset`. |
| StoneController.cs | `StoneController` | Per-stone kinematic physics: deceleration, curl force, boundary check, sweep modification. `FixedUpdate` owns the integration. Exposes `public float SweepIntensity` for SweepFX. |
| StoneSimulator.cs | `StoneSimulator` | Pools 8 stones per team. Handles launch, per-frame sweep relay, broad-phase collision checks, stop detection. Fires `OnStoneCollision` and `OnAllStonesStopped`. |
| CollisionResolver.cs | `CollisionResolver` (static) | Analytic elastic collision for two equal-mass spheres. No PhysX — fully deterministic. |
| ScoringSystem.cs | `ScoringSystem` (static) | Given a list of `StoneState`, returns the scoring team and point count for an end. |

### AI/

| File | Class | Purpose |
|---|---|---|
| IAIStrategy.cs | `IAIStrategy` | `ThrowData CalculateThrow(SheetState)` + `float CalculateSweep(StoneState, SheetState)` |
| BaseAIStrategy.cs | `BaseAIStrategy` (abstract) | Applies Gaussian noise scaled by `AccuracyBias` (0 = perfect, 1 = very noisy) to ideal throw parameters. |
| EasyAIStrategy.cs | `EasyAIStrategy` | `AccuracyBias = 0.85`, always draws, never sweeps. |
| MediumAIStrategy.cs | `MediumAIStrategy` | `AccuracyBias = 0.40`, uses AIDirector for intent, sweeps at 0.50. |
| HardAIStrategy.cs | `HardAIStrategy` | `AccuracyBias = 0.10`, full intent selection, sweeps at 1.00. |
| AIInputProvider.cs | `AIInputProvider` | Wraps an `IAIStrategy`. Delays throw by 0.5–1.5 s (think time), then fires `OnThrowCommitted`. |
| AIDirector.cs | `AIDirector` | Chooses `ThrowIntent` (`Draw, Guard, Takeout, Peel, Freeze`) based on `SheetState` for Medium/Hard AI. |
| SheetState.cs | `SheetState` (struct) | Read-only snapshot: `List<StoneState> Stones`, `int StonesRemainingThisEnd`, `TeamId AITeam/OpponentTeam`, scores, hammer flag, house geometry. |
| ThrowIntent.cs | `ThrowIntent` (enum) | `Draw, Guard, Takeout, Peel, Freeze` |

### Camera/

| File | Class | Purpose |
|---|---|---|
| CameraDirector.cs | `CuralingSimulator.Camera.CameraDirector` | Subscribes to `GameManager.OnPhaseChanged`. Switches Cinemachine vcam priorities: hack cam (ThrowAim), sweeper cam (StoneInMotion), overhead (scoring/menu). |

### UI/

| File | Class | Purpose |
|---|---|---|
| HUDController.cs | `HUDController` | Power bar, aim arrow, sweep intensity slider (all hidden outside relevant phases). End scoring overlay. Game-over screen. |
| Scoreboard.cs | `Scoreboard` | 10-column grid; fills per-end cells after each `OnEndScored`, shows running totals. |
| MainMenuController.cs | `MainMenuController` | Difficulty dropdown, team picker, Play button. Writes `MatchConfig` to `MatchConfig.PendingConfig`, loads MainGame scene. |

### Audio/

| File | Class | Purpose |
|---|---|---|
| AudioManager.cs | `AudioManager` | Singleton. Three `AudioSource`s: SFX (one-shot), sweep loop (looping), music. Methods: `PlayThrowRelease`, `PlayStoneCollision`, `PlayStoneComeToRest`, `PlayEndScore`, `PlaySweeping`, `StopSweeping`. |
| AudioBridge.cs | `AudioBridge` | MonoBehaviour wiring only: subscribes to `StoneSimulator`, `PlayerInputProvider`, and `GameManager` events and calls the matching `AudioManager` method. |

### Network/ (stubs — not yet implemented)

| File | Class | Purpose |
|---|---|---|
| INetworkInputProvider.cs | `INetworkInputProvider` | Extends `IInputProvider`; intended entry point for a network player. |
| INetworkTransport.cs | `INetworkTransport` | Abstraction over network library (Unity Netcode, Mirror, etc.). |
| MatchStateSnapshot.cs | `MatchStateSnapshot` (struct) | Serializable full snapshot for late-join, resync, and replay. |

### Visuals/

| File | Class | Purpose |
|---|---|---|
| StoneVisuals.cs | `StoneVisuals` | `[RequireComponent(StoneController)]`. Applies team material instance, spins mesh from `StoneState.AngularProgress`, toggles `TrailRenderer.emitting`, boosts emission on `SetHighlight(true)`. |
| AimIndicator.cs | `AimIndicator` | Listens to `PlayerInputProvider.OnAimUpdated`. Draws a `LineRenderer` prediction line from the active hack using kinematic distance formula `d = v²/(2a)`. Hidden outside `ThrowAim` phase. |
| SheetRenderer.cs | `SheetRenderer` | Builds ice-plane (scaled Unity `Plane` primitive) and all house rings + lines from `StoneSimConfig` values at `Start()`. |
| ImpactFX.cs | `ImpactFX` | Subscribes to `StoneSimulator.OnStoneCollision`. Instantiates `ImpactBurst` prefab at the midpoint between colliding stones, auto-destroys after particle lifetime. |
| SweepFX.cs | `SweepFX` | `[RequireComponent(StoneController)]`. Each `Update`, reads `StoneController.SweepIntensity` and sets `ParticleSystem` emission rate. |

### Editor/ (editor-only; stripped from builds)

| File | Class | Purpose |
|---|---|---|
| SceneBuilder.cs | `SceneBuilder` | Menu: **CurlingSimulator → Build Game Scene**. Creates `StoneSimConfig.asset`, stone prefabs, `ImpactBurst` prefab, `MainGame.unity`, `MainMenu.unity`, wires all Inspector refs, sets build settings. Also callable via `Unity.exe -batchmode -executeMethod SceneBuilder.BuildScene`. |
| ArtSetupWizard.cs | `ArtSetupWizard` | Menu: **CurlingSimulator → Setup Art Materials**. Creates 12 URP materials in `Assets/Materials/`, prints full manual setup checklist to Console. |

---

## Key Assets

| Path | Type | Notes |
|---|---|---|
| `Assets/Settings/StoneSimConfig.asset` | ScriptableObject | All physics constants — edit this to tune gameplay |
| `Assets/Scenes/MainGame.unity` | Scene | Main playable scene |
| `Assets/Scenes/MainMenu.unity` | Scene | Difficulty/team selection |
| `Assets/Prefabs/RedStone.prefab` | Prefab | 8 pooled per match (Red team) |
| `Assets/Prefabs/YellowStone.prefab` | Prefab | 8 pooled per match (Yellow team) |
| `Assets/Prefabs/ImpactBurst.prefab` | Prefab | Collision particle burst |
| `Assets/Materials/` | 12 `.mat` files | URP Lit/Unlit; see art guide below |
| `Assets/Settings/PostProcessProfile.asset` | URP PP | Bloom + colour adjustments for arcade look |

---

## Physics Model

Kinematic `Rigidbody` — **PhysX is not used for stone movement**.
`StoneController.FixedUpdate()` owns the entire integration loop:

```
velocity -= deceleration * dt            // base friction (from StoneSimConfig)
velocity -= curlForce * lateralDir * dt  // curl (direction from CurlDirection)
velocity += sweepBoost * forward * dt    // sweep modification
position = Rigidbody.MovePosition(pos + velocity * dt)
```

Collisions are resolved analytically by `CollisionResolver.Resolve(a, b)` —
deterministic, tick-identical on every run.

`StoneSimConfig` fields to know:
- `MinLaunchSpeed` / `MaxLaunchSpeed` — power 0–1 mapped to these
- `BaseDecelerationRate` — subtracted **per FixedUpdate frame** (not per second)
- `CurlForce` — lateral force magnitude
- `SweepDecelerationReduction` — how much sweep counters friction
- `StoneRadius` — used for collision detection
- `SheetHalfWidth`, `BackLineDistance`, `HogLineDistance` — boundary geometry

---

## Input Design

| Action | Input | Notes |
|---|---|---|
| Aim | Push mouse forward (+Y) while holding LMB | Y movement = power |
| Direction | Drag mouse horizontally | Maps to `aimAngleDeg` |
| Throw | Release LMB | Fires `OnThrowCommitted` |
| Curl toggle | Tab key | Switches `InTurn` / `OutTurn` |
| Sweep | Rapid left-right mouse scrubbing | Counts reversals; 12 rev/s = 100% intensity |

---

## Events (primary wiring points)

```csharp
// GameManager
GameManager.Instance.OnPhaseChanged   += (GamePhase phase) => { };
GameManager.Instance.OnEndScored      += (EndScoreResult r) => { };
GameManager.Instance.OnMatchOver      += (MatchState final) => { };

// StoneSimulator
_simulator.OnStoneCollision           += (StoneController a, StoneController b) => { };
_simulator.OnAllStonesStopped         += () => { };

// PlayerInputProvider
_playerInput.OnThrowCommitted         += (ThrowData d) => { };
_playerInput.OnSweepUpdate            += (SweepData d) => { };
_playerInput.OnAimUpdated             += (float power, float angleDeg, CurlDirection curl) => { };
```

---

## Materials Reference

| Material | Shader | Usage |
|---|---|---|
| `Ice_Mat` | URP/Lit | Ice plane (light blue, high smoothness) |
| `Stone_Red_Mat` | URP/Lit | Red team stones (emission glow) |
| `Stone_Yellow_Mat` | URP/Lit | Yellow team stones (emission glow) |
| `Ring12_Mat` | URP/Unlit | 12-ft house ring (red) |
| `Ring8_Mat` | URP/Unlit | 8-ft house ring (white) |
| `Ring4_Mat` | URP/Unlit | 4-ft house ring (light blue) |
| `Button_Mat` | URP/Unlit | Button centre (white) |
| `Line_Mat` | URP/Unlit | All sheet lines (light grey) |
| `AimLine_Red_Mat` | URP/Unlit (transparent) | Red team aim indicator line |
| `AimLine_Yellow_Mat` | URP/Unlit (transparent) | Yellow team aim indicator line |
| `Particle_IceBurst_Mat` | URP/Unlit (additive) | Impact burst particles (cyan) |
| `Particle_SweepChip_Mat` | URP/Unlit (additive) | Sweep particles (cyan) |

---

## Coding Conventions

- `private` fields use `_camelCase` prefix.
- No `public` fields — always `[SerializeField] private` + property or method.
- Events follow the pattern `public event Action<T> OnEventName;`.
- All game-state structs passed through events are `[Serializable]`.
- No `Find`, `FindObjectOfType`, or `GetComponentInParent` in hot paths —
  all references are wired in the Inspector or via `Start()` subscription.
- `GameManager`, `AudioManager` are singletons accessed via `.Instance`.
- No `using CurlingSimulator.Camera;` — use fully qualified names for that namespace.

---

## Common Gotchas

1. **`CurlingSimulator.Camera` vs `UnityEngine.Camera`** — always write
   `UnityEngine.Camera` and `CurlingSimulator.Camera.CameraDirector` explicitly.
2. **`SheetState` needs `using UnityEngine;`** (for `Vector2`).
3. **`IAIStrategy` needs `using CurlingSimulator.Core;`** (for `StoneState`).
4. **`BaseDecelerationRate` is per-frame**, not per-second. Account for
   `Time.fixedDeltaTime` when doing any kinematic distance prediction math
   (see `AimIndicator.cs` for the correct formula).
5. **`StoneSimulator` pools stones at scene load** — do not destroy or deactivate
   stone GameObjects directly; call `StoneSimulator` API instead.
6. **`MatchConfig.PendingConfig`** is a static field used to pass config from
   the menu scene to the game scene (no DontDestroyOnLoad manager needed).

---

## Running in Batch Mode

```bash
# Step 1 — init project / resolve packages
"C:/Program Files/Unity/Hub/Editor/6000.3.5f1/Editor/Unity.exe" \
  -batchmode -quit -nographics \
  -projectPath "C:/Users/murph/source/repos/CurlingSimulator" \
  -logFile "batch_step1.log"

# Step 2 — build scenes/prefabs/assets via SceneBuilder
"C:/Program Files/Unity/Hub/Editor/6000.3.5f1/Editor/Unity.exe" \
  -batchmode -quit -nographics \
  -projectPath "C:/Users/murph/source/repos/CurlingSimulator" \
  -executeMethod SceneBuilder.BuildScene \
  -logFile "batch_step2.log"
```

---

## What Is NOT Done

- Real audio clips (need to be sourced and assigned in Inspector).
- Real granite stone meshes (cylinders used as placeholders).
- URP render pipeline asset properly assigned in Graphics Settings.
- Multiplayer (Network/ folder is stubs only).
- Mobile / gamepad support (PC mouse + keyboard only by design).
- Build pipeline / CI.
