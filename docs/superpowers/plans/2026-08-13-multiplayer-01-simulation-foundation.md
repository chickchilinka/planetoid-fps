# Multiplayer Simulation Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the frame-coupled gravity and movement code with replayable services and keep the offline `Playground` smooth on Unity fixed physics.

**Architecture:** `SurfaceGravity.Core` and `Character.Simulation` are FishNet-free services. Unity scene queries and Rigidbody mutation live behind consumer-owned ports in runtime adapter assemblies; offline and network adapters will consume the same step result.

**Tech Stack:** Unity 6000.3.12f1, C# 9, Unity Physics, NUnit/Unity Test Framework 1.6.0, Zenject 9.3.1.

## Global Constraints

- The dedicated server is authoritative over physics and final game state; clients send input intent only.
- Core session, spawning, gravity, and character-simulation assemblies contain no FishNet references.
- `SurfaceGravity` supports static scene-authored sources only; dynamic rigidbodies are not gravity sources.
- Network runtime physics will use FishNet `TimeManager`; offline runtime uses Unity `FixedUpdate`.
- A single runtime never applies the same character step from both physics loops.
- Stable authored `SurfaceId` values replace Unity instance IDs and runtime GUIDs.
- Preserve unrelated working-tree changes and stage only files named by each task.
- Use TDD and make the commit at the end of every task.

---

## File structure

```text
Assets/Scripts/Modules/SurfaceGravity/Core/
  SurfaceGravity.Core.asmdef                 Pure gravity contracts, state, and solver.
  SurfaceId.cs                               Stable source identity.
  GravitySurfaceSample.cs                    Immutable provider sample.
  GravityState.cs                            Replayable surface selection and smoothed up.
  GravityStep.cs                             Input/result values.
  IGravitySurfaceProvider.cs                 Port owned by gravity.
  ISurfaceGravitySolver.cs                   Public module facade.
  SurfaceGravitySolver.cs                    Selection, hysteresis, and smoothing.
  SurfaceGravitySettings.cs                  Solver constants.

Assets/Scripts/Modules/SurfaceGravity/UnityRuntime/
  SurfaceGravity.UnityRuntime.asmdef
  GravitySurfaceView.cs                      Authored ID and static collider reference.
  SceneGravitySurfaceProvider.cs             Non-alloc scene sampling adapter.
  SurfaceGravityInstaller.cs                 Zenject composition entry.

Assets/Scripts/Modules/Character/Simulation/
  Character.Simulation.asmdef
  CharacterInput.cs
  CharacterBodySnapshot.cs
  CharacterSimulationState.cs
  CharacterStepResult.cs
  CharacterMovementSettings.cs
  ICharacterGravityProvider.cs               Consumer-owned gravity port.
  ICharacterSimulationService.cs             Public module facade.
  CharacterSimulationService.cs

Assets/Scripts/Modules/Character/UnityRuntime/
  Character.UnityRuntime.asmdef
  ICharacterInputSource.cs
  ICharacterViewYawProvider.cs
  UnityCharacterInputSource.cs               Render-frame input and jump-edge buffer.
  CharacterSurfaceGravityAdapter.cs
  ICharacterGroundProbe.cs
  PhysicsCharacterGroundProbe.cs
  OfflineCharacterController.cs              FixedUpdate body adapter.
  CharacterPresentationAnchor.cs              LateUpdate smoothing anchor.
  OfflineCharacterInstaller.cs

Assets/Tests/EditMode/SurfaceGravity/
  SurfaceGravity.Tests.asmdef
  SurfaceGravitySolverTests.cs
  FakeGravitySurfaceProvider.cs

Assets/Tests/EditMode/CharacterSimulation/
  Character.Simulation.Tests.asmdef
  CharacterSimulationServiceTests.cs
  FakeCharacterGravityProvider.cs

Assets/Tests/PlayMode/CharacterRuntime/
  Character.UnityRuntime.Tests.asmdef
  OfflineCharacterControllerTests.cs

Assets/Scripts/Base/SurfaceGravity/             Removed after the migration.
Assets/Scripts/Base/RigidbodyMovement/           Removed after the migration.
Assets/Scripts/Features/Character/Movement/      Removed after the migration.
```

## Task 1: Static surface-gravity solver

**Files:**
- Create: `Assets/Scripts/Modules/SurfaceGravity/Core/SurfaceGravity.Core.asmdef`
- Create: `Assets/Scripts/Modules/SurfaceGravity/Core/SurfaceId.cs`
- Create: `Assets/Scripts/Modules/SurfaceGravity/Core/GravitySurfaceSample.cs`
- Create: `Assets/Scripts/Modules/SurfaceGravity/Core/GravityState.cs`
- Create: `Assets/Scripts/Modules/SurfaceGravity/Core/GravityStep.cs`
- Create: `Assets/Scripts/Modules/SurfaceGravity/Core/IGravitySurfaceProvider.cs`
- Create: `Assets/Scripts/Modules/SurfaceGravity/Core/ISurfaceGravitySolver.cs`
- Create: `Assets/Scripts/Modules/SurfaceGravity/Core/SurfaceGravitySettings.cs`
- Create: `Assets/Scripts/Modules/SurfaceGravity/Core/SurfaceGravitySolver.cs`
- Create: `Assets/Tests/EditMode/SurfaceGravity/SurfaceGravity.Tests.asmdef`
- Create: `Assets/Tests/EditMode/SurfaceGravity/FakeGravitySurfaceProvider.cs`
- Create: `Assets/Tests/EditMode/SurfaceGravity/SurfaceGravitySolverTests.cs`

