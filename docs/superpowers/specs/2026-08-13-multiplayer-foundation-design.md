# Multiplayer FPS Foundation Design

**Date:** 2026-08-13
**Status:** Approved design
**Target:** first playable multiplayer slice for a dedicated server

## 1. Purpose

Build the multiplayer foundation for an FPS with server-authoritative physics, local client prediction, and reconciliation. The first playable slice covers:

- connection to a dedicated server by IP and port;
- a lobby for up to 10 players;
- ready state and match start when every connected lobby player is ready, with a minimum of 2 players;
- server-controlled match-scene loading;
- server-authoritative player spawning;
- join in progress;
- predicted surface-gravity character movement;
- reconciliation against the server state;
- physical collisions between players and with networked dynamic rigidbodies;
- clean disconnect and despawn handling;
- reuse of the same character simulation in the offline `Playground` scene.

The dedicated server is authoritative over physics and final game state. A client sends input commands and predicts only its locally owned player. It never submits an authoritative position, velocity, grounded state, or gravity result.

## 2. Scope boundaries

### In scope

- one dedicated server process hosting one match;
- direct IP/port connection;
- a single global match scene and physics scene;
- maximum 10 players;
- ready-based match start;
- join in progress after the match has started;
- static surface-gravity sources authored in the match scene;
- movement, look yaw, jumping, collisions, and dynamic-body interaction;
- FishNet prediction and reconciliation for physical state;
- `Base.Network` messaging for session commands and events;
- headless server composition;
- automated unit, contract, PlayMode, and multi-process tests.

### Out of scope for this slice

- matchmaking, server browser, account service, parties, and authentication;
- host/listen-server mode;
- multiple concurrent matches in one process;
- additive per-match physics scenes;
- moving planets or other moving gravity sources;
- shooting, damage, reload, grenades, inventory, and respawning rules;
- production anti-cheat beyond authoritative simulation and input validation;
- final UI and visual polish.

Shooting, reloads, and grenades will later use their own gameplay modules and `Base.Network` adapters. This design provides extension seams but does not implement those mechanics prematurely.

## 3. Architectural principles

The implementation follows these rules:

1. Each application/gameplay module exposes one public facade or service as its entry point.
2. A module accesses another module only through a provider interface declared by the consuming module.
3. Composition roots and integration adapters connect those consumer-owned ports to provider implementations.
4. Core modules contain no FishNet transport types such as `NetworkConnection`, `NetworkObject`, RPC attributes, or FishNet tick data.
5. Wire DTOs do not leak into domain/application APIs.
6. Unity and FishNet behaviours adapt engine callbacks into services; they do not own business rules.
7. Expected runtime failures are typed results. Exceptions indicate programming or configuration errors.
8. Commands that can be repeated by the network or lifecycle are idempotent.
9. Async lifecycle work is protected from stale completion callbacks.
10. Interfaces are introduced at module boundaries and variation points, not for every internal class.

The architecture is hybrid:

- `Base.Network` carries lobby/session messages and later discrete gameplay commands and events;
- FishNet owns network-object lifecycle, scene observation, physics prediction, replay, and reconciliation;
- transport-agnostic application modules own session, spawning, gravity, and character-simulation policy.

The existing FishNet-backed `Base.Network` adapter may reuse the same FishNet connection. No second gameplay socket or competing transport lifecycle is introduced.

## 4. Module map

Every module below receives its own assembly definition unless it is already an isolated assembly. Dependencies point inward toward contracts and pure logic.

### 4.1 `Multiplayer.Primitives`

A tiny shared kernel for stable value types only:

- `PlayerId`;
- `SessionId`;
- `MatchId`;
- `MapId`;
- `OperationId`.

It contains no services, behavior, Unity objects, FishNet objects, or transport identifiers.

### 4.2 `Base.Network`

The existing general client/server messaging facility remains responsible for:

