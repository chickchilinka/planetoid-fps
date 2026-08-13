# Multiplayer Session and Lobby Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver a transport-agnostic 10-player lobby with authoritative ready-state transitions and reliable `Base.Network` synchronization.

**Architecture:** `Multiplayer.Session` owns server/client facades and a serialized state machine. `Multiplayer.Session.Networking` maps `Base.Network` connections and MessagePack DTOs to consumer-owned ports; no session rule knows FishNet types.

**Tech Stack:** Unity 6000.3.12f1, C# 9, Base.Network, MessagePack, UniTask 2.2.5 adapters, NUnit/Unity Test Framework 1.6.0, Zenject 9.3.1.

## Global Constraints

- Dedicated server only; no host/listen-server mode.
- Direct IP and port connection; no matchmaking or external lobby service.
- Maximum 10 players; minimum 2 to start.
- Match start is committed only when every connected lobby player is ready.
- Connections accepted after start commitment are join-in-progress players and do not block start.
- A ready payload never contains `PlayerId`; sender identity comes from `MessageContext.Source`.
- Expected runtime failures use typed result values; configuration defects throw.
- Core session code contains no `Base.Network`, MessagePack, Unity scene, or FishNet references.
- Preserve unrelated working-tree changes and commit after every independently testable task.

---

## File structure

```text
Assets/Scripts/Modules/Multiplayer/Primitives/
  Multiplayer.Primitives.asmdef
  Identifiers.cs                         PlayerId, SessionId, MatchId, MapId, OperationId.
  Result.cs                              Result<TValue,TError> and Unit.

Assets/Scripts/Modules/Multiplayer/Session/
  Multiplayer.Session.asmdef
  Contracts/IServerSessionFacade.cs
  Contracts/IClientSessionFacade.cs
  Ports/IMatchWorldProvider.cs
  Ports/IPlayerSpawnProvider.cs
  Ports/ISessionEventPublisher.cs
  Ports/ISessionConnectionProvider.cs
  Ports/ISessionTelemetry.cs
  Ports/IIdentifierProvider.cs
  Model/SessionConnection.cs
  Model/SessionPhase.cs
  Model/SessionError.cs
  Model/SessionPlayer.cs
  Model/SessionSnapshot.cs
  Model/SessionConfiguration.cs
  Model/SessionTelemetryEvent.cs
  Services/SessionCommandQueue.cs
  Services/ServerSessionFacade.cs
  Services/ClientSessionFacade.cs
  Bootstrap/SessionInstaller.cs

Assets/Scripts/Modules/Multiplayer/SessionNetworking/
  Multiplayer.Session.Networking.asmdef
  Data/SessionMessageTypeIds.cs
  Data/SetReadyMessage.cs
  Data/JoinAcceptedMessage.cs
  Data/SessionSnapshotMessage.cs
  Data/SessionCommandRejectedMessage.cs
  Mapping/SessionDtoMapper.cs
  Runtime/SessionConnectionRegistry.cs
  Runtime/ServerSessionNetworkBridge.cs
  Runtime/ClientSessionNetworkBridge.cs
  Runtime/SetReadyMessageHandler.cs
  Runtime/SessionSnapshotMessageHandler.cs
  Runtime/JoinAcceptedMessageHandler.cs
  Runtime/SessionCommandRejectedMessageHandler.cs
  Runtime/SessionStructuredLogger.cs
  Bootstrap/SessionNetworkInstaller.cs

Assets/Tests/EditMode/MultiplayerPrimitives/
Assets/Tests/EditMode/MultiplayerSession/
Assets/Tests/EditMode/MultiplayerSessionNetworking/
```

## Task 1: Shared multiplayer value types and typed results

**Files:**
- Create: `Assets/Scripts/Modules/Multiplayer/Primitives/Multiplayer.Primitives.asmdef`
- Create: `Assets/Scripts/Modules/Multiplayer/Primitives/Identifiers.cs`
- Create: `Assets/Scripts/Modules/Multiplayer/Primitives/Result.cs`
- Create: `Assets/Tests/EditMode/MultiplayerPrimitives/Multiplayer.Primitives.Tests.asmdef`
- Create: `Assets/Tests/EditMode/MultiplayerPrimitives/IdentifierTests.cs`
- Create: `Assets/Tests/EditMode/MultiplayerPrimitives/ResultTests.cs`

**Interfaces:**
- Consumes: standard library only.
- Produces: stable IDs, `Unit.Value`, `Result.Success`, and `Result.Failure` used by all later plans.

- [ ] **Step 1: Write failing value-semantic tests**