**Interfaces:**
- Consumes: no project module; Unity `Vector3` math only.
- Produces: `ISurfaceGravitySolver.Solve(in GravityStepInput)` and `IGravitySurfaceProvider.GetSamples(Vector3, List<GravitySurfaceSample>)`.

- [ ] **Step 1: Create the test assembly and write failing selection/replay tests**

```json
{
  "name": "SurfaceGravity.Tests",
  "references": ["SurfaceGravity.Core", "UnityEngine.TestRunner", "UnityEditor.TestRunner"],
  "includePlatforms": ["Editor"],
  "optionalUnityReferences": ["TestAssemblies"]
}
```

```csharp
[Test]
public void Solve_SelectsNearestSurface_AndProducesAccelerationTowardIt()
{
    var provider = new FakeGravitySurfaceProvider(
        new GravitySurfaceSample(new SurfaceId("planet-a"), new Vector3(0, 0, 0), Vector3.up, 4f),
        new GravitySurfaceSample(new SurfaceId("planet-b"), new Vector3(0, 8, 0), Vector3.down, 64f));
    var solver = new SurfaceGravitySolver(provider, SurfaceGravitySettings.Default);

    var result = solver.Solve(new GravityStepInput(new Vector3(0, 2, 0), Vector3.up, GravityState.Empty, 0.02f));

    Assert.That(result.HasSurface, Is.True);
    Assert.That(result.State.ActiveSurface, Is.EqualTo(new SurfaceId("planet-a")));
    Assert.That(result.Acceleration, Is.EqualTo(Vector3.down * 9.81f).Using(Vector3ComparerWithEqualsOperator.Instance));
}

[Test]
public void Solve_RepeatingSameSequence_ProducesSameState()
{
    var inputs = Enumerable.Range(0, 30)
        .Select(i => new Vector3(i * 0.01f, 2f, 0f))
        .ToArray();
    Assert.That(Run(inputs), Is.EqualTo(Run(inputs)));
}
```

- [ ] **Step 2: Run the tests and confirm the missing-type failure**

Run:

```powershell
& 'D:\Program Files\Unity\Hub\Editor\6000.3.12f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath . -runTests -testPlatform EditMode -assemblyNames SurfaceGravity.Tests -testResults Temp/surface-gravity-results.xml -logFile Temp/surface-gravity.log
```

Expected: compilation fails because `SurfaceId`, solver contracts, and solver do not exist.

- [ ] **Step 3: Add the gravity value types and facade contracts**

```csharp
public readonly struct SurfaceId : IEquatable<SurfaceId>
{
    public static readonly SurfaceId None = new(string.Empty);
    public string Value { get; }
    public bool IsValid => !string.IsNullOrWhiteSpace(Value);
    public SurfaceId(string value) => Value = value ?? string.Empty;
    public bool Equals(SurfaceId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
    public override bool Equals(object obj) => obj is SurfaceId other && Equals(other);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
}

public readonly struct GravitySurfaceSample
{
    public SurfaceId SurfaceId { get; }
    public Vector3 ClosestPoint { get; }
    public Vector3 OutwardNormal { get; }
    public float SqrDistance { get; }
    public GravitySurfaceSample(SurfaceId id, Vector3 point, Vector3 normal, float sqrDistance)
    {
        SurfaceId = id;
        ClosestPoint = point;
        OutwardNormal = normal;
        SqrDistance = sqrDistance;
    }
}

public readonly struct GravityState : IEquatable<GravityState>
{
    public static readonly GravityState Empty = new(SurfaceId.None, Vector3.up);
    public SurfaceId ActiveSurface { get; }
    public Vector3 SmoothedUp { get; }
    public GravityState(SurfaceId activeSurface, Vector3 smoothedUp) { ActiveSurface = activeSurface; SmoothedUp = smoothedUp; }
}

public interface IGravitySurfaceProvider
{
    void GetSamples(Vector3 worldPosition, List<GravitySurfaceSample> samples);
}

public interface ISurfaceGravitySolver
{
    GravityStepResult Solve(in GravityStepInput input);
}
```

Use `GravityStepInput(Position, BodyUp, PreviousState, TickDelta)` and `GravityStepResult(HasSurface, Acceleration, TargetUp, State)`. `SurfaceGravitySettings.Default` is `new(acceleration: 9.81f, normalSharpness: 6f, switchHysteresisSqr: 0.25f)`. Validate `TickDelta > 0`, finite vectors, positive gravity, and positive smoothing rates in constructors.

