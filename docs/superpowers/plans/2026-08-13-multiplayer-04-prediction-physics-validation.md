# FishNet Prediction and Physics Validation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add server-authoritative FishNet character prediction/reconciliation, player collisions, dynamic-rigidbody interaction, presentation smoothing, and multi-process acceptance tests.

**Architecture:** A FishNet `TickNetworkBehaviour` maps replicate/reconcile structs to the pure character service from Plan 01. The physics root is corrected authoritatively while a separate graphical anchor and local-only camera smooth presentation; predicted physical callbacks are deduplicated before effects.

**Tech Stack:** Unity 6000.3.12f1, FishNet 4.6.12 local source, Unity Physics, Base.Network for discrete events only, NUnit/Unity Test Framework 1.6.0, Zenject 9.3.1.

## Global Constraints

- The owning client sends clamped movement, view-yaw intent, and jump flags only.
- Position, rotation, velocity, grounded state, gravity result, and surface normal never come from client payloads.
- Server physics and final state are authoritative; owner prediction is corrected and unacknowledged input is replayed.
- Network physics uses FishNet `TimeManager` at a configurable 50 Hz baseline.
- `PredictionRigidbody.Simulate()` is called exactly once per predicted body per replicate step.
- Reconcile includes Rigidbody state and every non-Rigidbody state that affects the next step.
- Player `NetworkObject` prediction and state forwarding are enabled for the initial 10-player target.
- Static gravity sources are not synchronized; dynamic rigidbodies are not gravity sources.
- No client-authoritative `NetworkTransform` controls a physics root.
- Preserve unrelated working-tree changes and commit after every task.

---

## File structure

```text
Assets/Scripts/Modules/FishNetworking/Gameplay/Prediction/
  CharacterReplicateData.cs
  CharacterReconcileData.cs
  PredictedCharacterNetworkBehaviour.cs
  NetworkCharacterInstaller.cs
  PredictionInputValidator.cs
  PredictionMetrics.cs

Assets/Scripts/Modules/FishNetworking/Gameplay/Physics/
  PredictedDynamicRigidbodyBehaviour.cs
  DynamicBodyForceCommand.cs
  ConfirmedCollisionEventGate.cs
  PredictionCollisionRelay.cs

Assets/Scripts/Modules/Client/Presentation/
  Client.Presentation.asmdef
  LocalPlayerPresentation.cs
  NetworkCharacterInputSource.cs
  ReconciliationPresentationSmoother.cs

Assets/Tests/EditMode/FishNetworkingGameplay/
  PredictionInputValidatorTests.cs
  PredictionStateMappingTests.cs
  ConfirmedCollisionEventGateTests.cs

Assets/Tests/PlayMode/Prediction/
  Multiplayer.Prediction.Tests.asmdef
  PredictionFixture.cs
  PredictedCharacterTests.cs
  DynamicRigidbodyPredictionTests.cs
  ReconciliationCameraTests.cs

Assets/Tests/Runtime/MultiplayerSoak/
  MultiplayerSoakClient.cs
  MultiplayerSoakReporter.cs

Assets/Scripts/Modules/FishNetworking/Gameplay/Diagnostics/
  FishNetNetworkImpairmentController.cs

Assets/Prefabs/Player/NetworkPlayer.prefab
Assets/Prefabs/Network/PredictedDynamicBox.prefab
Assets/Scenes/MultiplayerPlayground.unity
```

## Task 1: Replicate/reconcile data and input validation

**Files:**
- Create: `Assets/Scripts/Modules/FishNetworking/Gameplay/Prediction/CharacterReplicateData.cs`
- Create: `Assets/Scripts/Modules/FishNetworking/Gameplay/Prediction/CharacterReconcileData.cs`
- Create: `Assets/Scripts/Modules/FishNetworking/Gameplay/Prediction/PredictionInputValidator.cs`
- Create: `Assets/Tests/EditMode/FishNetworkingGameplay/PredictionInputValidatorTests.cs`
- Create: `Assets/Tests/EditMode/FishNetworkingGameplay/PredictionStateMappingTests.cs`