- connect and disconnect notifications;
- reliable and unreliable delivery abstractions;
- request/response and one-way messages;
- serialization and routing;
- client and server message context;
- generic FishNet transport adapters.

It is not responsible for lobby rules, match state, object spawning, scene loading, physics, prediction, or reconciliation.

### 4.3 `Multiplayer.Session`

Owns the authoritative session state machine and the client-side session projection.

Public entry points:

```csharp
public interface IServerSessionFacade
{
    ValueTask<Result<PlayerId, SessionError>> JoinAsync(
        SessionConnection connection,
        CancellationToken cancellationToken);

    ValueTask<Result<Unit, SessionError>> LeaveAsync(
        PlayerId playerId,
        CancellationToken cancellationToken);

    ValueTask<Result<Unit, SessionError>> SetReadyAsync(
        PlayerId playerId,
        bool ready,
        CancellationToken cancellationToken);

    ValueTask<Result<Unit, SessionError>> NotifyServerWorldReadyAsync(
        OperationId operationId,
        MatchId matchId,
        CancellationToken cancellationToken);

    ValueTask<Result<Unit, SessionError>> NotifyPlayerWorldReadyAsync(
        OperationId operationId,
        PlayerId playerId,
        MatchId matchId,
        CancellationToken cancellationToken);

    SessionSnapshot Snapshot { get; }
}

public interface IClientSessionFacade
{
    SessionSnapshot Snapshot { get; }
    event Action<SessionSnapshot> SnapshotChanged;

    ValueTask<Result<Unit, SessionError>> SetReadyAsync(
        bool ready,
        CancellationToken cancellationToken);

    void ApplySnapshot(SessionSnapshot snapshot);
}
```

Consumer-owned provider ports:

- `IMatchWorldProvider` starts/cancels a match-world load and reports readiness;
- `IPlayerSpawnProvider` spawns and despawns player entities;
- `ISessionEventPublisher` publishes snapshots, acceptances, and rejection reasons;
- `ISessionConnectionProvider` disconnects an invalid or timed-out connection.

All authoritative commands run through one serialized command queue. This avoids locks and prevents two simultaneous ready/disconnect/load callbacks from producing different state transitions.

`SessionConnection` is an opaque, transport-neutral connection token minted by the networking adapter. It may be used for correlation through `ISessionConnectionProvider`, but it never exposes a FishNet connection or permits session rules to call the transport directly.

### 4.4 `Multiplayer.Spawning`

Owns spawn orchestration without knowing the active network transport.

Public entry point:

```csharp
public interface IPlayerSpawnService
{
    ValueTask<Result<SpawnHandle, SpawnError>> SpawnAsync(
        PlayerSpawnRequest request,
        CancellationToken cancellationToken);

    ValueTask<Result<DespawnOutcome, SpawnError>> DespawnAsync(
        PlayerId playerId,
        CancellationToken cancellationToken);
}
```

Consumer-owned provider ports:

- `ISpawnPointProvider` supplies and reserves suitable spawn poses;
- `IPlayerEntityRuntimeProvider` creates and destroys a runtime player entity;
- `ISpawnSafetyProvider` is an optional later extension for occupancy checks.

The service owns an idempotent `PlayerId -> SpawnHandle` registry. A duplicate spawn request returns the existing handle. Despawning a missing player succeeds with `AlreadyAbsent`.

`SpawnHandle` is a neutral application value. It does not expose a FishNet `NetworkObject`.

### 4.5 `SurfaceGravity`

Owns calculation of character gravity against static scene-authored sources.

Public entry point:

```csharp
public interface ISurfaceGravitySolver
{
    GravityStepResult Solve(in GravityStepInput input);
}
```

Consumer-owned provider port:

- `IGravitySurfaceProvider` supplies immutable gravity-surface data for the current match.

The first slice supports static gravity sources only:

- their transforms and geometry do not change during the match;
- they require no runtime network synchronization;
- server and clients load the same authored configuration;
- every source has a stable authored `SurfaceId`, never a Unity instance ID;
- the same query and smoothing rules run during prediction, authoritative simulation, and replay.