- [ ] **Step 4: Implement deterministic selection, hysteresis, and exponential smoothing**

```csharp
public GravityStepResult Solve(in GravityStepInput input)
{
    _samples.Clear();
    _surfaceProvider.GetSamples(input.Position, _samples);
    if (_samples.Count == 0)
        return GravityStepResult.NoSurface(input.PreviousState, input.BodyUp);

    _samples.Sort(GravitySurfaceSampleComparer.Instance); // SqrDistance, then SurfaceId ordinal.
    var selected = _samples[0];
    if (input.PreviousState.ActiveSurface.IsValid &&
        TryFind(input.PreviousState.ActiveSurface, out var current) &&
        current.SqrDistance <= selected.SqrDistance + _settings.SwitchHysteresisSqr)
        selected = current;

    var targetUp = selected.OutwardNormal.normalized;
    var from = input.PreviousState.SmoothedUp.sqrMagnitude > 0.5f
        ? input.PreviousState.SmoothedUp.normalized
        : input.BodyUp.normalized;
    var blend = 1f - Mathf.Exp(-_settings.NormalSharpness * input.TickDelta);
    var smoothUp = Vector3.Slerp(from, targetUp, blend).normalized;
    var state = new GravityState(selected.SurfaceId, smoothUp);
    return new GravityStepResult(true, -targetUp * _settings.Acceleration, smoothUp, state);
}
```

`GravitySurfaceSampleComparer` must make ties stable by `SurfaceId.Value`; never rely on provider enumeration order.

- [ ] **Step 5: Run the gravity tests**

Run the Step 2 command. Expected: all `SurfaceGravity.Tests` pass and the XML contains zero failures.

- [ ] **Step 6: Commit the solver**

```powershell
git add -- Assets/Scripts/Modules/SurfaceGravity/Core Assets/Tests/EditMode/SurfaceGravity
git commit -m "feat: add replayable surface gravity solver"
```

## Task 2: Static Unity gravity-surface provider

**Files:**
- Create: `Assets/Scripts/Modules/SurfaceGravity/UnityRuntime/SurfaceGravity.UnityRuntime.asmdef`
- Create: `Assets/Scripts/Modules/SurfaceGravity/UnityRuntime/GravitySurfaceView.cs`
- Create: `Assets/Scripts/Modules/SurfaceGravity/UnityRuntime/SceneGravitySurfaceProvider.cs`
- Create: `Assets/Scripts/Modules/SurfaceGravity/UnityRuntime/SurfaceGravityInstaller.cs`
- Create: `Assets/Tests/EditMode/SurfaceGravity/SceneGravitySurfaceProviderTests.cs`
- Modify: `Assets/Prefabs/Planets/BoxPlanet.prefab`
- Modify: `Assets/Prefabs/Planets/CapsulePlanet.prefab`
- Modify: `Assets/Prefabs/Planets/SpherePlanet.prefab`

**Interfaces:**
- Consumes: `SurfaceId`, `GravitySurfaceSample`, `IGravitySurfaceProvider`.
- Produces: immutable scene samples keyed by serialized IDs.

- [ ] **Step 1: Write failing tests for stable IDs and duplicate rejection**

```csharp
[Test]
public void BuildIndex_DuplicateSurfaceId_ThrowsConfigurationException()
{
    var a = CreateView("planet-same", Vector3.zero);
    var b = CreateView("planet-same", Vector3.right * 10f);
    Assert.Throws<InvalidOperationException>(() => new SceneGravitySurfaceProvider(new[] { a, b }, 300f, 16));
}

[Test]
public void GetSamples_OrdersEqualDistancesBySurfaceId()
{
    var provider = BuildSymmetricProvider("planet-b", "planet-a");
    var samples = new List<GravitySurfaceSample>();
    provider.GetSamples(Vector3.zero, samples);
    Assert.That(samples.Select(x => x.SurfaceId.Value), Is.EqualTo(new[] { "planet-a", "planet-b" }));
}
```

- [ ] **Step 2: Run `SurfaceGravity.Tests` and confirm the provider types are missing**

Run the Task 1 Step 2 command. Expected: compilation fails on `GravitySurfaceView` and `SceneGravitySurfaceProvider`.

- [ ] **Step 3: Implement immutable authored surfaces**

```csharp
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class GravitySurfaceView : MonoBehaviour
{
    [SerializeField] private string _surfaceId;
    [SerializeField] private Collider _gravityCollider;
    public SurfaceId SurfaceId => new(_surfaceId);
    public Collider GravityCollider => _gravityCollider;

#if UNITY_EDITOR
    private void OnValidate()
    {
        _gravityCollider ??= GetComponent<Collider>();
        if (string.IsNullOrWhiteSpace(_surfaceId))
            _surfaceId = GUID.Generate().ToString();
    }
#endif
}
```