```csharp
[Test]
public void PlayerId_EqualityUsesValue()
{
    var value = Guid.Parse("11111111-1111-1111-1111-111111111111");
    Assert.That(new PlayerId(value), Is.EqualTo(new PlayerId(value)));
    Assert.That(new PlayerId(value).IsValid, Is.True);
    Assert.That(PlayerId.None.IsValid, Is.False);
}

[Test]
public void Result_ExposesOnlyMatchingBranch()
{
    var success = Result<int, string>.Success(7);
    Assert.That(success.IsSuccess, Is.True);
    Assert.That(success.Value, Is.EqualTo(7));
    Assert.Throws<InvalidOperationException>(() => _ = success.Error);
}
```

- [ ] **Step 2: Run the primitive tests and verify missing types**

```powershell
& 'D:\Program Files\Unity\Hub\Editor\6000.3.12f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath . -runTests -testPlatform EditMode -assemblyNames Multiplayer.Primitives.Tests -testResults Temp/multiplayer-primitives.xml -logFile Temp/multiplayer-primitives.log
```

Expected: compilation fails on the new IDs and result type.

- [ ] **Step 3: Implement explicit ID wrappers**

```csharp
public readonly struct PlayerId : IEquatable<PlayerId>
{
    public static readonly PlayerId None = new(Guid.Empty);
    public Guid Value { get; }
    public bool IsValid => Value != Guid.Empty;
    public PlayerId(Guid value) => Value = value;
    public bool Equals(PlayerId other) => Value.Equals(other.Value);
    public override bool Equals(object obj) => obj is PlayerId other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString("N");
}
```

Repeat the complete value semantics for `SessionId`, `MatchId`, `MapId`, and `OperationId`; do not use implicit conversion operators between IDs.

- [ ] **Step 4: Implement the result union**

```csharp
public readonly struct Result<TValue, TError>
{
    private readonly TValue _value;
    private readonly TError _error;
    public bool IsSuccess { get; }
    public TValue Value => IsSuccess ? _value : throw new InvalidOperationException("Failure has no value.");
    public TError Error => !IsSuccess ? _error : throw new InvalidOperationException("Success has no error.");
    private Result(bool success, TValue value, TError error) { IsSuccess = success; _value = value; _error = error; }
    public static Result<TValue, TError> Success(TValue value) => new(true, value, default);
    public static Result<TValue, TError> Failure(TError error) => new(false, default, error);
}

public readonly struct Unit : IEquatable<Unit>
{
    public static readonly Unit Value = new();
    public bool Equals(Unit other) => true;
}
```

- [ ] **Step 5: Run tests and commit**

Run Step 2; expect all tests to pass.

```powershell
git add -- Assets/Scripts/Modules/Multiplayer/Primitives Assets/Tests/EditMode/MultiplayerPrimitives
git commit -m "feat: add multiplayer primitives"
```

## Task 2: Server session state machine

**Files:**
- Create: `Assets/Scripts/Modules/Multiplayer/Session/Multiplayer.Session.asmdef`
- Create: `Assets/Scripts/Modules/Multiplayer/Session/Contracts/IServerSessionFacade.cs`
- Create: `Assets/Scripts/Modules/Multiplayer/Session/Ports/IMatchWorldProvider.cs`
- Create: `Assets/Scripts/Modules/Multiplayer/Session/Ports/IPlayerSpawnProvider.cs`
- Create: `Assets/Scripts/Modules/Multiplayer/Session/Ports/ISessionEventPublisher.cs`
- Create: `Assets/Scripts/Modules/Multiplayer/Session/Ports/ISessionConnectionProvider.cs`
- Create: `Assets/Scripts/Modules/Multiplayer/Session/Ports/ISessionTelemetry.cs`
- Create: `Assets/Scripts/Modules/Multiplayer/Session/Ports/IIdentifierProvider.cs`
- Create: `Assets/Scripts/Modules/Multiplayer/Session/Model/SessionConnection.cs`
- Create: `Assets/Scripts/Modules/Multiplayer/Session/Model/SessionPhase.cs`
- Create: `Assets/Scripts/Modules/Multiplayer/Session/Model/SessionError.cs`
- Create: `Assets/Scripts/Modules/Multiplayer/Session/Model/SessionPlayer.cs`
- Create: `Assets/Scripts/Modules/Multiplayer/Session/Model/SessionSnapshot.cs`
- Create: `Assets/Scripts/Modules/Multiplayer/Session/Model/SessionConfiguration.cs`
- Create: `Assets/Scripts/Modules/Multiplayer/Session/Model/SessionTelemetryEvent.cs`
- Create: `Assets/Scripts/Modules/Multiplayer/Session/Services/SessionCommandQueue.cs`
- Create: `Assets/Scripts/Modules/Multiplayer/Session/Services/ServerSessionFacade.cs`
- Create: `Assets/Scripts/Modules/Multiplayer/Session/Bootstrap/SessionInstaller.cs`
- Create: `Assets/Tests/EditMode/MultiplayerSession/Multiplayer.Session.Tests.asmdef`
- Create: `Assets/Tests/EditMode/MultiplayerSession/Fakes.cs`
- Create: `Assets/Tests/EditMode/MultiplayerSession/ServerSessionFacadeTests.cs`

