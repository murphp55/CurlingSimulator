# CurlingSimulator — Agent Notes

## One-liner
3D arcade curling simulator in Unity 6 (6000.3.5f1) with URP, C#, custom kinematic physics (no PhysX).

## Run it
```
# Open in Unity Hub: Add → select repo folder. Unity 6000.3.5f1 required (pinned in ProjectSettings/ProjectVersion.txt).
# Then open Assets/Scenes/MainGame.unity and press Play.

# Batch / headless build of scenes & prefabs:
"C:/Program Files/Unity/Hub/Editor/6000.3.5f1/Editor/Unity.exe" -batchmode -quit -nographics ^
  -projectPath "C:/Users/murph/source/repos/CurlingSimulator" ^
  -executeMethod SceneBuilder.BuildScene -logFile batch.log
```

## Where we are right now
- Last touched: 2026-03-24
- Working on: Project is feature-complete (all 8 stages); last commits added URP/TMP packages and fixed null StoneSimConfig refs in MainGame.
- Known broken: Nothing known. Untracked `README.md` exists in working tree (newly added, not committed).

*This section goes stale fast. Check `git log -5` and `git status` before trusting it.*

## Gotchas
- `CurlingSimulator.Camera` is a **namespace**, which collides with the `UnityEngine.Camera` type. Never `using CurlingSimulator.Camera;` — always fully qualify both (`UnityEngine.Camera` and `CurlingSimulator.Camera.CameraDirector`).
- `StoneSimConfig.BaseDecelerationRate` is applied **per FixedUpdate frame**, not per second. Any distance prediction must use `Time.fixedDeltaTime` (see `AimIndicator.cs` for the correct `d = v²/(2a)` formula).
- Stones are pooled by `StoneSimulator` at scene load — never `Destroy` or `SetActive(false)` stone GameObjects directly; go through the `StoneSimulator` API.
- `MatchConfig.PendingConfig` is a static field used to hand config from MainMenu → MainGame scene. There is no `DontDestroyOnLoad` manager; don't add one.
- Inspector wiring matters: `StoneSimConfig.asset` must be assigned on all components that need it. `SceneBuilder` wires it, but a recent commit (`0ea5f6d`) fixed null refs after re-builds — verify in Play mode if you rebuild scenes.
- Audio clips are NOT assigned in `AudioManager` — the system is wired but every clip slot is empty in the Inspector. Don't assume sound works.

## Non-obvious conventions
- All input goes through `IInputProvider`. `GameManager` must never call `UnityEngine.Input` directly — that interface is the multiplayer seam.
- `GamePhase` enum drives everything via `GameManager.OnPhaseChanged`. Systems subscribe; they never set the phase themselves.
- `[SerializeField] private` + property/method only — no `public` fields anywhere.
- No `Find` / `FindObjectOfType` / `GetComponentInParent` in hot paths. Wire in Inspector or subscribe in `Start()`.
- Editor-only tools live under `Assets/Scripts/Editor/` (`SceneBuilder`, `ArtSetupWizard`) and are accessible via the **CurlingSimulator** Unity menu.

See README.md for project description, tech stack, controls, file map, and feature list.
