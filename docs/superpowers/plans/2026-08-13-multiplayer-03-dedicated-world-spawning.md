# Dedicated World and Player Spawning Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Start a headless dedicated server by IP/port, load one FishNet global match scene, and spawn/despawn owned players including join in progress.

**Architecture:** `Multiplayer.Spawning` selects and reserves spawn poses without transport knowledge. `FishNetworking.Gameplay` implements session world and entity-runtime ports using local FishNet 4.6.12 APIs and a connection resolver isolated in the FishNet assembly.

**Tech Stack:** Unity 6000.3.12f1, FishNet 4.6.12, Tugboat transport, Base.Network, UniTask 2.2.5, Zenject 9.3.1, NUnit/Unity Test Framework 1.6.0.

## Global Constraints

- One dedicated server process hosts one match in one global physics scene.
- Direct IP/port connection; maximum 10 players.
- The server selects spawn positions and owns spawn/despawn lifecycle.
- A player spawns only after the server world and that player's client world are ready.
- Join-in-progress loads the existing global match scene and does not restart the match.
- Core spawning code contains no FishNet, Base.Network, Unity scene-management, or `NetworkObject` types.
- Duplicate spawn returns the existing neutral handle; missing despawn returns `AlreadyAbsent`.
- Initial spawn retries once at a different point, then disconnects only that player with `SpawnFailed`.
- Preserve unrelated working-tree changes and make a focused commit after every task.

---

## File structure

```text
Assets/Scripts/Modules/Multiplayer/Spawning/
  Multiplayer.Spawning.asmdef
  Contracts/IPlayerSpawnService.cs
  Ports/ISpawnPointProvider.cs
  Ports/IPlayerEntityRuntimeProvider.cs
  Ports/ISpawnTelemetry.cs
  Model/SpawnPose.cs
  Model/SpawnHandle.cs
  Model/SpawnError.cs
  Model/PlayerSpawnRequest.cs
  Model/SpawnTelemetryEvent.cs
  Services/PlayerSpawnService.cs

Assets/Scripts/Modules/Multiplayer/SpawningUnity/
  Multiplayer.Spawning.Unity.asmdef
  SpawnPointView.cs
  SceneSpawnPointProvider.cs
  SpawnStructuredLogger.cs
  SpawningInstaller.cs

Assets/Scripts/Features/FishNetworking/
  FishNetworking.asmdef                         Existing adapter assembly boundary.
  Impl/Providers/IFishNetConnectionResolver.cs
  Impl/Providers/FishNetConnectionResolver.cs

Assets/Scripts/Modules/FishNetworking/Gameplay/
  FishNetworking.Gameplay.asmdef
  World/FishNetMatchWorldProvider.cs
  World/FishNetSessionWorldBridge.cs
  Spawning/IFishNetPlayerConnectionProvider.cs
  Spawning/SessionPlayerConnectionAdapter.cs
  Spawning/FishNetPlayerEntityRuntimeProvider.cs
  Spawning/SessionSpawningAdapter.cs
  Bootstrap/FishNetGameplayInstaller.cs

Assets/Scripts/Modules/Multiplayer/Bootstrap/
  Multiplayer.Bootstrap.asmdef
  StartupRole.cs
  MultiplayerStartupOptions.cs
  CommandLineStartupOptionsProvider.cs
  MultiplayerStartupInstaller.cs
  DedicatedServerBootstrap.cs
  MultiplayerClientBootstrap.cs

Assets/Scripts/CI/Editor/
  MultiplayerBuild.cs

Assets/Scenes/MultiplayerBootstrap.unity
Assets/Scenes/MultiplayerPlayground.unity
Assets/Prefabs/Player/NetworkPlayer.prefab
```

## Task 1: Transport-agnostic spawn service

**Files:**
- Create: `Assets/Scripts/Modules/Multiplayer/Spawning/Multiplayer.Spawning.asmdef`
- Create: `Assets/Scripts/Modules/Multiplayer/Spawning/Contracts/IPlayerSpawnService.cs`
- Create: `Assets/Scripts/Modules/Multiplayer/Spawning/Ports/ISpawnPointProvider.cs`
- Create: `Assets/Scripts/Modules/Multiplayer/Spawning/Ports/IPlayerEntityRuntimeProvider.cs`
- Create: `Assets/Scripts/Modules/Multiplayer/Spawning/Ports/ISpawnTelemetry.cs`
- Create: `Assets/Scripts/Modules/Multiplayer/Spawning/Model/SpawnPose.cs`
- Create: `Assets/Scripts/Modules/Multiplayer/Spawning/Model/SpawnHandle.cs`
- Create: `Assets/Scripts/Modules/Multiplayer/Spawning/Model/SpawnError.cs`
- Create: `Assets/Scripts/Modules/Multiplayer/Spawning/Model/PlayerSpawnRequest.cs`
- Create: `Assets/Scripts/Modules/Multiplayer/Spawning/Model/SpawnTelemetryEvent.cs`
- Create: `Assets/Scripts/Modules/Multiplayer/Spawning/Services/PlayerSpawnService.cs`
- Create: `Assets/Tests/EditMode/MultiplayerSpawning/Multiplayer.Spawning.Tests.asmdef`
- Create: `Assets/Tests/EditMode/MultiplayerSpawning/Fakes.cs`
- Create: `Assets/Tests/EditMode/MultiplayerSpawning/PlayerSpawnServiceTests.cs`