**Interfaces:**
- Consumes: `CharacterInput`, `CharacterSimulationState`, FishNet `IReplicateData`, `IReconcileData`, `PredictionRigidbody`.
- Produces: exact unreliable prediction payloads and validated server input.

- [ ] **Step 1: Write failing validation and round-trip tests**

```csharp
[Test]
public void Validate_ClampsMoveAndYawDelta()
{
    var validator = new PredictionInputValidator(maxYawDegreesPerTick: 12f);
    var result = validator.Validate(new CharacterReplicateData(new Vector2(4f, -3f), 90f, true, true), previousYaw: 10f);
    Assert.That(result.Move.magnitude, Is.EqualTo(1f).Within(0.0001f));
    Assert.That(Mathf.DeltaAngle(10f, result.ViewYaw), Is.EqualTo(12f).Within(0.0001f));
}

[TestCase(float.NaN)]
[TestCase(float.PositiveInfinity)]
public void Validate_NonFiniteYaw_ReusesPreviousYaw(float invalid)
{
    var result = Validator.Validate(new CharacterReplicateData(Vector2.zero, invalid, false, false), 25f);
    Assert.That(result.ViewYaw, Is.EqualTo(25f));
}

[Test]
public void ReconcileState_ContainsAllMotorState()
{
    var state = States.HoldingJumpOn("planet-sphere", elapsed: 0.08f, yaw: 37f);
    var data = CharacterReconcileData.Create(PredictionBody, state);
    Assert.That(data.ToSimulationState(), Is.EqualTo(state));
}
```

- [ ] **Step 2: Run FishNetworking gameplay tests and verify missing prediction data**

```powershell
& 'D:\Program Files\Unity\Hub\Editor\6000.3.12f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath . -runTests -testPlatform EditMode -assemblyNames FishNetworking.Gameplay.Tests -testResults Temp/fish-gameplay-prediction.xml -logFile Temp/fish-gameplay-prediction.log
```

- [ ] **Step 3: Implement FishNet prediction structs**

```csharp
public struct CharacterReplicateData : IReplicateData
{
    public Vector2 Move;
    public float ViewYaw;
    public bool JumpPressed;
    public bool JumpHeld;
    private uint _tick;
    public CharacterReplicateData(Vector2 move, float viewYaw, bool jumpPressed, bool jumpHeld)
    { Move = move; ViewYaw = viewYaw; JumpPressed = jumpPressed; JumpHeld = jumpHeld; _tick = 0; }
    public uint GetTick() => _tick;
    public void SetTick(uint value) => _tick = value;
    public void Dispose() { }
}

public struct CharacterReconcileData : IReconcileData
{
    public PredictionRigidbody Body;
    public string SurfaceId;
    public Vector3 SmoothedUp;
    public byte JumpPhase;
    public float JumpElapsed;
    public float ViewYaw;
    private uint _tick;
    public uint GetTick() => _tick;
    public void SetTick(uint value) => _tick = value;
    public void Dispose() { }
}
```

Provide total `Create` and `ToSimulationState` mappings. Invalid enum bytes throw `InvalidDataException`; an empty surface ID maps to `SurfaceId.None`.

- [ ] **Step 4: Implement server input validation**

Normalize nonfinite movement to zero, clamp its magnitude to 1, normalize yaw to `[0, 360)`, and clamp its shortest delta from the previous accepted yaw. Jump booleans pass through; a second `JumpPressed` while jump is already active has no effect in the pure simulation.

- [ ] **Step 5: Run tests and commit**

```powershell
git add -- Assets/Scripts/Modules/FishNetworking/Gameplay/Prediction Assets/Tests/EditMode/FishNetworkingGameplay
git commit -m "feat: add character prediction data contracts"
```

## Task 2: Predicted character NetworkBehaviour

**Files:**
- Create: `Assets/Scripts/Modules/FishNetworking/Gameplay/Prediction/PredictedCharacterNetworkBehaviour.cs`
- Create: `Assets/Scripts/Modules/FishNetworking/Gameplay/Prediction/NetworkCharacterInstaller.cs`
- Create: `Assets/Scripts/Modules/FishNetworking/Gameplay/Prediction/PredictionMetrics.cs`
- Create: `Assets/Tests/PlayMode/Prediction/Multiplayer.Prediction.Tests.asmdef`
- Create: `Assets/Tests/PlayMode/Prediction/PredictionFixture.cs`
- Create: `Assets/Tests/PlayMode/Prediction/PredictedCharacterTests.cs`