**Interfaces:**
- Consumes: `Multiplayer.Primitives`.
- Produces: authoritative join/leave/ready state and committed match-load request.

- [ ] **Step 1: Write failing lobby transition tests**

```csharp
[Test]
public async Task TwoPlayers_AllReady_CommitsOneMatchLoad()
{
    var fixture = SessionFixture.Create(maxPlayers: 10, minPlayers: 2);
    var first = (await fixture.Join()).Value;
    var second = (await fixture.Join()).Value;
    await fixture.Service.SetReadyAsync(first, true, default);
    await fixture.Service.SetReadyAsync(second, true, default);

    Assert.That(fixture.Service.Snapshot.Phase, Is.EqualTo(SessionPhase.LoadingMatch));
    Assert.That(fixture.World.LoadRequests, Has.Count.EqualTo(1));
    Assert.That(fixture.Service.Snapshot.MatchId.IsValid, Is.True);
}

[Test]
public async Task ReadyRepeated_DoesNotIncrementRevisionOrStartTwice()
{
    var fixture = await SessionFixture.TwoPlayersInLobby();
    var before = fixture.Service.Snapshot.Revision;
    await fixture.Service.SetReadyAsync(fixture.First, true, default);
    var changed = fixture.Service.Snapshot.Revision;
    await fixture.Service.SetReadyAsync(fixture.First, true, default);
    Assert.That(changed, Is.EqualTo(before + 1));
    Assert.That(fixture.Service.Snapshot.Revision, Is.EqualTo(changed));
}

[Test]
public async Task EleventhJoin_ReturnsSessionFull()
{
    var fixture = SessionFixture.Create(10, 2);
    for (var i = 0; i < 10; i++) Assert.That((await fixture.Join()).IsSuccess, Is.True);
    Assert.That((await fixture.Join()).Error, Is.EqualTo(SessionError.SessionFull));
}
```

- [ ] **Step 2: Run session tests and verify missing session types**

```powershell
& 'D:\Program Files\Unity\Hub\Editor\6000.3.12f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath . -runTests -testPlatform EditMode -assemblyNames Multiplayer.Session.Tests -testResults Temp/multiplayer-session.xml -logFile Temp/multiplayer-session.log
```

Expected: compilation fails because `ServerSessionFacade` and models do not exist.

- [ ] **Step 3: Define the facade and provider contracts exactly**

```csharp
public interface IServerSessionFacade
{
    ValueTask<Result<PlayerId, SessionError>> JoinAsync(SessionConnection connection, CancellationToken token);
    ValueTask<Result<Unit, SessionError>> LeaveAsync(PlayerId playerId, CancellationToken token);
    ValueTask<Result<Unit, SessionError>> SetReadyAsync(PlayerId playerId, bool ready, CancellationToken token);
    ValueTask<Result<Unit, SessionError>> NotifyServerWorldReadyAsync(OperationId operationId, MatchId matchId, CancellationToken token);
    ValueTask<Result<Unit, SessionError>> NotifyPlayerWorldReadyAsync(OperationId operationId, PlayerId playerId, MatchId matchId, CancellationToken token);
    SessionSnapshot Snapshot { get; }
}

public interface IMatchWorldProvider
{
    ValueTask<Result<Unit, SessionError>> LoadAsync(MatchWorldLoadRequest request, CancellationToken token);
    ValueTask CancelAsync(OperationId operationId, CancellationToken token);
}

public interface IPlayerSpawnProvider
{
    ValueTask<Result<SpawnPlayerResult, SessionError>> SpawnAsync(SpawnPlayerRequest request, CancellationToken token);
    ValueTask<Result<Unit, SessionError>> DespawnAsync(PlayerId playerId, CancellationToken token);
}

public interface ISessionEventPublisher
{
    ValueTask PublishSnapshotAsync(SessionSnapshot snapshot, CancellationToken token);
}

public interface ISessionConnectionProvider
{
    ValueTask DisconnectAsync(SessionConnection connection, SessionError reason, CancellationToken token);
}

public interface IIdentifierProvider
{
    Guid NewGuid();
}
```