**Interfaces:**
- Consumes: `PlayerId`, `MatchId` from `Multiplayer.Primitives`.
- Produces: `IPlayerSpawnService.SpawnAsync` and `DespawnAsync`.

- [ ] **Step 1: Write failing idempotency, retry, and cleanup tests**

```csharp
[Test]
public async Task DuplicateSpawn_ReturnsExistingHandleWithoutSecondCreation()
{
    var fixture = SpawnFixture.Successful();
    var first = await fixture.Service.SpawnAsync(fixture.Request, default);
    var second = await fixture.Service.SpawnAsync(fixture.Request, default);
    Assert.That(second.Value, Is.EqualTo(first.Value));
    Assert.That(fixture.Runtime.CreateCalls, Is.EqualTo(1));
}

[Test]
public async Task FirstCreationFailure_RetriesAtDifferentPoint()
{
    var fixture = SpawnFixture.FailFirstCreation();
    var result = await fixture.Service.SpawnAsync(fixture.Request, default);
    Assert.That(result.IsSuccess, Is.True);
    Assert.That(fixture.Points.ReservedIds, Is.EqualTo(new[] { "spawn-a", "spawn-b" }));
    Assert.That(fixture.Points.ReleasedIds, Does.Contain("spawn-a"));
}

[Test]
public async Task DespawnMissing_ReturnsAlreadyAbsent()
{
    var result = await SpawnFixture.Successful().Service.DespawnAsync(Player("absent"), default);
    Assert.That(result.Value, Is.EqualTo(DespawnOutcome.AlreadyAbsent));
}
```

- [ ] **Step 2: Run tests and verify missing spawn types**

```powershell
& 'D:\Program Files\Unity\Hub\Editor\6000.3.12f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath . -runTests -testPlatform EditMode -assemblyNames Multiplayer.Spawning.Tests -testResults Temp/multiplayer-spawning.xml -logFile Temp/multiplayer-spawning.log
```

Expected: compilation fails on spawn contracts.

- [ ] **Step 3: Define neutral spawn contracts**

```csharp
public interface IPlayerSpawnService
{
    ValueTask<Result<SpawnHandle, SpawnError>> SpawnAsync(PlayerSpawnRequest request, CancellationToken token);
    ValueTask<Result<DespawnOutcome, SpawnError>> DespawnAsync(PlayerId playerId, CancellationToken token);
}

public interface ISpawnPointProvider
{
    Result<SpawnReservation, SpawnError> Reserve(PlayerId playerId, IReadOnlyCollection<string> excludedPointIds);
    void Release(SpawnReservation reservation);
}

public interface IPlayerEntityRuntimeProvider
{
    ValueTask<Result<RuntimeEntityHandle, SpawnError>> CreateAsync(PlayerEntityCreateRequest request, CancellationToken token);
    ValueTask<Result<Unit, SpawnError>> DestroyAsync(RuntimeEntityHandle handle, CancellationToken token);
}

public interface ISpawnTelemetry
{
    void Record(SpawnTelemetryEvent value);
}
```

`SpawnHandle` contains `PlayerId`, `MatchId`, neutral `RuntimeEntityHandle`, and `SpawnReservation`; no `GameObject` or `NetworkObject`.

```csharp
public enum SpawnError
{
    ConnectionUnavailable, WorldNotReady, NoSpawnPoint,
    EntityCreationFailed, Cancelled
}

public enum DespawnOutcome { Despawned, AlreadyAbsent }
```

Record reservation, first failure, retry, success, destroy, and release through `ISpawnTelemetry`; each event contains `PlayerId`, `MatchId`, point ID, attempt number, duration, and typed outcome.

```csharp
public enum SpawnTelemetryKind { Reserved, AttemptFailed, Spawned, Destroyed, Released }

public readonly struct SpawnTelemetryEvent
{
    public SpawnTelemetryKind Kind { get; }
    public PlayerId PlayerId { get; }
    public MatchId MatchId { get; }
    public string PointId { get; }
    public int Attempt { get; }
    public TimeSpan Duration { get; }
    public SpawnError? Error { get; }
}
```

- [ ] **Step 4: Implement the two-attempt idempotent service**