**Interfaces:**
- Consumes: pure character service, gravity adapter, ground probe, local input source, FishNet prediction lifecycle.
- Produces: owner prediction, server authority, and replay through one replicate method.

- [ ] **Step 1: Write a failing PlayMode tick/reconcile test**

```csharp
[UnityTest]
public IEnumerator OwnerTick_SimulatesOnce_AndServerReconcileRestoresMotorState()
{
    var fixture = yield return PredictionFixture.StartOwnerAndServer();
    fixture.OwnerInput.Set(new CharacterInput(Vector2.up, 15f, true, true));
    yield return fixture.AdvanceTicks(4);
    Assert.That(fixture.OwnerDriver.SimulateCallsPerTick, Is.All.EqualTo(1));
    fixture.ForceOwnerDivergence(positionOffset: Vector3.right * 3f, surfaceId: "wrong-surface");
    yield return fixture.AdvanceTicks(8);
    Assert.That(fixture.OwnerPosition, Is.EqualTo(fixture.ServerPosition).Using(Vector3ComparerWithEqualsOperator.Instance));
    Assert.That(fixture.OwnerState.Gravity.ActiveSurface, Is.EqualTo(fixture.ServerState.Gravity.ActiveSurface));
}
```

`PredictionFixture` builds an isolated physics scene with paired owner/server bodies, exposes deterministic `AdvanceTicks(int)`, records calls through a body-driver spy, and provides `ForceOwnerDivergence`, `WithTwoOwnersFacingEachOther`, and `WithDynamicBox`. `States` is a fixture helper that constructs complete `CharacterSimulationState` values from literal surface/jump/yaw inputs.

- [ ] **Step 2: Run PlayMode tests and verify missing behaviour**

```powershell
& 'D:\Program Files\Unity\Hub\Editor\6000.3.12f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath . -runTests -testPlatform PlayMode -assemblyNames Multiplayer.Prediction.Tests -testResults Temp/prediction-playmode.xml -logFile Temp/prediction-playmode.log
```

- [ ] **Step 3: Initialize prediction and callbacks exactly once**

```csharp
private readonly PredictionRigidbody _predictionBody = new();

private void Awake()
{
    _rigidbody.useGravity = false;
    _predictionBody.Initialize(_rigidbody);
}

public override void OnStartNetwork()
{
    SetTickCallbacks(TickCallback.Tick | TickCallback.PostTick);
}

protected override void TimeManager_OnTick()
{
    PerformReplicate(BuildReplicateData());
}

protected override void TimeManager_OnPostTick()
{
    CreateReconcile();
}
```

`BuildReplicateData` returns default unless `IsOwner`; only the owner consumes the render-frame input buffer.

- [ ] **Step 4: Implement the single predicted simulation path**

```csharp
[Replicate]
private void PerformReplicate(CharacterReplicateData data,
    ReplicateState replicateState = ReplicateState.Invalid, Channel channel = Channel.Unreliable)
{
    var input = IsServerStarted ? _validator.Validate(data, _state.ViewYaw) : data.ToCharacterInput();
    var up = _state.Gravity.SmoothedUp.sqrMagnitude > 0.5f ? _state.Gravity.SmoothedUp : transform.up;
    var grounded = _groundProbe.IsGrounded(_rigidbody.position, up, out var groundNormal);
    var snapshot = new CharacterBodySnapshot(_rigidbody.position, _rigidbody.rotation,
        _rigidbody.linearVelocity, grounded, groundNormal);
    var result = _simulation.Simulate(input, snapshot, _state, (float)TimeManager.TickDelta);
    _predictionBody.Velocity(result.LinearVelocity);
    _predictionBody.AddForce(result.Acceleration, ForceMode.Acceleration);
    _rigidbody.MoveRotation(Quaternion.RotateTowards(_rigidbody.rotation, result.TargetRotation,
        _settings.MaxRotationDegreesPerTick));
    _predictionBody.Simulate();
    _state = result.State;
}
```