`SceneGravitySurfaceProvider` receives the views from the installer, rejects blank/duplicate IDs, caches `Collider -> SurfaceId`, uses one preallocated `Collider[]`, calls `Physics.OverlapSphereNonAlloc`, and creates samples from `Collider.ClosestPoint`. Sort samples by distance then ID before returning. Cache each initial `localToWorldMatrix`; because `Collider.ClosestPoint` reads the collider transform, throw a development-build configuration error when the matrix changes instead of silently supporting a moving gravity source.

- [ ] **Step 4: Bind the module through one installer entry point**

```csharp
public override void InstallBindings()
{
    var views = FindObjectsByType<GravitySurfaceView>(FindObjectsInactive.Include, FindObjectsSortMode.None);
    Container.Bind<IGravitySurfaceProvider>().To<SceneGravitySurfaceProvider>().AsSingle()
        .WithArguments(views, _settings.PlanetSearchRadius, _maxOverlaps);
    Container.Bind<ISurfaceGravitySolver>().To<SurfaceGravitySolver>().AsSingle();
    Container.BindInstance(_settings).AsSingle();
}
```

The runtime asmdef references `SurfaceGravity.Core` and `Zenject`, not FishNet.

- [ ] **Step 5: Assign persistent IDs to the three planet prefabs**

Open each prefab, replace `GravityPlanetView` with `GravitySurfaceView`, retain its collider, and assign these literal IDs:

```text
BoxPlanet:    planet-box
CapsulePlanet: planet-capsule
SpherePlanet: planet-sphere
```

Save, reopen, and confirm the serialized IDs do not change.

- [ ] **Step 6: Run tests and validate prefab serialization**

Run the Task 1 Step 2 command, then:

```powershell
rg -n "planet-box|planet-capsule|planet-sphere" Assets/Prefabs/Planets -g '*.prefab'
```

Expected: tests pass and each ID occurs in exactly one prefab.

- [ ] **Step 7: Commit the Unity provider**

```powershell
git add -- Assets/Scripts/Modules/SurfaceGravity/UnityRuntime Assets/Tests/EditMode/SurfaceGravity/SceneGravitySurfaceProviderTests.cs Assets/Prefabs/Planets
git commit -m "feat: add static scene gravity provider"
```

## Task 3: Pure character simulation service

**Files:**
- Create: `Assets/Scripts/Modules/Character/Simulation/Character.Simulation.asmdef`
- Create: `Assets/Scripts/Modules/Character/Simulation/CharacterInput.cs`
- Create: `Assets/Scripts/Modules/Character/Simulation/CharacterBodySnapshot.cs`
- Create: `Assets/Scripts/Modules/Character/Simulation/CharacterSimulationState.cs`
- Create: `Assets/Scripts/Modules/Character/Simulation/CharacterStepResult.cs`
- Create: `Assets/Scripts/Modules/Character/Simulation/CharacterMovementSettings.cs`
- Create: `Assets/Scripts/Modules/Character/Simulation/ICharacterGravityProvider.cs`
- Create: `Assets/Scripts/Modules/Character/Simulation/ICharacterSimulationService.cs`
- Create: `Assets/Scripts/Modules/Character/Simulation/CharacterSimulationService.cs`
- Create: `Assets/Tests/EditMode/CharacterSimulation/Character.Simulation.Tests.asmdef`
- Create: `Assets/Tests/EditMode/CharacterSimulation/FakeCharacterGravityProvider.cs`
- Create: `Assets/Tests/EditMode/CharacterSimulation/CharacterSimulationServiceTests.cs`

**Interfaces:**
- Consumes: consumer-owned `ICharacterGravityProvider.Solve(in CharacterGravityQuery)`.
- Produces: `ICharacterSimulationService.Simulate(in CharacterInput, in CharacterBodySnapshot, in CharacterSimulationState, float) -> CharacterStepResult`.

- [ ] **Step 1: Write failing movement, jump, and replay tests**

```csharp
[Test]
public void Simulate_GroundedInput_ReplacesTangentVelocityAndPreservesVerticalVelocity()
{
    var body = Body(velocity: new Vector3(1f, -2f, 3f), grounded: true, up: Vector3.up);
    var result = Service().Simulate(new CharacterInput(Vector2.up, 0f, false, false), body, CharacterSimulationState.Initial, 0.02f);
    Assert.That(Vector3.Dot(result.LinearVelocity, Vector3.up), Is.EqualTo(-2f).Within(0.0001f));
    Assert.That(Vector3.ProjectOnPlane(result.LinearVelocity, Vector3.up).magnitude, Is.EqualTo(6f).Within(0.0001f));
}

[Test]
public void Simulate_JumpPressedOnce_StartsJumpWithoutDependingOnFrameTime()
{
    var result = Service().Simulate(new CharacterInput(Vector2.zero, 0f, true, true), Body(true), CharacterSimulationState.Initial, 0.02f);
    Assert.That(Vector3.Dot(result.LinearVelocity, Vector3.up), Is.EqualTo(5f).Within(0.0001f));
    Assert.That(result.State.JumpPhase, Is.EqualTo(JumpPhase.Holding));
}

[Test]
public void Simulate_ReplaySequence_IsBitwiseStableWithinOneRuntime()
{
    var first = RunSequence(Inputs);
    var second = RunSequence(Inputs);
    Assert.That(second, Is.EqualTo(first));
}
```