```csharp
public async ValueTask<Result<SpawnHandle, SpawnError>> SpawnAsync(PlayerSpawnRequest request, CancellationToken token)
{
    await _gate.WaitAsync(token);
    try
    {
        if (_spawned.TryGetValue(request.PlayerId, out var existing))
            return Result<SpawnHandle, SpawnError>.Success(existing);
        var excluded = new HashSet<string>(StringComparer.Ordinal);
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var reservation = _points.Reserve(request.PlayerId, excluded);
            if (!reservation.IsSuccess) return Result<SpawnHandle, SpawnError>.Failure(reservation.Error);
            excluded.Add(reservation.Value.PointId);
            var created = await _runtime.CreateAsync(new PlayerEntityCreateRequest(request, reservation.Value.Pose), token);
            if (created.IsSuccess)
            {
                var handle = new SpawnHandle(request.PlayerId, request.MatchId, created.Value, reservation.Value);
                _spawned.Add(request.PlayerId, handle);
                return Result<SpawnHandle, SpawnError>.Success(handle);
            }
            _points.Release(reservation.Value);
            if (created.Error != SpawnError.EntityCreationFailed)
                return Result<SpawnHandle, SpawnError>.Failure(created.Error);
            if (attempt == 1)
                return Result<SpawnHandle, SpawnError>.Failure(SpawnError.EntityCreationFailed);
        }
        throw new InvalidOperationException("Spawn attempt loop exited unexpectedly.");
    }
    finally { _gate.Release(); }
}
```

At each public boundary catch `OperationCanceledException` only when the supplied token is cancelled, release any held reservation, and return `SpawnError.Cancelled`. `IPlayerEntityRuntimeProvider` checks cancellation before creating an entity; once FishNet spawn begins, it completes and returns a handle so the caller can clean it up rather than abandoning a spawned object. `DespawnAsync` removes the registry entry only after successful runtime destruction, then releases the reservation.

- [ ] **Step 5: Run tests and commit**

```powershell
git add -- Assets/Scripts/Modules/Multiplayer/Spawning Assets/Tests/EditMode/MultiplayerSpawning
git commit -m "feat: add transport agnostic player spawning"
```

## Task 2: Scene-authored spawn points

**Files:**
- Create: `Assets/Scripts/Modules/Multiplayer/SpawningUnity/Multiplayer.Spawning.Unity.asmdef`
- Create: `Assets/Scripts/Modules/Multiplayer/SpawningUnity/SpawnPointView.cs`
- Create: `Assets/Scripts/Modules/Multiplayer/SpawningUnity/SceneSpawnPointProvider.cs`
- Create: `Assets/Scripts/Modules/Multiplayer/SpawningUnity/SpawnStructuredLogger.cs`
- Create: `Assets/Scripts/Modules/Multiplayer/SpawningUnity/SpawningInstaller.cs`
- Create: `Assets/Tests/EditMode/MultiplayerSpawning/SceneSpawnPointProviderTests.cs`

**Interfaces:**
- Consumes: `ISpawnPointProvider` owned by `Multiplayer.Spawning`.
- Produces: deterministic round-robin reservation over stable point IDs.

- [ ] **Step 1: Write failing duplicate/reservation tests**

```csharp
[Test]
public void Constructor_RejectsDuplicatePointIds()
{
    Assert.Throws<InvalidOperationException>(() => Provider(Point("same"), Point("same")));
}

[Test]
public void Reserve_SkipsReservedAndExcludedPoints()
{
    var provider = Provider(Point("a"), Point("b"), Point("c"));
    var first = provider.Reserve(Player("one"), Array.Empty<string>()).Value;
    var second = provider.Reserve(Player("two"), new HashSet<string> { "b" }).Value;
    Assert.That(first.PointId, Is.EqualTo("a"));
    Assert.That(second.PointId, Is.EqualTo("c"));
}
```

- [ ] **Step 2: Run spawning tests and verify failures**

Run Task 1 Step 2. Expected: new provider tests fail.

- [ ] **Step 3: Implement authored view and provider**

```csharp
public sealed class SpawnPointView : MonoBehaviour
{
    [SerializeField] private string _pointId;
    public string PointId => _pointId;
    public SpawnPose Pose => new(transform.position, transform.rotation);
}
```

At construction, sort views by `PointId`, reject blank/duplicate IDs, and keep a `Dictionary<string, PlayerId>` reservation table. Reservation begins after the last chosen index, wraps once, skips occupied/excluded points, and returns `NoSpawnPoint` when none remain.

Bind `ISpawnTelemetry -> SpawnStructuredLogger`. It emits JSON containing stable player/match/point IDs, attempt, elapsed milliseconds, and typed outcome.

- [ ] **Step 4: Run tests and commit**

```powershell
git add -- Assets/Scripts/Modules/Multiplayer/SpawningUnity Assets/Tests/EditMode/MultiplayerSpawning/SceneSpawnPointProviderTests.cs
git commit -m "feat: add scene spawn point provider"
```

## Task 3: FishNet assembly boundary and connection resolver

