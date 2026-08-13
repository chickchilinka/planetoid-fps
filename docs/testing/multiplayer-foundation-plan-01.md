# Multiplayer Foundation Plan 01 — Offline Simulation Baseline

Date: 2026-08-13

## Environment

- Unity: `6000.3.12f1` (`ProjectSettings/ProjectVersion.txt`)
- FishNet: `4.6.12` (`Assets/FishNet/package.json` and `NetworkManager.FISHNET_VERSION`)
- Target covered by this record: Plan 01 offline gravity/character simulation foundation only

## Acceptance status

The Plan 01 acceptance gate is **not fully executed**. The project is open in the user's Unity Editor and `Temp/UnityLockfile` is present. A second Unity process was not started and the user's editor was not terminated. Therefore the complete EditMode and PlayMode suites were not run, and no Test Runner XML artifacts or authoritative pass/fail counts were produced.

The exact intended commands remain:

```powershell
& 'D:\Program Files\Unity\Hub\Editor\6000.3.12f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath . -runTests -testPlatform EditMode -testResults Temp/plan-01-editmode.xml -logFile Temp/plan-01-editmode.log
```

```powershell
& 'D:\Program Files\Unity\Hub\Editor\6000.3.12f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath . -runTests -testPlatform PlayMode -testResults Temp/plan-01-playmode.xml -logFile Temp/plan-01-playmode.log
```

Run both commands from a clean CI checkout or after the interactive editor releases the project lock. Retain `Temp/plan-01-editmode.xml` and `Temp/plan-01-playmode.xml` as CI artifacts.

## Checks completed

The following generated modular projects were compiled with `dotnet build <project> --no-restore --nologo --verbosity:minimal`:

| Project | Result |
| --- | --- |
| `SurfaceGravity.Core.csproj` | Success, 0 warnings, 0 errors |
| `SurfaceGravity.UnityRuntime.csproj` | Success, 0 warnings, 0 errors |
| `SurfaceGravity.Tests.csproj` | Success, 0 warnings, 0 errors |
| `Character.Simulation.csproj` | Success, 0 warnings, 0 errors |
| `Character.Simulation.Tests.csproj` | Success, 0 warnings, 0 errors |

The offline runtime and PlayMode test sources also compiled through the temporary verifier projects `Temp/Character.UnityRuntime.verify.csproj` and `Temp/Character.UnityRuntime.Tests.verify.csproj`, each with 0 warnings and 0 errors. These builds verify C# compilation only; they do not execute Unity lifecycle or physics tests.

The source inventory contains 60 declared EditMode NUnit cases and 6 declared PlayMode `UnityTest` cases for the new Plan 01 modules. These are source-declared counts, not Test Runner discovery/execution counts.

Static checks recorded during the gate:

- no FishNet reference was found in the four Plan 01 runtime module source/asmdef trees (`SurfaceGravity.Core`, `SurfaceGravity.UnityRuntime`, `Character.Simulation`, `Character.UnityRuntime`);
- the planet prefab YAML contains the three authored IDs `planet-box`, `planet-capsule`, and `planet-sphere`, each in its corresponding prefab;
- `Assets/Prefabs/Network/Network.prefab` serializes `_physicsMode: 0` (Unity physics timing);
- no legacy `SurfaceGravityService`, `RigidbodyMovementController`, `CharacterControllerInstaller`, `GravityBodyView`, or `GravityPlanetView` reference was found in the checked Playground/player/planet YAML;
- the latest completed domain reload in the active Unity `Editor.log` had no post-reload compiler-error match and reported `LogAssemblyErrors` with no errors.

`Assembly-CSharp.csproj` is stale after removal of the legacy movement/gravity files and still lists 20 deleted sources, so a direct build of that generated project fails with `CS2001`. The active Unity compilation log is the stronger current evidence; regenerate IDE project files before using `Assembly-CSharp.csproj` as a command-line gate.

## Manual Playground baseline

- Manual Play Mode duration: **0 minutes (not performed)**
- Average visible FPS: **not measured**
- Visible character/camera jitter: **not assessed**
- Reconciliation corrections: **not applicable to the offline Plan 01 adapter and not assessed**

No FPS, smoothness, or jitter claim is made by this record. A two-minute walk/jump pass around every planet, with console inspection for missing scripts and duplicate surface IDs, remains required before Plan 01 can be considered manually accepted.

## Residual risks and next-plan dependencies

- FishNet reconciliation must include `CharacterSimulationState.HeadingForward` together with gravity selection/smoothing, jump state, and view yaw. Omitting heading state would make replay orientation diverge after an authoritative correction.
- Every current and future gravity surface requires a unique, stable authored `SurfaceId`. The three current prefab IDs are present, but the duplicate-ID runtime guard and the full Unity test suite still need execution in a clean test run.
- The fresh `Character.UnityRuntime` and PlayMode assemblies need a full Unity asset refresh/Test Runner pass; command-line verifier compilation is not a substitute for Unity import, lifecycle, or physics execution.
- The manual Playground check must confirm exactly one movement/gravity loop, Unity physics timing, presentation-anchor camera following, and absence of missing scripts or visible periodic jitter.

Plan 02 may use this document as a compilation baseline, but should not interpret it as a completed Unity acceptance run.