`SessionConnection` wraps a `Guid` token. It does not wrap `Base.Network.ConnectionId`. `SessionConfiguration` validates `MaxPlayers == 10`, `MinPlayers == 2`, a valid `MapId`, and positive load timeouts.

```csharp
public enum SessionError
{
    SessionFull, UnknownPlayer, InvalidPhase, NotEnoughPlayers,
    WorldLoadFailed, WorldLoadTimeout, PlayerLoadTimeout,
    SpawnFailed, ConnectionClosed
}
```

`ISessionTelemetry.Record(SessionTelemetryEvent value)` receives phase/revision transitions, accepted/rejected commands, active `OperationId`, load outcome, readiness, spawn outcome, and disconnect reason. Its null implementation is used in unit tests; the runtime adapter emits structured Unity log records.

```csharp
public enum SessionTelemetryKind { PhaseChanged, CommandRejected, WorldLoad, PlayerWorldReady, Spawn, Disconnect }

public readonly struct SessionTelemetryEvent
{
    public SessionTelemetryKind Kind { get; }
    public SessionId SessionId { get; }
    public MatchId MatchId { get; }
    public OperationId OperationId { get; }
    public PlayerId PlayerId { get; }
    public SessionPhase Phase { get; }
    public SessionError? Error { get; }
}
```

`SessionInstaller.InstallServer(DiContainer, SessionConfiguration)` binds `SessionCommandQueue` and `IServerSessionFacade -> ServerSessionFacade` after composition has bound the four provider ports. `InstallClient` binds `IClientSessionFacade -> ClientSessionFacade`; the module does not import a runtime-role type.

- [ ] **Step 4: Implement serialized mutation and immutable snapshots**

```csharp
internal sealed class SessionCommandQueue : IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    public async ValueTask<T> ExecuteAsync<T>(Func<ValueTask<T>> command, CancellationToken token)
    {
        await _gate.WaitAsync(token);
        try { return await command(); }
        finally { _gate.Release(); }
    }
    public void Dispose() => _gate.Dispose();
}
```

Inside the gate, copy the player dictionary into a newly ordered `SessionPlayerSnapshot[]` for every changed snapshot. Increment revision only when state changes. Phase derivation is `WaitingForPlayers` below 2 and `Lobby` at 2 or more until load commitment.

- [ ] **Step 5: Implement atomic ready-to-load commitment**

```csharp
private bool CanCommitStart()
    => _phase == SessionPhase.Lobby && _players.Count >= _configuration.MinPlayers &&
       _players.Values.All(player => player.Ready);

private MatchWorldLoadRequest CommitStart()
{
    _phase = SessionPhase.LoadingMatch;
    _matchId = new MatchId(_ids.NewGuid());
    _operationId = new OperationId(_ids.NewGuid());
    _initialParticipants = _players.Keys.ToHashSet();
    PublishChangedSnapshot();
    return new MatchWorldLoadRequest(_operationId, _matchId, _configuration.MapId);
}
```

Invoke `IMatchWorldProvider.LoadAsync` after releasing the mutation gate. Completion enters only through the two notify methods with matching operation and match IDs.

Only `_initialParticipants` determine completion of the initial load. Enter `Playing` when the server world is ready and every still-connected initial participant is `Spawned`; a connection accepted after `CommitStart` is `SessionJoinKind.InProgress`, follows the same readiness/spawn pipeline, but is excluded from that transition condition.

- [ ] **Step 6: Run tests and commit**

Run Step 2; expect all transition, idempotency, and capacity tests to pass.

```powershell
git add -- Assets/Scripts/Modules/Multiplayer/Session Assets/Tests/EditMode/MultiplayerSession
git commit -m "feat: add authoritative session state machine"
```

## Task 3: Load readiness, join in progress, disconnect, and recovery

**Files:**
- Modify: `Assets/Scripts/Modules/Multiplayer/Session/Services/ServerSessionFacade.cs`
- Modify: `Assets/Scripts/Modules/Multiplayer/Session/Model/SessionSnapshot.cs`
- Modify: `Assets/Tests/EditMode/MultiplayerSession/ServerSessionFacadeTests.cs`

**Interfaces:**
- Consumes: world/spawn/connection ports from Task 2.
- Produces: stale-callback protection and complete lifecycle policy.

- [ ] **Step 1: Add failing lifecycle tests**