**Files:**
- Create: `Assets/Scripts/Features/FishNetworking/FishNetworking.asmdef`
- Create: `Assets/Scripts/Features/FishNetworking/Impl/Providers/IFishNetConnectionResolver.cs`
- Create: `Assets/Scripts/Features/FishNetworking/Impl/Providers/FishNetConnectionResolver.cs`
- Modify: `Assets/Scripts/Features/FishNetworking/Impl/Adapters/FishnetServerAdapter.cs`
- Modify: `Assets/Scripts/Features/FishNetworking/Impl/Adapters/FishnetClientAdapter.cs`
- Modify: `Assets/Scripts/Features/FishNetworking/Bootstrap/FishNetNetworkInstaller.cs`
- Modify: `Assets/Scripts/Base/Network/Data/ServerStartOptions.cs`
- Create: `Assets/Tests/EditMode/FishNetworking/FishNetworking.Tests.asmdef`
- Create: `Assets/Tests/EditMode/FishNetworking/FishNetConnectionResolverTests.cs`

**Interfaces:**
- Consumes: Base.Network `ConnectionId`; FishNet `NetworkConnection` only inside this assembly.
- Produces: `IFishNetConnectionResolver.TryResolve(ConnectionId, out NetworkConnection)`.

- [ ] **Step 1: Write failing resolver lifecycle tests**

```csharp
[Test]
public void Remove_MakesConnectionUnresolvable()
{
    var resolver = new FishNetConnectionResolver();
    var fishConnection = CreateFishNetConnection(clientId: 7);
    resolver.Add(new ConnectionId(7), fishConnection);
    resolver.Remove(new ConnectionId(7));
    Assert.That(resolver.TryResolve(new ConnectionId(7), out _), Is.False);
}
```

- [ ] **Step 2: Run FishNetworking tests and verify missing resolver**

```powershell
& 'D:\Program Files\Unity\Hub\Editor\6000.3.12f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath . -runTests -testPlatform EditMode -assemblyNames FishNetworking.Tests -testResults Temp/fish-networking.xml -logFile Temp/fish-networking.log
```

- [ ] **Step 3: Add the assembly definition and resolver**

The asmdef references `Network`, `FishNet.Runtime`, `UniTask`, `UniRx`, `Zenject`, and the MessagePack DLL reference already used by `Network.asmdef`. The resolver owns both `ConnectionId -> NetworkConnection` and `NetworkConnection -> ConnectionId` dictionaries and rejects duplicates.

```csharp
public interface IFishNetConnectionResolver
{
    bool TryResolve(ConnectionId id, out NetworkConnection connection);
    bool TryResolve(NetworkConnection connection, out ConnectionId id);
}
```

- [ ] **Step 4: Register/remove connections in `FishnetServerAdapter`**

Inject `FishNetConnectionResolver`. Add to it before raising `OnConnected`; remove from it before raising `OnDisconnected`. Clear it during `Dispose` and `StopAsync`.

- [ ] **Step 5: Apply direct server bind options**

```csharp
public struct ServerStartOptions
{
    public string BindAddress;
    public ushort Port;
}

public async UniTask StartAsync(ServerStartOptions options)
{
    var transport = _networkManager.TransportManager.Transport;
    transport.SetServerBindAddress(options.BindAddress, IPAddressType.IPv4);
    transport.SetPort(options.Port);
    Subscribe();
    if (!Server.StartConnection(options.Port)) throw new InvalidOperationException("FishNet server failed to start.");
    await UniTask.WaitUntil(() => Server.Started);
}
```

Validate nonblank bind address and nonzero port before mutating the transport.

- [ ] **Step 6: Run tests and commit**

```powershell
git add -- Assets/Scripts/Features/FishNetworking Assets/Scripts/Base/Network/Data/ServerStartOptions.cs Assets/Tests/EditMode/FishNetworking
git commit -m "refactor: expose fishnet connection resolver"
```

## Task 4: FishNet global match-world provider

**Files:**
- Create: `Assets/Scripts/Modules/FishNetworking/Gameplay/FishNetworking.Gameplay.asmdef`
- Create: `Assets/Scripts/Modules/FishNetworking/Gameplay/World/FishNetMatchWorldProvider.cs`
- Create: `Assets/Scripts/Modules/FishNetworking/Gameplay/World/FishNetSessionWorldBridge.cs`
- Create: `Assets/Tests/EditMode/FishNetworkingGameplay/FishNetworking.Gameplay.Tests.asmdef`
- Create: `Assets/Tests/EditMode/FishNetworkingGameplay/FishNetGameplayFixture.cs`
- Create: `Assets/Tests/EditMode/FishNetworkingGameplay/FishNetMatchWorldProviderTests.cs`

**Interfaces:**
- Consumes: session-owned `IMatchWorldProvider`, `IServerSessionFacade`; FishNet 4.6.12 `SceneManager` events.
- Produces: correlated server and per-player world-ready callbacks.

- [ ] **Step 1: Write failing event-correlation tests**