- [ ] **Step 2: Run the test assembly and verify missing types**

```powershell
& 'D:\Program Files\Unity\Hub\Editor\6000.3.12f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath . -runTests -testPlatform EditMode -assemblyNames Character.Simulation.Tests -testResults Temp/character-simulation-results.xml -logFile Temp/character-simulation.log
```

Expected: compilation fails because the simulation types do not exist.

- [ ] **Step 3: Define replayable input, body, state, result, and facade**

```csharp
public readonly struct CharacterInput
{
    public Vector2 Move { get; }
    public float ViewYaw { get; }
    public bool JumpPressed { get; }
    public bool JumpHeld { get; }
}

public readonly struct CharacterBodySnapshot
{
    public Vector3 Position { get; }
    public Quaternion Rotation { get; }
    public Vector3 LinearVelocity { get; }
    public bool IsGrounded { get; }
    public Vector3 GroundNormal { get; }
}

public readonly struct CharacterSimulationState
{
    public GravityState Gravity { get; }
    public JumpPhase JumpPhase { get; }
    public float JumpElapsed { get; }
    public float ViewYaw { get; }
}

public readonly struct CharacterStepResult
{
    public Vector3 LinearVelocity { get; }
    public Vector3 Acceleration { get; }
    public Quaternion TargetRotation { get; }
    public CharacterSimulationState State { get; }
}

public interface ICharacterSimulationService
{
    CharacterStepResult Simulate(in CharacterInput input, in CharacterBodySnapshot body,
        in CharacterSimulationState state, float tickDelta);
}
```

`Character.Simulation.asmdef` references `SurfaceGravity.Core` for `GravityState` but not `SurfaceGravity.UnityRuntime` or FishNet. `ICharacterGravityProvider` is declared inside `Character.Simulation` and returns a gravity result containing acceleration, target up, and next `GravityState`.

- [ ] **Step 4: Implement the motor using explicit tick delta only**

```csharp
public CharacterStepResult Simulate(in CharacterInput input, in CharacterBodySnapshot body,
    in CharacterSimulationState state, float tickDelta)
{
    Validate(input, body, tickDelta);
    var gravity = _gravityProvider.Solve(new CharacterGravityQuery(body.Position, body.Rotation * Vector3.up, state.Gravity, tickDelta));
    var up = gravity.TargetUp;
    var referenceAxis = Mathf.Abs(Vector3.Dot(up, Vector3.forward)) < 0.99f ? Vector3.forward : Vector3.right;
    var referenceForward = Vector3.ProjectOnPlane(referenceAxis, up).normalized;
    var forward = Quaternion.AngleAxis(input.ViewYaw, up) * referenceForward;
    var right = Vector3.Cross(up, forward).normalized;
    var move = Vector2.ClampMagnitude(input.Move, 1f);
    var desired = (forward * move.y + right * move.x) * _settings.MoveSpeed;
    var vertical = Vector3.Project(body.LinearVelocity, up);
    var tangent = Vector3.ProjectOnPlane(body.LinearVelocity, up);
    tangent = body.IsGrounded
        ? desired
        : Vector3.MoveTowards(tangent, desired, _settings.AirAcceleration * tickDelta);

    var next = AdvanceJump(input, body.IsGrounded, state, up, ref vertical, tickDelta);
    var targetRotation = Quaternion.LookRotation(forward, up);
    return new CharacterStepResult(tangent + vertical, gravity.Acceleration, targetRotation, next.WithGravity(gravity.State));
}
```

`AdvanceJump` removes existing velocity along `up` when starting, applies `InitialJumpSpeed` once, adds `HoldJumpAcceleration * tickDelta` only while held and below `MaxJumpHoldTime`, and returns `JumpPhase.None` when released or expired. No method reads `Time.deltaTime`, `Time.fixedDeltaTime`, input devices, physics statics, or Rigidbody state.

- [ ] **Step 5: Run character and gravity tests**

Run the Step 2 command and Task 1 Step 2 command. Expected: both assemblies pass.

- [ ] **Step 6: Commit the character simulation**

```powershell
git add -- Assets/Scripts/Modules/Character/Simulation Assets/Tests/EditMode/CharacterSimulation
git commit -m "feat: add replayable character simulation"
```

## Task 4: Offline Unity adapter and input edge buffer