```csharp
[Test]
public async Task StaleWorldReady_DoesNotAdvanceReplacementOperation()
{
    var fixture = await SessionFixture.LoadingMatch();
    var staleOperation = fixture.LoadOperation;
    await fixture.DisconnectUntilStartCancels();
    await fixture.StartAgain();
    var result = await fixture.Service.NotifyServerWorldReadyAsync(staleOperation, fixture.FirstMatchId, default);
    Assert.That(result.Error, Is.EqualTo(SessionError.InvalidPhase));
    Assert.That(fixture.ServerWorldReady, Is.False);
}

[Test]
public async Task JoinDuringPlaying_IsJipAndDoesNotChangeReadyCondition()
{
    var fixture = await SessionFixture.Playing();
    var joined = await fixture.Join();
    Assert.That(joined.IsSuccess, Is.True);
    Assert.That(fixture.Service.Snapshot.Phase, Is.EqualTo(SessionPhase.Playing));
    Assert.That(fixture.Service.Snapshot.Player(joined.Value).JoinKind, Is.EqualTo(SessionJoinKind.InProgress));
}

[Test]
public async Task DisconnectDuringInitialLoadBelowTwo_CancelsAndResetsReady()
{
    var fixture = await SessionFixture.LoadingMatch();
    await fixture.Service.LeaveAsync(fixture.Second, default);
    Assert.That(fixture.World.CancelledOperation, Is.EqualTo(fixture.LoadOperation));
    Assert.That(fixture.Service.Snapshot.Phase, Is.EqualTo(SessionPhase.WaitingForPlayers));
    Assert.That(fixture.Service.Snapshot.Players.All(x => !x.Ready), Is.True);
}
```

- [ ] **Step 2: Run session tests and verify lifecycle failures**

Run Task 2 Step 2. Expected: the new tests fail on missing join/readiness/recovery behavior.

- [ ] **Step 3: Implement readiness and idempotent spawn requests**

```csharp
private async ValueTask TrySpawnAsync(PlayerId playerId, CancellationToken token)
{
    var prepared = await _queue.ExecuteAsync(() =>
    {
        if (!_serverWorldReady || !_players.TryGetValue(playerId, out var player) ||
            !player.WorldReady || player.SpawnState is SpawnState.Spawning or SpawnState.Spawned)
            return ValueTask.FromResult((Ready: false, Request: default(SpawnPlayerRequest)));
        player.SpawnState = SpawnState.Spawning;
        var request = new SpawnPlayerRequest(_operationId, _matchId, playerId, player.Connection);
        PublishChangedSnapshot();
        return ValueTask.FromResult((Ready: true, Request: request));
    }, token);
    if (!prepared.Ready) return;
    var result = await _spawnProvider.SpawnAsync(prepared.Request, token);
    await ApplySpawnResultAsync(prepared.Request, result, token);
}
```

`NotifyServerWorldReadyAsync` tries every already-world-ready player; `NotifyPlayerWorldReadyAsync` tries that player. Duplicate notifications do not create duplicate requests.

Arm operation-scoped timeouts without allowing stale tasks to mutate a replacement operation:

```csharp
private async Task EnforcePlayerLoadTimeoutAsync(PlayerId playerId, OperationId operationId,
    SessionConnection connection, CancellationToken token)
{
    await Task.Delay(_configuration.PlayerLoadTimeout, token);
    var shouldDisconnect = await _queue.ExecuteAsync(() => ValueTask.FromResult(
        _operationId == operationId && _players.TryGetValue(playerId, out var player) && !player.WorldReady), token);
    if (shouldDisconnect)
        await _connectionProvider.DisconnectAsync(connection, SessionError.PlayerLoadTimeout, token);
}
```

Use an operation-owned `CancellationTokenSource`: `CancelAfter(WorldLoadTimeout)` for initial load, cancel it on load success/cancellation, and create a separate player timeout source for every JIP player. Disconnect/leave cancels that player's source.

- [ ] **Step 4: Implement failure policy**

```text
initial LoadAsync failure or timeout -> cancel active operation, reset ready flags, Lobby/WaitingForPlayers
disconnect during LoadingMatch leaving fewer than 2 -> cancel active operation and reset ready flags
disconnect during Playing -> despawn only that player and keep Playing
JIP player load timeout -> disconnect only that SessionConnection with PlayerLoadTimeout
stale OperationId/MatchId -> return InvalidPhase, do not mutate state
```

Model these outcomes in explicit branches covered by the tests; do not catch and discard exceptions from programming/configuration defects.

- [ ] **Step 5: Run tests and commit**

Run Task 2 Step 2; expect zero failures.

```powershell
git add -- Assets/Scripts/Modules/Multiplayer/Session Assets/Tests/EditMode/MultiplayerSession
git commit -m "feat: complete session lifecycle recovery"
```

## Task 4: Client session projection