```csharp
[Test]
public async Task LoadAsync_CompletesOnlyForActiveGlobalServerLoad()
{
    var provider = Fixture.Provider;
    var pending = provider.LoadAsync(Request(Operation("active"), Match("m1"), Map("playground")), default);
    Fixture.RaiseLoadEnd(asServer: false, scene: "MultiplayerPlayground");
    Assert.That(pending.IsCompleted, Is.False);
    Fixture.RaiseLoadEnd(asServer: true, scene: "MultiplayerPlayground");
    Assert.That((await pending).IsSuccess, Is.True);
}

[Test]
public void PresenceAdded_MapsConnectionToPlayerAndNotifiesSession()
{
    Fixture.RaisePresenceAdded(clientId: 8, scene: "MultiplayerPlayground");
    Assert.That(Fixture.Session.LastWorldReadyPlayer, Is.EqualTo(Player("eight")));
}
```

- [ ] **Step 2: Run gameplay tests and verify missing provider**

```powershell
& 'D:\Program Files\Unity\Hub\Editor\6000.3.12f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath . -runTests -testPlatform EditMode -assemblyNames FishNetworking.Gameplay.Tests -testResults Temp/fish-gameplay.xml -logFile Temp/fish-gameplay.log
```

- [ ] **Step 3: Load exactly one global scene through FishNet**

```csharp
public ValueTask<Result<Unit, SessionError>> LoadAsync(MatchWorldLoadRequest request, CancellationToken token)
{
    if (_active.HasValue) return ValueTask.FromResult(Result<Unit, SessionError>.Failure(SessionError.InvalidPhase));
    _active = new ActiveLoad(request, new TaskCompletionSource<Result<Unit, SessionError>>());
    var data = new SceneLoadData(_maps.SceneName(request.MapId));
    data.ReplaceScenes = ReplaceOption.OnlineOnly;
    _sceneManager.LoadGlobalScenes(data);
    return AwaitActiveLoadAsync(_active.Value, token);
}

private static async ValueTask<Result<Unit, SessionError>> AwaitActiveLoadAsync(ActiveLoad load, CancellationToken token)
{
    using var registration = token.Register(() =>
        load.Completion.TrySetResult(Result<Unit, SessionError>.Failure(SessionError.WorldLoadTimeout)));
    return await load.Completion.Task;
}
```

On `OnLoadEnd`, require `args.QueueData.AsServer`, `SceneScopeType.Global`, and the expected scene in loaded or skipped names. Complete once, cache the loaded Unity `Scene`, and clear the pending load. `CancelAsync` uses `UnloadGlobalScenes(new SceneUnloadData(scene))` when the scene completed, or marks the operation cancelled so its later callback is ignored.

- [ ] **Step 4: Bridge client presence to player readiness**

Subscribe to `OnClientPresenceChangeEnd`. Require `Added == true` and the active match scene. Resolve `NetworkConnection -> ConnectionId -> PlayerId`, then call `NotifyPlayerWorldReadyAsync(active.OperationId, playerId, active.MatchId)`.

- [ ] **Step 5: Run tests and commit**

```powershell
git add -- Assets/Scripts/Modules/FishNetworking/Gameplay/World Assets/Scripts/Modules/FishNetworking/Gameplay/FishNetworking.Gameplay.asmdef Assets/Tests/EditMode/FishNetworkingGameplay
git commit -m "feat: load fishnet global match world"
```

## Task 5: FishNet player entity runtime and session adapter

**Files:**
- Create: `Assets/Scripts/Modules/FishNetworking/Gameplay/Spawning/IFishNetPlayerConnectionProvider.cs`
- Create: `Assets/Scripts/Modules/FishNetworking/Gameplay/Spawning/SessionPlayerConnectionAdapter.cs`
- Create: `Assets/Scripts/Modules/FishNetworking/Gameplay/Spawning/FishNetPlayerEntityRuntimeProvider.cs`
- Create: `Assets/Scripts/Modules/FishNetworking/Gameplay/Spawning/SessionSpawningAdapter.cs`
- Create: `Assets/Scripts/Modules/FishNetworking/Gameplay/Bootstrap/FishNetGameplayInstaller.cs`
- Create: `Assets/Tests/EditMode/FishNetworkingGameplay/FishNetPlayerEntityRuntimeProviderTests.cs`
- Modify: `Assets/Scripts/Modules/Multiplayer/SessionNetworking/Runtime/SessionConnectionRegistry.cs`

**Interfaces:**
- Consumes: spawn-owned `IPlayerEntityRuntimeProvider`, session-owned `IPlayerSpawnProvider`.
- Produces: owned FishNet spawn/despawn while returning neutral handles upstream.

- [ ] **Step 1: Write failing ownership and missing-connection tests**

```csharp
[Test]
public async Task CreateAsync_MissingPlayerConnection_ReturnsConnectionUnavailable()
{
    var provider = Fixture.ProviderWithoutConnection();
    var result = await provider.CreateAsync(Fixture.CreateRequest, default);
    Assert.That(result.Error, Is.EqualTo(SpawnError.ConnectionUnavailable));
}

[Test]
public async Task CreateAsync_SpawnsPrefabWithCorrectOwnerAndMatchScene()
{
    var result = await Fixture.Provider.CreateAsync(Fixture.CreateRequest, default);
    Assert.That(result.IsSuccess, Is.True);
    Assert.That(Fixture.Server.LastSpawnOwner.ClientId, Is.EqualTo(8));
    Assert.That(Fixture.Server.LastSpawnScene.name, Is.EqualTo("MultiplayerPlayground"));
}
```