The solver is stepped explicitly by character simulation. It is not a global `IFixedTickable` that mutates every registered rigidbody. This keeps its time source aligned with both FishNet ticks and offline fixed updates.

Moving planets are intentionally not implemented. The provider boundary leaves a future migration path to tick-indexed source poses without coupling current character simulation to FishNet.

### 4.6 `Character.Simulation`

Owns the transport-agnostic movement algorithm used by offline play, client prediction, server authority, and reconciliation replay.

Public entry point:

```csharp
public interface ICharacterSimulationService
{
    CharacterStepResult Simulate(
        in CharacterInput input,
        in CharacterBodySnapshot body,
        in CharacterSimulationState state,
        float tickDelta);
}
```

Consumer-owned provider port:

- `ICharacterGravityProvider` evaluates gravity for the requested simulation step.

An integration adapter implements `ICharacterGravityProvider` by calling `ISurfaceGravitySolver`.

`CharacterInput` contains only player intent:

- `Vector2 Move`;
- `float ViewYaw`;
- `bool JumpPressed`;
- `bool JumpHeld`.

`CharacterSimulationState` contains replayable non-rigidbody state, including:

- gravity selection/smoothing state;
- jump phase and elapsed jump state;
- current view yaw;
- other deterministic motor state added later.

`CharacterStepResult` describes velocity/force changes, target physical rotation, and the next simulation state. It does not directly mutate a `Rigidbody`.

### 4.7 `Character.UnityRuntime`

Adapts ordinary Unity physics to the same character simulation for the offline `Playground` scene:

- gathers local input;
- steps on Unity `FixedUpdate`;
- reads an ordinary `Rigidbody` snapshot;
- invokes `ICharacterSimulationService`;
- applies the result once to the `Rigidbody`;
- updates the presentation anchor in `LateUpdate`.

Offline play does not start FishNet manual physics. The `Playground` remains on Unity physics timing, avoiding the previous mismatch where gravity/movement ran in `FixedUpdate` while FishNet simulated physics in `Update`.

### 4.8 `Multiplayer.Session.Networking`

Maps `Base.Network` DTOs to and from `Multiplayer.Session` calls. It is the only session layer that knows `IMessagePayload`, route IDs, message contexts, and connection metadata.

The server derives `PlayerId` from the authenticated message context/connection mapping. It never trusts a player identifier supplied by a client payload.

### 4.9 `FishNetworking.Gameplay`

Contains FishNet-specific runtime adapters:

- `PredictedCharacterNetworkBehaviour`;
- `FishNetPlayerEntityRuntimeProvider`;
- `FishNetMatchWorldProvider`;
- connection-to-`PlayerId` mapping;
- predicted dynamic-rigidbody behaviours;
- prediction-aware collision adapters.

It translates between FishNet replicate/reconcile data and pure character types. It does not decide lobby, spawn-point, or motor policy.

### 4.10 `Client.Presentation`

Owns client-only input, camera, HUD, audio listener, and visual smoothing.

Only the locally owned player enables these objects. The camera follows a presentation anchor/graphical child, not the rollback physics root, so reconciliation corrections do not appear as raw camera snaps.

### 4.11 `DedicatedServer.Bootstrap`

Is the dedicated headless composition root. It installs server facades and FishNet adapters but no input, camera, audio listener, HUD, or local-player presentation.

Runtime role comes from an immutable startup profile/build configuration. It is not selected by a mutable global `ScriptableObject` flag.

## 5. Integration adapters and dependency direction

Examples of legal connections:

```text
Multiplayer.Session
  -> owns IPlayerSpawnProvider
  <- SessionSpawningAdapter
  -> calls IPlayerSpawnService

Multiplayer.Spawning
  -> owns IPlayerEntityRuntimeProvider
  <- FishNetPlayerEntityRuntimeProvider

Character.Simulation
  -> owns ICharacterGravityProvider
  <- CharacterSurfaceGravityAdapter
  -> calls ISurfaceGravitySolver
```