**Files:**
- Create: `Assets/Scripts/Modules/Multiplayer/Session/Contracts/IClientSessionFacade.cs`
- Create: `Assets/Scripts/Modules/Multiplayer/Session/Ports/IClientSessionCommandPublisher.cs`
- Create: `Assets/Scripts/Modules/Multiplayer/Session/Services/ClientSessionFacade.cs`
- Create: `Assets/Tests/EditMode/MultiplayerSession/ClientSessionFacadeTests.cs`

**Interfaces:**
- Consumes: `IClientSessionCommandPublisher` declared in the client session module.
- Produces: observable snapshot projection and ready facade.

- [ ] **Step 1: Write failing revision tests**

```csharp
[Test]
public void ApplySnapshot_IgnoresOlderOrEqualRevision()
{
    var client = CreateClient();
    client.ApplySnapshot(Snapshot("session-a", revision: 5));
    client.ApplySnapshot(Snapshot("session-a", revision: 4));
    client.ApplySnapshot(Snapshot("session-a", revision: 5));
    Assert.That(client.Snapshot.Revision, Is.EqualTo(5));
    Assert.That(client.ChangeCount, Is.EqualTo(1));
}

[Test]
public void ApplySnapshot_NewSessionAcceptsLowerRevision()
{
    var client = CreateClient();
    client.ApplySnapshot(Snapshot("session-a", 9));
    client.ApplySnapshot(Snapshot("session-b", 1));
    Assert.That(client.Snapshot.SessionId, Is.EqualTo(Session("session-b")));
}
```

- [ ] **Step 2: Run tests and confirm failure**

Run Task 2 Step 2. Expected: client facade types are missing.

- [ ] **Step 3: Implement the client facade**

```csharp
public void ApplySnapshot(SessionSnapshot snapshot)
{
    if (snapshot.SessionId == _snapshot.SessionId && snapshot.Revision <= _snapshot.Revision)
        return;
    _snapshot = snapshot;
    SnapshotChanged?.Invoke(snapshot);
}

public ValueTask<Result<Unit, SessionError>> SetReadyAsync(bool ready, CancellationToken token)
    => _commandPublisher.SetReadyAsync(ready, token);
```

The client module declares the exact port below; the network adapter implements it in Task 6.

```csharp
public interface IClientSessionCommandPublisher
{
    ValueTask<Result<Unit, SessionError>> SetReadyAsync(bool ready, CancellationToken token);
}
```

- [ ] **Step 4: Run tests and commit**

```powershell
git add -- Assets/Scripts/Modules/Multiplayer/Session Assets/Tests/EditMode/MultiplayerSession
git commit -m "feat: add client session projection"
```

## Task 5: Session wire DTOs and mapping

**Files:**
- Create: `Assets/Scripts/Modules/Multiplayer/SessionNetworking/Multiplayer.Session.Networking.asmdef`
- Create: `Assets/Scripts/Modules/Multiplayer/SessionNetworking/Data/SessionMessageTypeIds.cs`
- Create: `Assets/Scripts/Modules/Multiplayer/SessionNetworking/Data/SetReadyMessage.cs`
- Create: `Assets/Scripts/Modules/Multiplayer/SessionNetworking/Data/JoinAcceptedMessage.cs`
- Create: `Assets/Scripts/Modules/Multiplayer/SessionNetworking/Data/SessionSnapshotMessage.cs`
- Create: `Assets/Scripts/Modules/Multiplayer/SessionNetworking/Data/SessionCommandRejectedMessage.cs`
- Create: `Assets/Scripts/Modules/Multiplayer/SessionNetworking/Mapping/SessionDtoMapper.cs`
- Create: `Assets/Tests/EditMode/MultiplayerSessionNetworking/Multiplayer.Session.Networking.Tests.asmdef`
- Create: `Assets/Tests/EditMode/MultiplayerSessionNetworking/SessionNetworkingFixtures.cs`
- Create: `Assets/Tests/EditMode/MultiplayerSessionNetworking/SessionDtoMapperTests.cs`

**Interfaces:**
- Consumes: session models and `Base.Network.IMessagePayload`.
- Produces: stable MessagePack DTOs with reserved IDs 1000-1003.

- [ ] **Step 1: Write failing round-trip and identity tests**

```csharp
[Test]
public void SetReadyMessage_ContainsOnlyReadyFlag()
{
    var keys = typeof(SetReadyMessage).GetProperties().Select(x => x.Name).ToArray();
    Assert.That(keys, Is.EqualTo(new[] { "Ready" }));
}

[Test]
public void Snapshot_RoundTripsAllAuthoritativeFields()
{
    var expected = SessionSnapshots.PlayingWithThreePlayers(revision: 17);
    var actual = SessionDtoMapper.ToDomain(SessionDtoMapper.ToDto(expected));
    Assert.That(actual, Is.EqualTo(expected));
}
```