The authoritative server path always validates. Client replay uses the already recorded input. No `FixedUpdate` exists on this behaviour or any enabled network-player movement component.

- [ ] **Step 5: Reconcile complete physics and motor state**

```csharp
public override void CreateReconcile()
{
    PerformReconcile(CharacterReconcileData.Create(_predictionBody, _state));
}

[Reconcile]
private void PerformReconcile(CharacterReconcileData data, Channel channel = Channel.Unreliable)
{
    _predictionBody.Reconcile(data.Body);
    _state = data.ToSimulationState();
}
```

Track pre/post reconcile position and angle in `PredictionMetrics`; emit structured logs only when configured development thresholds are exceeded.

- [ ] **Step 6: Bind spawned-player dependencies through `GameObjectContext`**

`NetworkCharacterInstaller` binds serialized movement/gravity settings, the local `Rigidbody`, ground probe, character simulation, and `PredictedCharacterNetworkBehaviour`. Client input is optional and resolved only for `IsOwner`; server/spectator instances use a null input source.

- [ ] **Step 7: Run tests and commit**

```powershell
git add -- Assets/Scripts/Modules/FishNetworking/Gameplay/Prediction Assets/Tests/PlayMode/Prediction
git commit -m "feat: add predicted surface gravity character"
```

## Task 3: Owner-only input, camera, and presentation smoothing

**Files:**
- Create: `Assets/Scripts/Modules/Client/Presentation/Client.Presentation.asmdef`
- Create: `Assets/Scripts/Modules/Client/Presentation/NetworkCharacterInputSource.cs`
- Create: `Assets/Scripts/Modules/Client/Presentation/LocalPlayerPresentation.cs`
- Create: `Assets/Scripts/Modules/Client/Presentation/ReconciliationPresentationSmoother.cs`
- Create: `Assets/Tests/PlayMode/Prediction/ReconciliationCameraTests.cs`
- Modify: `Assets/Prefabs/Player/NetworkPlayer.prefab`
- Modify: `Assets/Prefabs/Player/CameraRoot.prefab`

**Interfaces:**
- Consumes: ownership callbacks and `ICharacterInputSource`.
- Produces: local-only input/camera/audio and a smoothed graphics anchor.

- [ ] **Step 1: Write failing ownership and correction-smoothing tests**

```csharp
[UnityTest]
public IEnumerator NonOwner_HasNoEnabledInputCameraOrAudioListener()
{
    var remote = yield return Fixture.SpawnRemotePlayer();
    Assert.That(remote.Input.enabled, Is.False);
    Assert.That(remote.Camera.enabled, Is.False);
    Assert.That(remote.AudioListener.enabled, Is.False);
}

[UnityTest]
public IEnumerator SmallCorrection_SmoothsGraphicsButNotPhysicsRoot()
{
    var owner = yield return Fixture.SpawnOwner();
    owner.ApplyAuthoritativeCorrection(Vector3.right * 0.25f);
    Assert.That(owner.PhysicsRoot.position, Is.EqualTo(owner.AuthoritativePosition));
    Assert.That(owner.Graphics.position, Is.Not.EqualTo(owner.PhysicsRoot.position));
    yield return Fixture.RenderFrames(20);
    Assert.That(Vector3.Distance(owner.Graphics.position, owner.PhysicsRoot.position), Is.LessThan(0.01f));
}
```

- [ ] **Step 2: Run prediction PlayMode tests and verify failure**

Run Task 2 Step 2.

- [ ] **Step 3: Implement the owner-only presentation switch**

```csharp
public override void OnStartClient()
{
    var local = IsOwner;
    _input.enabled = local;
    _camera.enabled = local;
    _audioListener.enabled = local;
    _hudRoot.SetActive(local);
}
```

Dedicated server builds never instantiate the camera root. Remote players keep renderers and collision but no input, HUD, camera, or listener.

- [ ] **Step 4: Smooth the detached graphical anchor**

Before a correction, store graphics world pose; after correction, accumulate the delta offset. In `LateUpdate`, decay offsets with exponential sharpness. If correction distance exceeds 2 m or angle exceeds 45 degrees, clear offsets immediately.