Composition roots create these bridges. Neither side imports the implementation assembly merely to discover a service.

Installers are composition entry points for pure integration assemblies. Application modules still expose a facade/service as their functional entry point.

## 6. Authoritative session model

### 6.1 Phases

The server session has these phases:

```text
WaitingForPlayers -> Lobby -> LoadingMatch -> Playing -> Stopping
```

- `WaitingForPlayers`: fewer than two lobby players are connected.
- `Lobby`: at least two players are connected; ready changes are accepted.
- `LoadingMatch`: match start is committed and the global scene is loading.
- `Playing`: server world is ready and eligible players are spawned.
- `Stopping`: controlled shutdown/teardown.

Returning from `Lobby` to `WaitingForPlayers` is allowed when a player leaves. An initial load may be cancelled back to `Lobby` or `WaitingForPlayers` according to the remaining player count.

### 6.2 Start condition

The transition from `Lobby` to `LoadingMatch` occurs only when:

- at least 2 players are connected;
- every player currently counted in the lobby is ready;
- no start operation is already committed.

The check and transition are atomic within the serialized command queue. Once start is committed, later connections are join-in-progress players and do not invalidate or block the start.

### 6.3 Snapshot

`SessionSnapshot` contains:

- `SessionId`;
- optional `MatchId` and `MapId`;
- `SessionPhase`;
- monotonic `Revision`;
- ordered player summaries with `PlayerId`, ready state, world-ready state, and spawn state.

Every accepted state mutation increments `Revision`. An idempotent command that does not change state keeps the revision and may resend the latest snapshot as its acknowledgement. The client ignores snapshots whose revision is lower than or equal to the latest applied revision, except an explicitly defined reconnect reset with a new `SessionId`.

### 6.4 Async operation safety

Starting a world load creates an `OperationId`. Every load/readiness callback carries that ID and the expected `MatchId`. A callback from a cancelled or superseded operation is ignored and logged; it cannot move the active session forward.

## 7. Network messages

Session DTOs carried through `Base.Network` are intentionally small.

Client to server:

- `SetReadyMessage { bool Ready; }`.

Server to client:

- `JoinAcceptedMessage` with the assigned `PlayerId`, `SessionId`, and current revision;
- `SessionSnapshotMessage`;
- `SessionCommandRejectedMessage` with a stable reason code.

Ready messages contain no `PlayerId`. The server resolves the sender through the message context.

Movement input is never sent through these session DTOs. It uses FishNet replication so its tick, replay, and reconciliation lifecycle stays unified with predicted physics.

Later discrete features such as shooting, reloads, and grenades add their own modules and `Base.Network` adapters. They consume authoritative gameplay services rather than calling session or FishNet behaviours directly.

## 8. Runtime flows

### 8.1 Dedicated-server startup

1. The headless bootstrap selects the server startup profile.
2. FishNet starts in dedicated-server mode with no client instance.
3. FishNet `TimeManager` owns manual physics stepping for the networked runtime.
4. Baseline tick rate is 50 Hz and is configurable by profile.
5. The server enters `WaitingForPlayers` and listens on the configured IP/port.

The offline profile does not use FishNet manual physics and keeps Unity `FixedUpdate` physics.

### 8.2 Connection and lobby

1. `Base.Network` reports a new server connection.
2. The session networking adapter calls `IServerSessionFacade.JoinAsync`.
3. The server assigns a neutral `PlayerId`; only an integration mapping knows the FishNet connection.
4. If the session is full, the server sends `SessionFull` and closes the connection.
5. Otherwise it sends `JoinAcceptedMessage` and the latest snapshot reliably.
6. A ready command is validated, applied idempotently, and followed by a new authoritative snapshot.

### 8.3 Match load