- [ ] **Step 2: Run networking tests and verify missing DTOs**

```powershell
& 'D:\Program Files\Unity\Hub\Editor\6000.3.12f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath . -runTests -testPlatform EditMode -assemblyNames Multiplayer.Session.Networking.Tests -testResults Temp/session-networking.xml -logFile Temp/session-networking.log
```

Expected: compilation fails on DTOs and mapper.

- [ ] **Step 3: Implement MessagePack DTOs with fixed numeric keys**

```csharp
public static class SessionMessageTypeIds
{
    public const ushort SetReady = 1000;
    public const ushort JoinAccepted = 1001;
    public const ushort Snapshot = 1002;
    public const ushort CommandRejected = 1003;
}

[MessagePackObject]
public struct SetReadyMessage : IMessagePayload
{
    [Key(0)] public bool Ready { get; set; }
}
```

Use string-form GUIDs for ID DTO fields, numeric enums for phases/errors, and arrays for player snapshots. Mapper validation rejects malformed GUIDs and duplicate players with `InvalidDataException`; domain assemblies remain unaware of MessagePack.

- [ ] **Step 4: Run tests and commit**

Run Step 2; expect all mapper tests to pass.

```powershell
git add -- Assets/Scripts/Modules/Multiplayer/SessionNetworking Assets/Tests/EditMode/MultiplayerSessionNetworking
git commit -m "feat: add session network contracts"
```

## Task 6: Base.Network session bridges

**Files:**
- Create: `Assets/Scripts/Modules/Multiplayer/SessionNetworking/Runtime/SessionConnectionRegistry.cs`
- Create: `Assets/Scripts/Modules/Multiplayer/SessionNetworking/Runtime/ServerSessionNetworkBridge.cs`
- Create: `Assets/Scripts/Modules/Multiplayer/SessionNetworking/Runtime/ClientSessionNetworkBridge.cs`
- Create: `Assets/Scripts/Modules/Multiplayer/SessionNetworking/Runtime/SetReadyMessageHandler.cs`
- Create: `Assets/Scripts/Modules/Multiplayer/SessionNetworking/Runtime/SessionSnapshotMessageHandler.cs`
- Create: `Assets/Scripts/Modules/Multiplayer/SessionNetworking/Runtime/JoinAcceptedMessageHandler.cs`
- Create: `Assets/Scripts/Modules/Multiplayer/SessionNetworking/Runtime/SessionCommandRejectedMessageHandler.cs`
- Create: `Assets/Scripts/Modules/Multiplayer/SessionNetworking/Runtime/SessionStructuredLogger.cs`
- Create: `Assets/Scripts/Modules/Multiplayer/SessionNetworking/Bootstrap/SessionNetworkInstaller.cs`
- Create: `Assets/Tests/EditMode/MultiplayerSessionNetworking/SessionNetworkBridgeTests.cs`

**Interfaces:**
- Consumes: `INetworkServer`, `ClientMessenger`, `ServerMessenger`, handler interfaces, session facades.
- Produces: connection-derived player identity and reliable snapshots.

- [ ] **Step 1: Write failing sender-identity and disconnect tests**

```csharp
[Test]
public async Task ReadyHandler_UsesConnectionRegistry_NotPayloadIdentity()
{
    var player = Player("known-player");
    var registry = Registry.With(new ConnectionId(42), player);
    var handler = new SetReadyMessageHandler(registry, _serverSession, _publisher);
    await handler.HandleAsync(new SetReadyMessage { Ready = true }, new ConnectionId(42), Context(42));
    Assert.That(_serverSession.LastReadyPlayer, Is.EqualTo(player));
}

[Test]
public void Disconnect_RemovesBothConnectionAndPlayerMappingBeforeLeave()
{
    _bridge.OnDisconnected(new ConnectionId(42), DisconnectReason.Timeout);
    Assert.That(_registry.TryGetPlayer(new ConnectionId(42), out _), Is.False);
}
```

- [ ] **Step 2: Run networking tests and verify bridge failures**

Run Task 5 Step 2. Expected: new tests fail because bridges do not exist.

- [ ] **Step 3: Implement the bidirectional neutral registry**