**Files:**
- Create: `Assets/Scripts/Modules/Character/UnityRuntime/Character.UnityRuntime.asmdef`
- Create: `Assets/Scripts/Modules/Character/UnityRuntime/ICharacterInputSource.cs`
- Create: `Assets/Scripts/Modules/Character/UnityRuntime/ICharacterViewYawProvider.cs`
- Create: `Assets/Scripts/Modules/Character/UnityRuntime/UnityCharacterInputSource.cs`
- Create: `Assets/Scripts/Modules/Character/UnityRuntime/CharacterSurfaceGravityAdapter.cs`
- Create: `Assets/Scripts/Modules/Character/UnityRuntime/ICharacterGroundProbe.cs`
- Create: `Assets/Scripts/Modules/Character/UnityRuntime/PhysicsCharacterGroundProbe.cs`
- Create: `Assets/Scripts/Modules/Character/UnityRuntime/OfflineCharacterController.cs`
- Create: `Assets/Scripts/Modules/Character/UnityRuntime/CharacterPresentationAnchor.cs`
- Create: `Assets/Scripts/Modules/Character/UnityRuntime/OfflineCharacterInstaller.cs`
- Create: `Assets/Tests/PlayMode/CharacterRuntime/Character.UnityRuntime.Tests.asmdef`
- Create: `Assets/Tests/PlayMode/CharacterRuntime/OfflineCharacterControllerTests.cs`

**Interfaces:**
- Consumes: `ICharacterSimulationService`, `ISurfaceGravitySolver`.
- Produces: one `FixedUpdate` Rigidbody application path and reusable `ConsumeForTick()` input semantics.

- [ ] **Step 1: Write failing PlayMode tests for jump buffering and one-step application**

```csharp
[UnityTest]
public IEnumerator JumpPressedBetweenFixedTicks_IsConsumedExactlyOnce()
{
    var source = CreateInputSource();
    source.Sample(Vector2.zero, 10f, jumpHeld: true);
    var first = source.ConsumeForTick();
    var second = source.ConsumeForTick();
    Assert.That(first.JumpPressed, Is.True);
    Assert.That(second.JumpPressed, Is.False);
    yield return null;
}

[UnityTest]
public IEnumerator FixedUpdate_AppliesOneSimulationStep()
{
    var spy = new CharacterSimulationSpy();
    var controller = CreateController(spy);
    yield return new WaitForFixedUpdate();
    Assert.That(spy.CallCount, Is.EqualTo(1));
    Assert.That(spy.LastTickDelta, Is.EqualTo(Time.fixedDeltaTime));
}
```

- [ ] **Step 2: Run PlayMode tests and verify the missing adapter failure**

```powershell
& 'D:\Program Files\Unity\Hub\Editor\6000.3.12f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath . -runTests -testPlatform PlayMode -assemblyNames Character.UnityRuntime.Tests -testResults Temp/character-runtime-results.xml -logFile Temp/character-runtime.log
```

Expected: compilation fails on the new adapter types.

- [ ] **Step 3: Implement render sampling and tick consumption**

```csharp
public sealed class UnityCharacterInputSource : ICharacterInputSource, ITickable
{
    private bool _previousJumpHeld;
    private bool _jumpPressedBuffered;
    private Vector2 _move;
    private float _viewYaw;

    public void Tick()
    {
        var held = _input.GetButton("Jump");
        _jumpPressedBuffered |= held && !_previousJumpHeld;
        _previousJumpHeld = held;
        _move = Vector2.ClampMagnitude(new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")), 1f);
        _viewYaw = _viewYawProvider.Yaw;
    }

    public CharacterInput ConsumeForTick()
    {
        var value = new CharacterInput(_move, _viewYaw, _jumpPressedBuffered, _previousJumpHeld);
        _jumpPressedBuffered = false;
        return value;
    }
}
```

`ICharacterViewYawProvider` exposes `float Yaw { get; }`; its runtime implementation reads the camera yaw transform. Expose an internal `Sample(Vector2 move, float viewYaw, bool jumpHeld)` method to tests via `InternalsVisibleTo("Character.UnityRuntime.Tests")`; production sampling stays in `Tick()`.

- [ ] **Step 4: Implement the gravity bridge and offline controller**

```csharp
public CharacterGravityResult Solve(in CharacterGravityQuery query)
{
    var result = _solver.Solve(new GravityStepInput(query.Position, query.BodyUp, query.PreviousState, query.TickDelta));
    return new CharacterGravityResult(result.Acceleration, result.TargetUp, result.State);
}

private void FixedUpdate()
{
    var up = _state.Gravity.SmoothedUp.sqrMagnitude > 0.5f ? _state.Gravity.SmoothedUp : transform.up;
    var grounded = _groundProbe.IsGrounded(_rigidbody.position, up, out var groundNormal);
    var body = new CharacterBodySnapshot(_rigidbody.position, _rigidbody.rotation,
        _rigidbody.linearVelocity, grounded, groundNormal);
    var result = _simulation.Simulate(_input.ConsumeForTick(), body, _state, Time.fixedDeltaTime);
    _rigidbody.linearVelocity = result.LinearVelocity;
    _rigidbody.AddForce(result.Acceleration, ForceMode.Acceleration);
    _rigidbody.MoveRotation(Quaternion.RotateTowards(_rigidbody.rotation, result.TargetRotation,
        _settings.MaxRotationDegreesPerTick));
    _state = result.State;
}
```