- [ ] **Step 2: Run gameplay tests and verify failures**

Run Task 4 Step 2.

- [ ] **Step 3: Expose player-to-connection lookup without leaking it to spawning core**

Add `TryGetConnectionId(PlayerId, out ConnectionId)` to the session-network registry. `SessionPlayerConnectionAdapter` implements the FishNet gameplay-owned port by chaining that lookup with `IFishNetConnectionResolver`.

```csharp
public interface IFishNetPlayerConnectionProvider
{
    bool TryGet(PlayerId playerId, out NetworkConnection connection);
}
```

- [ ] **Step 4: Implement authoritative owned creation/destruction**

```csharp
public ValueTask<Result<RuntimeEntityHandle, SpawnError>> CreateAsync(PlayerEntityCreateRequest request, CancellationToken token)
{
    if (!_connections.TryGet(request.PlayerId, out var owner)) return Failure(SpawnError.ConnectionUnavailable);
    if (!_world.TryGetLoadedScene(request.MatchId, out var scene)) return Failure(SpawnError.WorldNotReady);
    var instance = _networkManager.GetPooledInstantiated(_playerPrefab, request.Pose.Position, request.Pose.Rotation, true);
    if (instance == null) return Failure(SpawnError.EntityCreationFailed);
    _networkManager.ServerManager.Spawn(instance, owner, scene);
    var id = new RuntimeEntityHandle(Guid.NewGuid());
    _entities.Add(id, instance);
    return Success(id);
}
```

Destroy resolves the neutral handle, calls `ServerManager.Despawn`, removes the registry entry, and succeeds idempotently when the object already vanished after disconnect.

- [ ] **Step 5: Implement the session-to-spawning adapter**

Map `SpawnPlayerRequest` to `PlayerSpawnRequest`. Map success to session `SpawnPlayerResult`; map a second `EntityCreationFailed` to `SessionError.SpawnFailed`. Despawn delegates by `PlayerId`.

- [ ] **Step 6: Bind only through provider interfaces and run tests**

`FishNetGameplayInstaller` binds:

```text
IMatchWorldProvider -> FishNetMatchWorldProvider
IPlayerSpawnProvider -> SessionSpawningAdapter
IPlayerEntityRuntimeProvider -> FishNetPlayerEntityRuntimeProvider
IFishNetPlayerConnectionProvider -> SessionPlayerConnectionAdapter
```

After those provider bindings, the dedicated composition calls `SessionInstaller.InstallServer(Container, sessionConfiguration)`. Client composition calls `SessionInstaller.InstallClient(Container)` and binds its command publisher in `SessionNetworkInstaller`.

Run Task 4 Step 2; expect zero failures.

- [ ] **Step 7: Commit**

```powershell
git add -- Assets/Scripts/Modules/FishNetworking/Gameplay Assets/Scripts/Modules/Multiplayer/SessionNetworking/Runtime/SessionConnectionRegistry.cs Assets/Tests/EditMode/FishNetworkingGameplay
git commit -m "feat: spawn owned fishnet players"
```

## Task 6: Immutable startup roles and dedicated server build

**Files:**
- Create: `Assets/Scripts/Modules/Multiplayer/Bootstrap/Multiplayer.Bootstrap.asmdef`
- Create: `Assets/Scripts/Modules/Multiplayer/Bootstrap/StartupRole.cs`
- Create: `Assets/Scripts/Modules/Multiplayer/Bootstrap/MultiplayerStartupOptions.cs`
- Create: `Assets/Scripts/Modules/Multiplayer/Bootstrap/CommandLineStartupOptionsProvider.cs`
- Create: `Assets/Scripts/Modules/Multiplayer/Bootstrap/MultiplayerStartupInstaller.cs`
- Create: `Assets/Scripts/Modules/Multiplayer/Bootstrap/DedicatedServerBootstrap.cs`
- Create: `Assets/Scripts/Modules/Multiplayer/Bootstrap/MultiplayerClientBootstrap.cs`
- Create: `Assets/Scripts/CI/Editor/MultiplayerBuild.cs`
- Modify: `Assets/Scripts/CI/Editor/BuildMenu.cs`
- Modify: `Assets/Scripts/Features/FishNetworking/Bootstrap/FishNetNetworkInstaller.cs`
- Delete: `Assets/Scripts/Features/FishNetworking/Impl/Data/FishNetGeneralSettings.cs`
- Delete: `Assets/Data/Network/FishNetGeneralSettings.asset`
- Modify: `Assets/Prefabs/Network/Network.prefab`
- Modify: `Assets/Resources/ProjectContext.prefab`
- Create: `Assets/Tests/EditMode/MultiplayerBootstrap/Multiplayer.Bootstrap.Tests.asmdef`
- Create: `Assets/Tests/EditMode/MultiplayerBootstrap/CommandLineStartupOptionsProviderTests.cs`