1. When the ready condition becomes true, the session creates `MatchId` and `OperationId` and enters `LoadingMatch`.
2. `IMatchWorldProvider` maps to FishNet scene management and loads one global match scene.
3. Server world readiness and each player's world readiness are tracked separately.
4. No player entity is spawned before both the server world and that player's client world are ready.
5. Once the initial eligible players are spawned, the session enters `Playing`.

A global FishNet scene is used because existing and future connections must observe the same match objects.

### 8.4 Server-authoritative spawn

1. Session requests a spawn using neutral IDs and match context.
2. Spawning reserves a server-selected spawn pose.
3. `IPlayerEntityRuntimeProvider` creates the player entity.
4. The FishNet adapter spawns an owned `NetworkObject` into the global match scene.
5. The spawn registry records the handle and the session publishes the new snapshot.

The client cannot select or submit its authoritative spawn pose.

### 8.5 Join in progress

1. A connection accepted during `LoadingMatch` after start commitment or during `Playing` is marked as join in progress.
2. FishNet loads/observes the current global match scene for that connection.
3. Session waits for that player's world-ready signal.
4. The server spawns the owned player entity using the normal idempotent spawn path.
5. Existing observed network objects supply the current world state.

A join-in-progress load timeout disconnects only that client; it does not stop the match.

### 8.6 Disconnect

Disconnect handling is idempotent in every phase:

- remove the connection mapping;
- cancel that player's pending load/spawn work;
- despawn an existing entity;
- release its spawn reservation;
- remove it from the session snapshot.

If a disconnect during the initial `LoadingMatch` leaves fewer than two players, the start is cancelled and remaining ready flags are reset. If the match is already `Playing`, it continues even with one remaining player.

## 9. Predicted character simulation

### 9.1 Input collection

Client presentation samples continuous controls every rendered frame and buffers edge-triggered actions such as `JumpPressed` until consumed by a network tick.

On each FishNet tick, the owning client builds replicate data containing only:

- clamped movement axes;
- view-yaw intent;
- jump pressed/held flags;
- FishNet replication metadata.

The server validates finite values, clamps axes and angular deltas, and rejects impossible command rates. The payload never includes transforms or derived physical state.

### 9.2 Replicate step

The FishNet behaviour's replicate method:

1. maps FishNet replicate data to `CharacterInput`;
2. reads the predicted body snapshot;
3. calls `ICharacterSimulationService` with FishNet tick delta;
4. applies the returned forces, velocity changes, and physical rotation through `PredictionRigidbody`;
5. calls the FishNet prediction-rigidbody simulation path exactly once.

The same simulation code runs for owner prediction, server authority, and replay. No gameplay `FixedUpdate` competes with this tick on networked characters.

### 9.3 Physics step

FishNet advances the global physics scene once per tick. Unity physics resolves:

- character/world contacts;
- player/player contacts;
- character/dynamic-body contacts;
- dynamic-body interactions.

The server result is final.

### 9.4 Reconcile step

After authoritative physics, reconcile data captures:

- the complete `PredictionRigidbody` state;
- `CharacterSimulationState`;
- gravity selection and smoothing state;
- jump state;
- view yaw needed by the motor.

The owner restores the authoritative state and FishNet replays unacknowledged inputs. Reconciliation must restore all non-rigidbody state that can affect the next simulation step; otherwise gravity or jump behavior will diverge even if the transform is corrected.

### 9.5 Remote players and state forwarding

For the initial 10-player target, predicted player objects enable FishNet state forwarding so collision partners have useful historical/predicted state during replay. Bandwidth and CPU cost are measured before considering a more selective strategy.

No client-authoritative network transform controls the physics root.

### 9.6 Presentation and camera

The networked player is split into:

- an authoritative/predicted physics root;
- a graphical presentation child or anchor;
- local-only camera/input/audio components.

The physics root may be corrected immediately. The presentation layer smooths small corrections and resets on teleport-sized corrections. The camera follows the presentation anchor during `LateUpdate`, preventing visible camera jitter from raw rollback motion.