```csharp
var alpha = 1f - Mathf.Exp(-_sharpness * Time.deltaTime);
_positionOffset = Vector3.Lerp(_positionOffset, Vector3.zero, alpha);
_rotationOffset = Quaternion.Slerp(_rotationOffset, Quaternion.identity, alpha);
_graphics.SetPositionAndRotation(_physicsRoot.position + _positionOffset,
    _rotationOffset * _physicsRoot.rotation);
```

The camera reads the graphics/presentation anchor in its existing `LateUpdate`.

- [ ] **Step 5: Configure the NetworkPlayer hierarchy**

```text
NetworkPlayer (NetworkObject + Rigidbody + CapsuleCollider + PredictedCharacterNetworkBehaviour)
  Graphics (detached/smoothed NetworkObject graphical object)
  LocalPresentation (camera-root spawn point, input, HUD hooks; owner only)
```

Set NetworkObject `Enable Prediction = true`, prediction type Rigidbody, `Enable State Forwarding = true`, graphical object `Graphics`, owner interpolation enabled, teleport threshold 2 m. Remove/disable `OfflineCharacterController` on the network prefab. Do not add a client-authoritative NetworkTransform.

- [ ] **Step 6: Run tests and commit**

```powershell
git add -- Assets/Scripts/Modules/Client/Presentation Assets/Tests/PlayMode/Prediction/ReconciliationCameraTests.cs Assets/Prefabs/Player/NetworkPlayer.prefab Assets/Prefabs/Player/CameraRoot.prefab
git commit -m "feat: add local predicted player presentation"
```

## Task 4: Predicted dynamic rigidbodies and physical collisions

**Files:**
- Create: `Assets/Scripts/Modules/FishNetworking/Gameplay/Physics/DynamicBodyForceCommand.cs`
- Create: `Assets/Scripts/Modules/FishNetworking/Gameplay/Physics/PredictedDynamicRigidbodyBehaviour.cs`
- Create: `Assets/Scripts/Modules/FishNetworking/Gameplay/Physics/ConfirmedCollisionEventGate.cs`
- Create: `Assets/Scripts/Modules/FishNetworking/Gameplay/Physics/PredictionCollisionRelay.cs`
- Create: `Assets/Tests/EditMode/FishNetworkingGameplay/ConfirmedCollisionEventGateTests.cs`
- Create: `Assets/Tests/PlayMode/Prediction/DynamicRigidbodyPredictionTests.cs`
- Create: `Assets/Prefabs/Network/PredictedDynamicBox.prefab`
- Modify: `Assets/DefaultPrefabObjects.asset`
- Modify: `Assets/Scenes/MultiplayerPlayground.unity`

**Interfaces:**
- Consumes: FishNet prediction-rigidbody and `NetworkCollision` APIs.
- Produces: server-authoritative pushable boxes and deduplicated physical events.

- [ ] **Step 1: Write failing collision convergence and event-gate tests**

```csharp
[Test]
public void ConfirmedGate_EmitsOneEventForRepeatedReplayTick()
{
    var gate = new ConfirmedCollisionEventGate();
    Assert.That(gate.TryConfirm(objectId: 12, otherId: 40, tick: 100), Is.True);
    Assert.That(gate.TryConfirm(objectId: 12, otherId: 40, tick: 100), Is.False);
}

[UnityTest]
public IEnumerator OwnerPushesDynamicBox_ServerAndOwnerConverge()
{
    var fixture = yield return PredictionFixture.WithDynamicBox();
    fixture.OwnerInput.SetMove(Vector2.up);
    yield return fixture.AdvanceTicks(100);
    Assert.That(Vector3.Distance(fixture.ServerBox.position, fixture.OwnerBox.position), Is.LessThan(0.1f));
    Assert.That(fixture.ConfirmedCollisionEffectCount, Is.EqualTo(1));
}

[UnityTest]
public IEnumerator TwoPlayersCollide_ServerAndBothOwnersConverge()
{
    var fixture = yield return PredictionFixture.WithTwoOwnersFacingEachOther();
    fixture.SetBothMoveForward();
    yield return fixture.AdvanceTicks(100);
    Assert.That(fixture.ServerPlayersOverlap, Is.False);
    Assert.That(fixture.MaxOwnerServerPositionError, Is.LessThan(0.1f));
}
```