**Interfaces:**
- Consumes: `INetworkServer.StartAsync`, `INetworkClient.ConnectAsync`.
- Produces: `-role server|client -address 127.0.0.1 -port 7777` composition without mutable assets.

- [ ] **Step 1: Write failing command-line parsing tests**

```csharp
[TestCase("server", "0.0.0.0", "7777", StartupRole.DedicatedServer, "0.0.0.0", 7777)]
[TestCase("client", "127.0.0.1", "8888", StartupRole.Client, "127.0.0.1", 8888)]
public void Parse_ValidArguments_ReturnsImmutableOptions(string role, string address, string port,
    StartupRole expectedRole, string expectedAddress, ushort expectedPort)
{
    var result = CommandLineStartupOptionsProvider.Parse(new[] { "game", "-role", role, "-address", address, "-port", port });
    Assert.That(result.Role, Is.EqualTo(expectedRole));
    Assert.That(result.Address, Is.EqualTo(expectedAddress));
    Assert.That(result.Port, Is.EqualTo(expectedPort));
    Assert.That(result.TickRate, Is.EqualTo(50));
}
```

- [ ] **Step 2: Run bootstrap tests and verify missing parser**

```powershell
& 'D:\Program Files\Unity\Hub\Editor\6000.3.12f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath . -runTests -testPlatform EditMode -assemblyNames Multiplayer.Bootstrap.Tests -testResults Temp/multiplayer-bootstrap.xml -logFile Temp/multiplayer-bootstrap.log
```

- [ ] **Step 3: Implement immutable startup options**

```csharp
public enum StartupRole
{
    Offline,
    Client,
    DedicatedServer
}

public readonly struct MultiplayerStartupOptions
{
    public StartupRole Role { get; }
    public string Address { get; }
    public ushort Port { get; }
    public ushort TickRate { get; }
    public bool AutoReady { get; }
    public MultiplayerStartupOptions(StartupRole role, string address, ushort port, ushort tickRate, bool autoReady)
    {
        if (string.IsNullOrWhiteSpace(address)) throw new ArgumentException("Address is required.", nameof(address));
        if (port == 0) throw new ArgumentOutOfRangeException(nameof(port));
        if (tickRate is < 20 or > 128) throw new ArgumentOutOfRangeException(nameof(tickRate));
        Role = role; Address = address; Port = port; TickRate = tickRate; AutoReady = autoReady;
    }
}
```

Role resolution order is explicit command line, `UNITY_SERVER`, `MULTIPLAYER_CLIENT`, then `Offline`. Address/port defaults are `127.0.0.1:7777`, tick rate defaults to `50`, and auto-ready defaults false. Accept `-tickRate 50`; reject values outside 20-128. `MultiplayerStartupInstaller` binds the immutable result before the network installer executes.

- [ ] **Step 4: Replace mutable `FishNetGeneralSettings` branching**

Inject `MultiplayerStartupOptions` into `FishNetNetworkInstaller`. Bind server or client adapters based on `Role`. For Client/DedicatedServer call `TimeManager.SetPhysicsMode(PhysicsMode.TimeManager)`; for Offline skip Base.Network/FishNet adapter bindings and call `TimeManager.SetPhysicsMode(PhysicsMode.Unity)`. This is required because the shared `ProjectContext` still contains the Network prefab while `Playground` is offline.

Call `TimeManager.SetTickRate(options.TickRate)` for both network roles before either socket starts; server and clients in a test/build profile use the same value.

Add `MultiplayerStartupInstaller` to `ProjectContext.prefab` before `FishNetNetworkInstaller` in installer order, and remove the serialized `FishNetGeneralSettings` reference from `Network.prefab`.

- [ ] **Step 5: Implement server/client startup**

```csharp
public async UniTaskVoid Start()
{
    await _server.StartAsync(new ServerStartOptions { BindAddress = _options.Address, Port = _options.Port });
}
```

Client uses `ClientConnectOptions { Host = Address, Port = Port, Timeout = TimeSpan.FromSeconds(10) }`; if `AutoReady`, it waits for `JoinAccepted` then calls client session `SetReadyAsync(true)`.

- [ ] **Step 6: Add a real Unity dedicated-server build entry**

Use `BuildPipeline.BuildPlayer` with `BuildPlayerOptions.subtarget = (int)StandaloneBuildSubtarget.Server`, `BuildTarget.StandaloneWindows64`, output `Build/windows/server/planetoid-server.exe`, and scenes `MultiplayerBootstrap` plus `MultiplayerPlayground`. Client output is `Build/windows/client/planetoid-client.exe`, uses the player subtarget, and adds the `MULTIPLAYER_CLIENT` extra scripting define. Replace the existing `BuildMenu` bodies with calls to these two methods, removing all asset mutation.

- [ ] **Step 7: Run tests and commit**