## 10. Gravity and physical-world rules

### 10.1 Static surface gravity

Gravity surfaces are static and identical on server and clients. A gravity step uses the character position, prior gravity state, and tick delta to choose/query a surface and produce:

- acceleration/force direction and magnitude;
- target up direction;
- next stable surface selection;
- next smoothing state.

Surface switching hysteresis and orientation smoothing are explicit replayable state. They do not depend on frame delta, wall-clock time, mutable singleton order, or Unity instance IDs.

### 10.2 Dynamic rigidbodies

Networked movable rigidbodies are server-authoritative predicted/reconciled objects. External forces that can participate in replay are applied through FishNet prediction-rigidbody facilities.

Non-networked rigidbodies that may be encountered during replay use FishNet's offline-rigidbody support so replay does not simulate their reactions multiple times incorrectly.

Dynamic rigidbodies may collide with and be pushed by players in the first slice. They are not gravity sources.

### 10.3 Collision callbacks

Gameplay collision logic uses FishNet prediction-aware collision callbacks. Replay may repeat physical contacts, so irreversible presentation effects such as sound, particles, decals, or analytics are emitted only from confirmed/deduplicated gameplay events.

## 11. Error model and recovery

Expected errors are returned as typed results.

`SessionError` includes:

- `SessionFull`;
- `UnknownPlayer`;
- `InvalidPhase`;
- `NotEnoughPlayers`;
- `WorldLoadFailed`;
- `WorldLoadTimeout`;
- `PlayerLoadTimeout`;
- `SpawnFailed`;
- `ConnectionClosed`.

`SpawnError` includes:

- `ConnectionUnavailable`;
- `WorldNotReady`;
- `NoSpawnPoint`;
- `EntityCreationFailed`;
- `Cancelled`.

Exceptions are reserved for configuration/programming defects such as duplicate wire type IDs, missing installer bindings, a player prefab without a `NetworkObject`, an unknown configured map, or duplicate authored `SurfaceId` values.

Recovery policy:

- initial world-load failure: cancel the operation, return to the appropriate lobby phase, reset ready flags, and publish the reason;
- stale async callback: ignore and log with operation IDs;
- out-of-order client snapshot: ignore using revision/session identity;
- first spawn failure: release the reservation and retry once at another spawn point;
- second spawn failure: reject/disconnect that player with `SpawnFailed`;
- join-in-progress load timeout: disconnect only the joining client;
- disconnect during play: despawn that player and continue the match.

## 12. Observability

Structured logs use stable IDs and avoid engine-object names as identity. Important events include:

- connection accepted/rejected/disconnected;
- session phase and revision transitions;
- match-load operation start/success/cancel/failure;
- per-player world readiness;
- spawn reservation, retry, success, and despawn;
- reconciliation correction magnitude;
- dropped/stale input and rejected values;
- tick duration and physics-step duration.

Development metrics track correction distance/angle distributions, replay count, tick overruns, spawn latency, scene-load latency, and per-client message rate.

## 13. Test strategy

### 13.1 Unit tests

`Multiplayer.Session`:

- phase transitions and minimum-player rule;
- all-ready atomic start;
- repeated ready/join/leave commands;
- join in progress not blocking a committed start;
- disconnect in every phase;
- stale `OperationId` callbacks;
- monotonic snapshot revisions;
- load/spawn failure recovery.

`Multiplayer.Spawning`:

- reservations and release;
- duplicate spawn returns the same handle;
- absent despawn returns `AlreadyAbsent`;
- retry selects another point;
- cancellation/disconnect during creation.

`SurfaceGravity`:

- surface selection for supported geometry;
- hysteresis and normal smoothing;
- stable source identity;
- identical results for repeated input/state sequences;
- behavior at seams and equidistant surfaces.

`Character.Simulation`:

- movement relative to gravity up;
- grounded/air control policy;
- jump press/hold transitions;
- orientation alignment;
- deterministic state replay for the same input and body snapshots.