`PhysicsCharacterGroundProbe` implements `ICharacterGroundProbe.IsGrounded(Vector3 position, Vector3 up, out Vector3 normal)` with one capsule/raycast query using the serialized ground mask and `QueryTriggerInteraction.Ignore`; it returns `up` as the normal when no ground is hit.

`OfflineCharacterController` is the only component that mutates the offline player Rigidbody. Set `useGravity = false`, `interpolation = Interpolate`, and freeze unwanted angular rotation in `Awake`.

- [ ] **Step 5: Add the presentation anchor**

```csharp
private void LateUpdate()
{
    var positionAlpha = 1f - Mathf.Exp(-_positionSharpness * Time.deltaTime);
    var rotationAlpha = 1f - Mathf.Exp(-_rotationSharpness * Time.deltaTime);
    transform.SetPositionAndRotation(
        Vector3.Lerp(transform.position, _physicsRoot.position, positionAlpha),
        Quaternion.Slerp(transform.rotation, _physicsRoot.rotation, rotationAlpha));
}
```

The offline camera provider reads this anchor; the physics root remains unsmoothed.

- [ ] **Step 6: Bind the offline composition and run PlayMode tests**

`OfflineCharacterInstaller` binds `ICharacterGravityProvider -> CharacterSurfaceGravityAdapter`, `ICharacterSimulationService -> CharacterSimulationService`, `ICharacterInputSource -> UnityCharacterInputSource`, and serialized settings/body/anchor instances. Run the Step 2 command. Expected: all PlayMode tests pass.

- [ ] **Step 7: Commit the offline adapter**

```powershell
git add -- Assets/Scripts/Modules/Character/UnityRuntime Assets/Tests/PlayMode/CharacterRuntime
git commit -m "feat: add offline character runtime adapter"
```

## Task 5: Migrate `Playground` and remove competing loops

**Files:**
- Modify: `Assets/Scenes/Playground.unity`
- Modify: `Assets/Prefabs/Player/Player.prefab`
- Modify: `Assets/Prefabs/Player/CameraRoot.prefab`
- Delete: `Assets/Scripts/Base/SurfaceGravity/Model/GravityBodyModel.cs`
- Delete: `Assets/Scripts/Base/SurfaceGravity/Bootstrap/SurfaceGravityMonoInstaller.cs`
- Delete: `Assets/Scripts/Base/SurfaceGravity/Data/SurfaceGravitySettings.cs`
- Delete: `Assets/Scripts/Base/SurfaceGravity/Services/SurfaceGravityManagementService.cs`
- Delete: `Assets/Scripts/Base/SurfaceGravity/Services/SurfaceGravityService.cs`
- Delete: `Assets/Scripts/Base/SurfaceGravity/Storage/GravityBodyModelStorage.cs`
- Delete: `Assets/Scripts/Base/SurfaceGravity/Storage/GravityBodyViewStorage.cs`
- Delete: `Assets/Scripts/Base/SurfaceGravity/Storage/GravityPlanetViewStorage.cs`
- Delete: `Assets/Scripts/Base/SurfaceGravity/View/GravityBodyView.cs`
- Delete: `Assets/Scripts/Base/SurfaceGravity/View/GravityPlanetView.cs`
- Delete: `Assets/Scripts/Base/SurfaceGravity/Utils/KDTree.cs`
- Delete: `Assets/Scripts/Base/RigidbodyMovement/RigidbodyMovementController.cs`
- Delete: `Assets/Scripts/Base/RigidbodyMovement/IRigidbody.cs`
- Delete: `Assets/Scripts/Base/RigidbodyMovement/Data/MovementSettings.cs`
- Delete: `Assets/Scripts/Base/RigidbodyMovement/Providers/IMovementDirectionProvider.cs`
- Delete: `Assets/Scripts/Base/RigidbodyMovement/Providers/IMovementInputProvider.cs`
- Delete: `Assets/Scripts/Features/Character/Movement/Adapter/LocalRigidbodyAdapter.cs`
- Delete: `Assets/Scripts/Features/Character/Movement/Provider/MovementDirectionProvider.cs`
- Delete: `Assets/Scripts/Features/Character/Movement/Provider/MovementInputProvider.cs`
- Delete: `Assets/Scripts/Features/Character/Movement/Bootstrap/CharacterControllerInstaller.cs`
- Modify: `Assets/Scripts/Base/ThirdPersonCamera/ThirdPersonCameraController.cs`

**Interfaces:**
- Consumes: new offline runtime and static gravity provider.
- Produces: smooth offline scene with exactly one movement/gravity physics loop.

- [ ] **Step 1: Add an edit-mode architecture test before deleting old code**