- [ ] **Step 2: Run prediction tests and verify missing dynamic body behavior**

Run Task 2 Step 2.

- [ ] **Step 3: Implement a server-driven predicted dynamic body**

Use a `TickNetworkBehaviour` with an empty/default replicate for inertia plus server-authored `DynamicBodyForceCommand` entries. Apply all commands through `PredictionRigidbody.AddForce/AddForceAtPosition`, call `Simulate()` once, and reconcile the full `PredictionRigidbody` in `TimeManager_OnPostTick`. Reject force commands originating from clients; gameplay services enqueue them only on the server.

```csharp
[Replicate]
private void PerformReplicate(DynamicBodyReplicateData data,
    ReplicateState state = ReplicateState.Invalid, Channel channel = Channel.Unreliable)
{
    if (IsServerStarted)
        foreach (var command in _serverForces.DequeueForTick(data.GetTick()))
            _predictionBody.AddForceAtPosition(command.Force, command.Position, command.Mode);
    _predictionBody.Simulate();
}
```

- [ ] **Step 4: Add prediction-aware collision callbacks**

Attach FishNet `NetworkCollision` to players and dynamic boxes. `PredictionCollisionRelay` listens to `OnEnter/OnStay/OnExit`, records physical contact immediately, but passes sounds/particles/analytics through `ConfirmedCollisionEventGate` keyed by `(selfObjectId, otherObjectId, tick, eventKind)`.

- [ ] **Step 5: Configure dynamic/static scene bodies**

`PredictedDynamicBox.prefab` has spawnable predicted `NetworkObject`, Rigidbody, BoxCollider, `PredictedDynamicRigidbodyBehaviour`, and `NetworkCollision`. Place at least three networked boxes in the match scene. Add FishNet `OfflineRigidbody` to non-networked movable scene props that can be touched during replay. Static planet/world colliders need neither component.

Register `PredictedDynamicBox.prefab` in `Assets/DefaultPrefabObjects.asset` when the test harness spawns it at runtime.

- [ ] **Step 6: Run tests and commit**

```powershell
git add -- Assets/Scripts/Modules/FishNetworking/Gameplay/Physics Assets/Tests/EditMode/FishNetworkingGameplay/ConfirmedCollisionEventGateTests.cs Assets/Tests/PlayMode/Prediction/DynamicRigidbodyPredictionTests.cs Assets/Prefabs/Network/PredictedDynamicBox.prefab Assets/DefaultPrefabObjects.asset Assets/Scenes/MultiplayerPlayground.unity
git commit -m "feat: add predicted dynamic physics interactions"
```

## Task 5: Prediction observability and forced-divergence tests

**Files:**
- Modify: `Assets/Scripts/Modules/FishNetworking/Gameplay/Prediction/PredictionMetrics.cs`
- Create: `Assets/Scripts/Modules/FishNetworking/Gameplay/Prediction/PredictionStructuredLogger.cs`
- Create: `Assets/Tests/PlayMode/Prediction/ForcedDivergenceTests.cs`
- Create: `Assets/Tests/Runtime/MultiplayerSoak/MultiplayerSoakReporter.cs`

**Interfaces:**
- Consumes: prediction tick/reconcile measurements.
- Produces: structured correction, replay, and tick-overrun evidence.

- [ ] **Step 1: Write a failing forced-divergence test**

```csharp
[UnityTest]
public IEnumerator ForcedOwnerDivergence_IsCorrectedWithCompleteState()
{
    var fixture = yield return PredictionFixture.StartOwnerAndServer();
    fixture.Owner.SetPhysicsState(position: Vector3.one * 20f, velocity: Vector3.one * 8f);
    fixture.Owner.SetMotorState(States.HoldingJumpOn("planet-box", 0.15f, 180f));
    yield return fixture.AdvanceTicks(12);
    Assert.That(Vector3.Distance(fixture.OwnerPosition, fixture.ServerPosition), Is.LessThan(0.05f));
    Assert.That(fixture.OwnerState, Is.EqualTo(fixture.ServerState));
    Assert.That(fixture.Metrics.LastCorrectionDistance, Is.GreaterThan(1f));
}
```