### 13.2 Contract tests

Each adapter is tested against its consumer-owned port contract:

- session-to-spawning adapter;
- session-to-FishNet world provider;
- FishNet entity runtime provider;
- session network DTO mapping and sender identity;
- character-to-gravity adapter.

### 13.3 Unity PlayMode tests

- offline and predicted adapters produce equivalent character commands for the same step inputs;
- walking and jumping across planet/surface transitions;
- reconcile restores gravity and jump state;
- player/player collisions;
- pushing a server-authoritative dynamic box;
- rollback does not duplicate confirmed collision effects;
- camera follows the smoothed presentation anchor rather than the corrected physics root.

### 13.4 Multi-process tests

Automated builds run one headless dedicated server and real client processes:

- two clients connect by IP/port, become ready, and trigger exactly one scene load;
- both receive exactly one correctly owned player entity;
- a third client joins an active match and spawns after world readiness;
- disconnects during lobby, loading, spawn, and play are cleaned up;
- 10 clients remain connected and moving for at least 10 minutes;
- forced owner divergence is corrected to the server state;
- collision and dynamic-body outcomes converge.

Network profiles include localhost, 100 ms RTT, 200 ms RTT, jitter, and 2% packet loss. Thresholds for acceptable correction magnitude and tick overruns are recorded with test hardware/build profile rather than hidden as universal constants.

## 14. Acceptance criteria

The first slice is complete when:

1. A headless dedicated server starts without client input, camera, HUD, or audio objects.
2. Up to 10 clients can connect directly by IP and port.
3. Two or more connected players start the match only after all lobby players send ready.
4. The server loads the global match scene through FishNet exactly once.
5. Each world-ready client receives one server-spawned, correctly owned player object.
6. A client joining an active match loads the current world and spawns without restarting or blocking it.
7. The owning client sends input intent only and predicts its movement locally.
8. The server simulates authoritative physics and corrects forced client divergence.
9. Surface gravity, jump state, and orientation survive reconciliation and replay without systematic drift.
10. Players collide with one another and interact with server-authoritative dynamic rigidbodies.
11. Disconnects leave no player entity, reservation, or stale session entry.
12. The offline `Playground` uses the same character/gravity simulation with Unity physics timing and remains smooth.
13. Core session, spawning, gravity, and character-simulation assemblies contain no FishNet references.
14. Unit, contract, PlayMode, and multi-process tests described for the slice pass.

## 15. Implementation constraints and verification notes

- FishNet version in the repository is the implementation source of truth. Exact 4.6.12 attributes, tick callbacks, prediction APIs, scene events, and predicted-rigidbody calls must be verified against local source before coding.
- Network runtime physics uses FishNet `TimeManager`; offline runtime uses Unity `FixedUpdate`. A single runtime never has both loops applying the same character step.
- The initial performance baseline is 50 Hz, 10 players, state forwarding enabled, and one global physics scene. Profiling may tune configuration but must not weaken server authority or module boundaries.
- Existing unrelated working-tree changes are outside this design and must be preserved.

## 16. Reference documentation

- FishNet scene loading: <https://fish-networking.gitbook.io/docs/guides/features/scene-management/loading-scenes>
- FishNet spawning: <https://fish-networking.gitbook.io/docs/guides/features/networked-gameobjects-and-scripts/spawning>
- FishNet ownership: <https://fish-networking.gitbook.io/docs/guides/features/ownership>
- FishNet prediction control: <https://fish-networking.gitbook.io/docs/guides/features/prediction/creating-code/controlling-an-object>
- FishNet `PredictionRigidbody`: <https://fish-networking.gitbook.io/docs/guides/features/prediction/predictionrigidbody>
- FishNet predicted `NetworkObject` configuration: <https://fish-networking.gitbook.io/docs/guides/features/prediction/configuring-networkobject>
- FishNet offline rigidbodies: <https://fish-networking.gitbook.io/docs/guides/features/prediction/offline-rigidbodies>
