# Multiplayer Foundation Plan 02 — Session and Lobby Acceptance

Date: 2026-08-13

## Scope

This record covers only the Plan 02 transport-agnostic lobby, client session projection, MessagePack contracts, and Base.Network bridge boundary. It does not claim FishNet prediction, reconciliation, dedicated-world spawning, or runtime loopback acceptance; those belong to Plans 03 and 04.

## Acceptance status

The Unity acceptance gate has not been executed. The project is open in the user's Unity Editor and `Temp/UnityLockfile` is present. A second Unity process was not started and the user's editor was not terminated. Consequently no Unity Test Runner XML, test-discovery count, or authoritative pass/fail result exists for this plan.

Run the following from CI or after the interactive editor has released the project lock:

```powershell
& 'D:\Program Files\Unity\Hub\Editor\6000.3.12f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath . -runTests -testPlatform EditMode -assemblyNames Multiplayer.Primitives.Tests,Multiplayer.Session.Tests,Multiplayer.Session.Networking.Tests -testResults Temp/plan-02-editmode.xml -logFile Temp/plan-02-editmode.log
```

Retain `Temp/plan-02-editmode.xml` and `Temp/plan-02-editmode.log` as CI artifacts. A runtime loopback test using the final Base.Network bridge is also still required after the bridge's Unity compilation succeeds.

## Completed compilation evidence

The following already-generated projects compiled with `dotnet build <project> --no-restore --nologo --verbosity:minimal` on 2026-08-13:

| Project | Result |
| --- | --- |
| `Multiplayer.Primitives.csproj` | Success, 0 warnings, 0 errors |
| `Multiplayer.Session.csproj` | Success, 0 warnings, 0 errors |
| `Multiplayer.Session.Tests.csproj` | Success, 0 warnings, 0 errors |

The generated `Multiplayer.Session.Tests.csproj` predates the new architecture-boundary source and therefore does not include `ArchitectureBoundaryTests.cs`; it is not evidence that this new UnityEditor-based test executed. Unity must refresh script assemblies and run the EditMode command above.

No generated `Multiplayer.Session.Networking.csproj` or `Multiplayer.Session.Networking.Tests.csproj` exists yet, so the networking DTO/bridge assembly was not compiled by `dotnet` in this gate. Its compilation and tests remain part of the required Unity Test Runner pass.

## Declared test coverage

The current test source declares 101 NUnit cases: 14 primitive cases, 37 session cases (including the three architecture-boundary cases), and 50 session-networking cases. These are source-declared counts only, not Unity Test Runner discovery or execution counts.

The declarative coverage includes:

- Capacity is limited to 10 and configuration rejects unsupported capacity/minimum values.
- Two players are required; all connected initial lobby players must be ready before exactly one match-load request is committed.
- Repeated ready updates are idempotent and snapshots are immutable, ordered, and published by revision.
- Stale world-ready and spawn completions cannot mutate replacement operations.
- Join-in-progress players do not block the initial transition and follow the later readiness/spawn path.
- Initial-load disconnect, load timeout/failure, spawn failure, and JIP timeout follow the documented recovery policies.
- Client snapshots ignore older/equal revisions within a session and accept a lower revision for a replacement session.
- Fixed MessagePack IDs, schema keys, GUID/enumeration validation, duplicate-player rejection, and mapper round trips are covered.

## Assembly boundaries

`Assets/Tests/EditMode/MultiplayerSession/ArchitectureBoundaryTests.cs` uses Unity's compilation metadata to enforce these edges after Unity compiles the project:

| Assembly | Forbidden direct references |
| --- | --- |
| `Multiplayer.Primitives` | `FishNet.Runtime`, `Network`, `MessagePack` |
| `Multiplayer.Session` | `FishNet.Runtime`, `Network`, `MessagePack` |
| `Multiplayer.Session.Networking` | `FishNet.Runtime` |

`Multiplayer.Session.Networking` is intentionally allowed to reference `Network` and MessagePack; it is the transport/DTO adapter layer. The first two assemblies remain transport and serialization agnostic.

## Remaining acceptance work

- Run all three Plan 02 EditMode assemblies after Unity releases the lock and regenerate the IDE project files.
- Verify the new architecture-boundary test through the Unity Test Runner rather than the stale generated project.
- Compile and execute the Base.Network bridge tests once Task 6 is present in the active Unity script-assembly graph.
- Run a dedicated-server plus two external-client loopback check after Plan 03 provides the server/world adapters; verify a third JIP client does not delay match start and that disconnect/recovery telemetry is emitted.