- [ ] **Step 2: Implement allocation-free metric aggregation**

Track count, max, sum, and histogram buckets for position/angle corrections; replay count; rejected input count; tick duration; and physics-step duration. Log records with `PlayerId`, local/server tick, correction values, active `SurfaceId`, and replay count. Do not log every zero correction.

- [ ] **Step 3: Run tests and commit**

```powershell
git add -- Assets/Scripts/Modules/FishNetworking/Gameplay/Prediction Assets/Tests/PlayMode/Prediction/ForcedDivergenceTests.cs Assets/Tests/Runtime/MultiplayerSoak/MultiplayerSoakReporter.cs
git commit -m "test: instrument prediction reconciliation"
```

## Task 6: Multi-process automation and network profiles

**Files:**
- Create: `Assets/Tests/Runtime/MultiplayerSoak/MultiplayerSoakClient.cs`
- Create: `Assets/Scripts/CI/Editor/MultiplayerSoakBuild.cs`
- Create: `Assets/Scripts/Modules/FishNetworking/Gameplay/Diagnostics/FishNetNetworkImpairmentController.cs`
- Create: `Tools/Run-MultiplayerSoak.ps1`
- Create: `docs/testing/multiplayer-network-profiles.md`

**Interfaces:**
- Consumes: dedicated/client builds and command-line startup.
- Produces: repeatable 2-, 3-, and 10-client process tests.

- [ ] **Step 1: Add deterministic bot input**

`MultiplayerSoakClient` activates only with `-soakClient <index>`. It auto-readies, walks a deterministic circle based on index, jumps every 150 ticks, and writes one JSON summary on exit with session/spawn/correction/tick metrics.

```csharp
var phase = (tick + (uint)(_clientIndex * 17)) % 600;
var angle = phase / 600f * Mathf.PI * 2f;
var move = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
var jumpPressed = tick % 150u == (uint)(_clientIndex % 150);
_input.SetAutomated(move, angle * Mathf.Rad2Deg, jumpPressed, jumpPressed);
```

- [ ] **Step 2: Implement the process harness**

`Run-MultiplayerSoak.ps1` accepts `-Clients`, `-DurationSeconds`, `-Port`, `-Profile`, and `-OutputDirectory`; starts the server hidden, waits for a `server-listening` log record, starts clients hidden, waits for summaries, then terminates only process IDs it started. It fails if phase never reaches `Playing`, any client lacks one owned spawn, correction/tick assertions fail, or a process exits nonzero.

Add `-Scenario steady|disconnect-lobby|disconnect-loading|disconnect-spawn|disconnect-playing|jip`. The harness delays or terminates one client at the named lifecycle point, then asserts the session phase, entity count, reservation count, and survival of unaffected clients from structured summaries.

- [ ] **Step 3: Define the four network profiles**

```text
local:       0 ms latency, 0 ms jitter, 0% loss
wan-100:    50 ms one-way latency, 10 ms jitter, 0% loss
wan-200:   100 ms one-way latency, 20 ms jitter, 0% loss
lossy-200: 100 ms one-way latency, 20 ms jitter, 2% packet loss
```

Implement `FishNetNetworkImpairmentController` over the local public `TransportManager.LatencySimulator` API. At each FishNet tick, use a seeded `System.Random` to select milliseconds in `[latency-jitter, latency+jitter]`, then call `SetLatency`, `SetPacketLoss(lossPercent / 100d)`, `SetOutOfOrder(0d)`, and `SetEnabled(true)`. The seed is `profileSeed + clientIndex`, so test runs are reproducible without changing FishNet vendor source.

```csharp
private void OnTick()
{
    var minimum = Math.Max(0, _profile.OneWayLatencyMs - _profile.JitterMs);
    var maximumExclusive = _profile.OneWayLatencyMs + _profile.JitterMs + 1;
    var latency = _random.Next(minimum, maximumExclusive);
    var simulator = _networkManager.TransportManager.LatencySimulator;
    simulator.SetLatency(latency);
    simulator.SetPacketLoss(_profile.PacketLossPercent / 100d);
    simulator.SetOutOfOrder(0d);
    simulator.SetEnabled(true);
}
```

- [ ] **Step 4: Run short gates**