```powershell
git add -- Assets/Scripts/Modules/Multiplayer/Bootstrap Assets/Scripts/CI/Editor/MultiplayerBuild.cs Assets/Scripts/CI/Editor/BuildMenu.cs Assets/Scripts/Features/FishNetworking Assets/Data/Network/FishNetGeneralSettings.asset Assets/Prefabs/Network/Network.prefab Assets/Resources/ProjectContext.prefab Assets/Tests/EditMode/MultiplayerBootstrap
git commit -m "feat: add dedicated multiplayer startup"
```

## Task 7: Match scenes, prefab, and lifecycle smoke test

**Files:**
- Create: `Assets/Scenes/MultiplayerBootstrap.unity`
- Create: `Assets/Scenes/MultiplayerPlayground.unity`
- Create: `Assets/Prefabs/Player/NetworkPlayer.prefab`
- Modify: `Assets/DefaultPrefabObjects.asset`
- Modify: `ProjectSettings/EditorBuildSettings.asset`
- Create: `Assets/Tests/PlayMode/MultiplayerLifecycle/Multiplayer.Lifecycle.Tests.asmdef`
- Create: `Assets/Tests/PlayMode/MultiplayerLifecycle/DedicatedLifecycleTests.cs`
- Create: `docs/testing/multiplayer-foundation-plan-03.md`

**Interfaces:**
- Consumes: session, world, spawn, FishNet, and bootstrap modules.
- Produces: real dedicated server lifecycle before prediction is added.

- [ ] **Step 1: Create a failing PlayMode lifecycle test**

```csharp
[UnityTest]
public IEnumerator TwoReadyPlayers_LoadOnce_AndSpawnOwnedEntities()
{
    yield return _harness.StartServer();
    yield return _harness.ConnectClients(2);
    yield return _harness.SetAllReady();
    yield return _harness.WaitForPhase(SessionPhase.Playing, 10f);
    Assert.That(_harness.GlobalLoadCount, Is.EqualTo(1));
    Assert.That(_harness.SpawnedPlayers, Has.Count.EqualTo(2));
    Assert.That(_harness.SpawnedPlayers.All(x => x.Owner.IsValid), Is.True);
}
```

- [ ] **Step 2: Author the two scenes and prefab**

`MultiplayerBootstrap` contains one `SceneContext`, role bootstrap, session/gameplay installers, and no camera/audio listener. Unity loads the existing `Resources/ProjectContext.prefab` automatically; that persistent context owns the Network prefab and `MultiplayerStartupInstaller`, so the scene must not create a second `ProjectContext` or `NetworkManager`. `MultiplayerPlayground` starts from a clean copy of the planet geometry in `Playground`, contains static gravity views, at least 10 spawn points named `spawn-00` through `spawn-09`, one scene context, and no preplaced player.

`NetworkPlayer.prefab` starts as a copy of `Player.prefab`, has a spawnable `NetworkObject`, Rigidbody `useGravity=false`, CapsuleCollider, a graphical child, and no demo `RigidbodyPrediction`. Prediction behaviour is added in Plan 04.

Register `NetworkPlayer.prefab` in `Assets/DefaultPrefabObjects.asset` and verify its FishNet prefab ID is no longer `65535`; otherwise pooled server creation cannot produce a spawnable object.

- [ ] **Step 3: Run PlayMode lifecycle tests**

```powershell
& 'D:\Program Files\Unity\Hub\Editor\6000.3.12f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath . -runTests -testPlatform PlayMode -assemblyNames Multiplayer.Lifecycle.Tests -testResults Temp/multiplayer-lifecycle.xml -logFile Temp/multiplayer-lifecycle.log
```

Expected: two fake/local clients load one global scene and receive owned entities; disconnect removes the matching entity and reservation.

- [ ] **Step 4: Build and run real processes**

```powershell
& 'D:\Program Files\Unity\Hub\Editor\6000.3.12f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath . -executeMethod CI.Editor.MultiplayerBuild.BuildServer -logFile Temp/build-server.log
& 'D:\Program Files\Unity\Hub\Editor\6000.3.12f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath . -executeMethod CI.Editor.MultiplayerBuild.BuildClient -logFile Temp/build-client.log
```

Start server with `-batchmode -nographics -role server -address 0.0.0.0 -port 7777`, then two clients with `-role client -address 127.0.0.1 -port 7777 -autoReady`. Verify server logs one load and two owned spawns. Start a third client after `Playing`; verify JIP spawn without phase reset.

- [ ] **Step 5: Record results and commit**

Record build hashes, command lines, connect/load/spawn timings, JIP result, and disconnect cleanup evidence.

```powershell
git add -- Assets/Scenes/MultiplayerBootstrap.unity Assets/Scenes/MultiplayerPlayground.unity Assets/Prefabs/Player/NetworkPlayer.prefab Assets/DefaultPrefabObjects.asset ProjectSettings/EditorBuildSettings.asset Assets/Tests/PlayMode/MultiplayerLifecycle docs/testing/multiplayer-foundation-plan-03.md
git commit -m "feat: complete dedicated spawn lifecycle"
```