```csharp
public SessionConnection Register(ConnectionId connectionId)
{
    if (_byConnection.ContainsKey(connectionId.Value)) throw new InvalidOperationException("Connection already registered.");
    var sessionConnection = new SessionConnection(Guid.NewGuid());
    _byConnection.Add(connectionId.Value, sessionConnection);
    _bySession.Add(sessionConnection, connectionId);
    return sessionConnection;
}

public void AttachPlayer(SessionConnection connection, PlayerId playerId)
{
    _playerBySession.Add(connection, playerId);
    _sessionByPlayer.Add(playerId, connection);
}
```

Unregister returns the previous player if present and removes all four indices atomically.

- [ ] **Step 4: Implement server bridge lifecycle**

Subscribe to `INetworkServer.OnConnected` and `OnDisconnected`. On connect, register the neutral token, call `JoinAsync`, attach the returned player, send `JoinAcceptedMessage`, then send current snapshot. On capacity failure, send rejection and call `IConnection.DisconnectAsync(ClosedByServer)`. On disconnect, unregister first and call `LeaveAsync` idempotently.

```csharp
private async UniTask HandleConnectedAsync(IConnection connection)
{
    var token = _registry.Register(connection.Id);
    var joined = await _session.JoinAsync(token, _lifetime.Token);
    if (!joined.IsSuccess) { await RejectAndDisconnect(connection, joined.Error); return; }
    _registry.AttachPlayer(token, joined.Value);
    await _messenger.To(connection.Id, _mapper.JoinAccepted(joined.Value, _session.Snapshot));
    await _messenger.To(connection.Id, _mapper.ToDto(_session.Snapshot));
}
```

- [ ] **Step 5: Implement client handlers and ready publisher**

`ClientSessionNetworkBridge.SetReadyAsync` sends only `new SetReadyMessage { Ready = ready }`. Snapshot handler maps then calls `IClientSessionFacade.ApplySnapshot`; JoinAccepted stores local `PlayerId`; rejection handler exposes a typed last error/event for UI and automation.

- [ ] **Step 6: Register fixed message IDs in one installer**

```csharp
container.RegisterGlobalMessageType<SetReadyMessage>(SessionMessageTypeIds.SetReady);
container.RegisterGlobalMessageType<JoinAcceptedMessage>(SessionMessageTypeIds.JoinAccepted);
container.RegisterGlobalMessageType<SessionSnapshotMessage>(SessionMessageTypeIds.Snapshot);
container.RegisterGlobalMessageType<SessionCommandRejectedMessage>(SessionMessageTypeIds.CommandRejected);
```

Register server or client handlers according to immutable startup role; never inspect `FishNetGeneralSettings.IsServer` inside the session module.

Bind `ISessionTelemetry -> SessionStructuredLogger` on the server. Emit one JSON object per event with session/match/operation/player IDs, revision, phase, kind, typed error, and UTC timestamp; never use a GameObject name as identity.

- [ ] **Step 7: Run tests and commit**

Run Task 5 Step 2; expect all bridge and mapper tests to pass.

```powershell
git add -- Assets/Scripts/Modules/Multiplayer/SessionNetworking Assets/Tests/EditMode/MultiplayerSessionNetworking
git commit -m "feat: connect session lobby to base network"
```

## Task 7: Session/lobby acceptance gate

**Files:**
- Create: `docs/testing/multiplayer-foundation-plan-02.md`

**Interfaces:**
- Consumes: all Plan 02 assemblies.
- Produces: repeatable lobby verification with fakes and loopback adapters.

- [ ] **Step 1: Run all Plan 02 test assemblies**

```powershell
& 'D:\Program Files\Unity\Hub\Editor\6000.3.12f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath . -runTests -testPlatform EditMode -assemblyNames Multiplayer.Primitives.Tests,Multiplayer.Session.Tests,Multiplayer.Session.Networking.Tests -testResults Temp/plan-02-editmode.xml -logFile Temp/plan-02-editmode.log
```

Expected: zero failures.

- [ ] **Step 2: Verify forbidden references**

Extend the architecture test to assert:

```text
Multiplayer.Primitives -> no FishNet.Runtime, Network, MessagePack
Multiplayer.Session -> no FishNet.Runtime, Network, MessagePack
Multiplayer.Session.Networking -> may reference Network and MessagePack, not FishNet.Runtime
```

Run `Multiplayer.Session.Tests`; expected: pass.

- [ ] **Step 3: Record the acceptance evidence and commit**

Document test counts plus observed assertions for capacity 10, minimum 2, all-ready, idempotent ready, stale operation, JIP, disconnect, and revision ordering.

```powershell
git add -- docs/testing/multiplayer-foundation-plan-02.md Assets/Tests/EditMode/MultiplayerSession/ArchitectureBoundaryTests.cs
git commit -m "test: verify session lobby boundaries"
```