```powershell
& Tools/Run-MultiplayerSoak.ps1 -Clients 2 -DurationSeconds 120 -Port 7777 -Profile local -OutputDirectory Temp/soak/local-2
& Tools/Run-MultiplayerSoak.ps1 -Clients 3 -DurationSeconds 120 -Port 7778 -Profile wan-100 -OutputDirectory Temp/soak/wan-3
& Tools/Run-MultiplayerSoak.ps1 -Clients 3 -DurationSeconds 120 -Port 7779 -Profile local -Scenario disconnect-loading -OutputDirectory Temp/soak/disconnect-loading
& Tools/Run-MultiplayerSoak.ps1 -Clients 3 -DurationSeconds 120 -Port 7780 -Profile local -Scenario jip -OutputDirectory Temp/soak/jip
```

Expected: one load, one owned spawn/client, third client JIP when delayed by 30 seconds, no server tick stall, and all processes exit successfully.

- [ ] **Step 5: Commit automation**

```powershell
git add -- Assets/Tests/Runtime/MultiplayerSoak Assets/Scripts/CI/Editor/MultiplayerSoakBuild.cs Tools/Run-MultiplayerSoak.ps1 docs/testing/multiplayer-network-profiles.md
git commit -m "test: add multiplayer process harness"
```

## Task 7: Final 10-player acceptance gate

**Files:**
- Create: `docs/testing/multiplayer-foundation-final-acceptance.md`
- Modify: `docs/superpowers/specs/2026-08-13-multiplayer-foundation-design.md`

**Interfaces:**
- Consumes: outputs of Plans 01-04.
- Produces: evidence that the approved first slice is complete.

- [ ] **Step 1: Run all EditMode tests**

```powershell
& 'D:\Program Files\Unity\Hub\Editor\6000.3.12f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath . -runTests -testPlatform EditMode -testResults Temp/final-editmode.xml -logFile Temp/final-editmode.log
```

Expected: exit code 0, zero failures, architecture-boundary tests included.

- [ ] **Step 2: Run all PlayMode tests**

```powershell
& 'D:\Program Files\Unity\Hub\Editor\6000.3.12f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath . -runTests -testPlatform PlayMode -testResults Temp/final-playmode.xml -logFile Temp/final-playmode.log
```

Expected: exit code 0, zero failures, including player/player collision, box push, divergence, gravity replay, and camera smoothing.

- [ ] **Step 3: Run the 10-client 10-minute matrix**

```powershell
& Tools/Run-MultiplayerSoak.ps1 -Clients 10 -DurationSeconds 600 -Port 7780 -Profile local -OutputDirectory Temp/soak/final-local
& Tools/Run-MultiplayerSoak.ps1 -Clients 10 -DurationSeconds 600 -Port 7781 -Profile wan-100 -OutputDirectory Temp/soak/final-wan-100
& Tools/Run-MultiplayerSoak.ps1 -Clients 10 -DurationSeconds 600 -Port 7782 -Profile wan-200 -OutputDirectory Temp/soak/final-wan-200
& Tools/Run-MultiplayerSoak.ps1 -Clients 10 -DurationSeconds 600 -Port 7783 -Profile lossy-200 -OutputDirectory Temp/soak/final-lossy
```

Record hardware/build profile with correction and tick metrics. Performance thresholds are accepted from this measured baseline, while functional requirements remain zero duplicate spawns, zero leaked session entries, no authority violations, and eventual owner/server convergence.

- [ ] **Step 4: Recheck offline Playground**

Run `Playground` for five minutes after all FishNet integration. Confirm TimeManager remains `PhysicsMode.Unity`, offline movement uses `FixedUpdate`, and character/camera remain smooth.

- [ ] **Step 5: Record all 14 acceptance criteria and mark the spec implemented**

For each criterion in the design spec, link the responsible test and captured result. Change design status from `Approved design` to `Implemented and verified` only when every criterion has evidence.

- [ ] **Step 6: Commit final evidence**

```powershell
git add -- docs/testing/multiplayer-foundation-final-acceptance.md docs/superpowers/specs/2026-08-13-multiplayer-foundation-design.md
git commit -m "test: verify multiplayer foundation acceptance"
```