```csharp
[Test]
public void RuntimeCharacterAssemblies_DoNotReferenceFishNet()
{
    AssertNoReference("SurfaceGravity.Core", "FishNet.Runtime");
    AssertNoReference("SurfaceGravity.UnityRuntime", "FishNet.Runtime");
    AssertNoReference("Character.Simulation", "FishNet.Runtime");
    AssertNoReference("Character.UnityRuntime", "FishNet.Runtime");
}
```

Add this to `Assets/Tests/EditMode/CharacterSimulation/ArchitectureBoundaryTests.cs` using `CompilationPipeline.GetAssemblies()`.

- [ ] **Step 2: Rewire prefab and scene in the Unity Editor**

On `Player.prefab`:

```text
remove GravityBodyView
remove demo RigidbodyPrediction
set Rigidbody.useGravity = false
keep NetworkObject disabled from offline execution; its prediction settings remain for plan 04
add OfflineCharacterController but leave it disabled on the shared prefab
add child PresentationAnchor and move MeshFilter/MeshRenderer under it
point CameraTransformProvider at PresentationAnchor
```

In `Playground.unity`:

```text
add SurfaceGravityInstaller to SceneContext
replace CharacterControllerInstaller with OfflineCharacterInstaller
enable OfflineCharacterController on the scene player instance
ensure Network prefab TimeManager physics mode remains Unity (serialized _physicsMode: 0)
ensure no SurfaceGravityService or RigidbodyMovementController is bound
```

Migrate the scene tuning explicitly:

```text
MoveSpeed = 5 m/s
InitialJumpSpeed = 8 m/s
HoldJumpAcceleration = 4 m/s²
AirAcceleration = 3 m/s²
GroundCheckDistance = 1.1 m
MaxJumpHoldTime = 1 s
MaxRotationDegreesPerTick = 45 degrees
GravityAcceleration = 9.81 m/s²
NormalSharpness = 6
PlanetSearchRadius = 300 m
```

Preserve all unrelated scene overrides already present in the dirty scene.

- [ ] **Step 3: Make the camera follow the presentation anchor without a second physics write**

Keep `ThirdPersonCameraController.LateUpdate`, but replace `Vector3.MoveTowards(current, target, FollowSpeed * Time.deltaTime)` with exponential interpolation:

```csharp
var alpha = 1f - Mathf.Exp(-_settings.FollowSpeed * Time.deltaTime);
_yawTransform.position = Vector3.Lerp(_yawTransform.position, _cameraTransformProvider.YawPivot, alpha);
```

The camera controller must never write the player Rigidbody or physics root.

- [ ] **Step 4: Delete the obsolete movement/gravity classes and their `.meta` files through Unity**

After prefab/scene references point to the new components and the literal values above are copied, remove every listed old asset in the Project window so Unity removes matching metadata.

- [ ] **Step 5: Run all new tests and inspect the scene for missing scripts**

Run both EditMode commands and the PlayMode command from earlier tasks. Then open `Playground`, enter Play Mode for two minutes, walk/jump around each planet, and verify:

```text
Console has no Missing Script or duplicate SurfaceId errors
Rigidbody movement occurs only from OfflineCharacterController.FixedUpdate
TimeManager physics mode is Unity
camera follows PresentationAnchor in LateUpdate
no visible periodic character/camera jitter at rest or while moving
```

- [ ] **Step 6: Commit the migration**

```powershell
git add -- Assets/Scenes/Playground.unity Assets/Prefabs/Player Assets/Scripts/Base/SurfaceGravity Assets/Scripts/Base/RigidbodyMovement Assets/Scripts/Base/ThirdPersonCamera Assets/Scripts/Features/Character/Movement Assets/Tests/EditMode/CharacterSimulation/ArchitectureBoundaryTests.cs
git commit -m "refactor: migrate playground to replayable simulation"
```

## Task 6: Plan 01 acceptance gate

**Files:**
- Create: `docs/testing/multiplayer-foundation-plan-01.md`

**Interfaces:**
- Consumes: all Plan 01 outputs.
- Produces: recorded baseline for the next FishNet adapter plan.

- [ ] **Step 1: Run the complete EditMode suite**

```powershell
& 'D:\Program Files\Unity\Hub\Editor\6000.3.12f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath . -runTests -testPlatform EditMode -testResults Temp/plan-01-editmode.xml -logFile Temp/plan-01-editmode.log
```

Expected: process exit code 0 and zero failures.

- [ ] **Step 2: Run the complete PlayMode suite**

```powershell
& 'D:\Program Files\Unity\Hub\Editor\6000.3.12f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath . -runTests -testPlatform PlayMode -testResults Temp/plan-01-playmode.xml -logFile Temp/plan-01-playmode.log
```

Expected: process exit code 0 and zero failures.

- [ ] **Step 3: Record the baseline**

Write the Unity/FishNet versions, test counts, manual Playground duration, average visible FPS, and whether any jitter/corrections were observed. Include the exact commands above and links to retained XML artifacts in CI.

- [ ] **Step 4: Commit the acceptance record**

```powershell
git add -- docs/testing/multiplayer-foundation-plan-01.md
git commit -m "test: record offline simulation baseline"
```
