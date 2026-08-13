using System;
using System.Collections.Generic;
using Modules.SurfaceGravity.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Utils;

namespace Modules.Character.Simulation.Tests
{
    [TestFixture]
    public sealed class CharacterSimulationServiceTests
    {
        private static readonly Vector3ComparerWithEqualsOperator VectorComparer =
            Vector3ComparerWithEqualsOperator.Instance;

        [Test]
        public void Simulate_GroundedInput_ReplacesTangentVelocityAndPreservesVerticalVelocity()
        {
            var body = Body(
                velocity: new Vector3(1f, -2f, 3f),
                grounded: true,
                up: Vector3.up);

            var result = Service().Simulate(
                new CharacterInput(Vector2.up, 0f, false, false),
                body,
                CharacterSimulationState.Initial,
                0.02f);

            Assert.That(Vector3.Dot(result.LinearVelocity, Vector3.up), Is.EqualTo(-2f).Within(0.0001f));
            Assert.That(
                Vector3.ProjectOnPlane(result.LinearVelocity, Vector3.up).magnitude,
                Is.EqualTo(6f).Within(0.0001f));
            Assert.That(result.LinearVelocity.x, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void Simulate_AirborneInput_MovesTangentVelocityByAccelerationTimesTickDelta()
        {
            var result = Service().Simulate(
                new CharacterInput(Vector2.up, 0f, false, false),
                Body(velocity: Vector3.down * 2f, grounded: false),
                CharacterSimulationState.Initial,
                0.02f);

            Assert.That(
                Vector3.ProjectOnPlane(result.LinearVelocity, Vector3.up),
                Is.EqualTo(Vector3.forward * 0.06f).Using(VectorComparer));
            Assert.That(Vector3.Dot(result.LinearVelocity, Vector3.up), Is.EqualTo(-2f).Within(0.0001f));
        }

        [Test]
        public void Simulate_MoveMagnitudeAboveOne_IsClamped()
        {
            var result = Service().Simulate(
                new CharacterInput(new Vector2(10f, 0f), 0f, false, false),
                Body(grounded: true),
                CharacterSimulationState.Initial,
                0.02f);

            Assert.That(result.LinearVelocity, Is.EqualTo(Vector3.right * 6f).Using(VectorComparer));
        }

        [Test]
        public void Simulate_JumpPressedOnce_StartsJumpWithoutDependingOnFrameTime()
        {
            var result = Service().Simulate(
                new CharacterInput(Vector2.zero, 0f, true, true),
                Body(grounded: true),
                CharacterSimulationState.Initial,
                0.02f);

            Assert.That(Vector3.Dot(result.LinearVelocity, Vector3.up), Is.EqualTo(5f).Within(0.0001f));
            Assert.That(result.State.JumpPhase, Is.EqualTo(JumpPhase.Holding));
            Assert.That(result.State.JumpElapsed, Is.EqualTo(0f));
        }

        [Test]
        public void Simulate_JumpHold_AddsAccelerationUsingExplicitTickDelta()
        {
            var first = Service().Simulate(
                new CharacterInput(Vector2.zero, 0f, true, true),
                Body(grounded: true),
                CharacterSimulationState.Initial,
                0.02f);

            var second = Service().Simulate(
                new CharacterInput(Vector2.zero, 0f, false, true),
                Body(velocity: first.LinearVelocity, grounded: false),
                first.State,
                0.02f);

            Assert.That(Vector3.Dot(second.LinearVelocity, Vector3.up), Is.EqualTo(5.08f).Within(0.0001f));
            Assert.That(second.State.JumpPhase, Is.EqualTo(JumpPhase.Holding));
            Assert.That(second.State.JumpElapsed, Is.EqualTo(0.02f).Within(0.0001f));
        }

        [Test]
        public void Simulate_JumpHoldAtLimit_AppliesOnlyRemainingTimeThenEnds()
        {
            var settings = Settings(maxJumpHoldTime: 0.03f);
            var service = Service(settings: settings);
            var first = service.Simulate(
                new CharacterInput(Vector2.zero, 0f, true, true),
                Body(grounded: true),
                CharacterSimulationState.Initial,
                0.02f);
            var second = service.Simulate(
                new CharacterInput(Vector2.zero, 0f, false, true),
                Body(velocity: first.LinearVelocity),
                first.State,
                0.02f);
            var third = service.Simulate(
                new CharacterInput(Vector2.zero, 0f, false, true),
                Body(velocity: second.LinearVelocity),
                second.State,
                0.02f);

            Assert.That(Vector3.Dot(third.LinearVelocity, Vector3.up), Is.EqualTo(5.12f).Within(0.0001f));
            Assert.That(third.State.JumpPhase, Is.EqualTo(JumpPhase.None));
            Assert.That(third.State.JumpElapsed, Is.EqualTo(0f));
        }

        [Test]
        public void Simulate_JumpReleased_StopsHoldAcceleration()
        {
            var holding = new CharacterSimulationState(
                GravityState.Empty,
                JumpPhase.Holding,
                0.02f,
                0f);

            var result = Service().Simulate(
                new CharacterInput(Vector2.zero, 0f, false, false),
                Body(velocity: Vector3.up * 5f),
                holding,
                0.02f);

            Assert.That(result.LinearVelocity, Is.EqualTo(Vector3.up * 5f).Using(VectorComparer));
            Assert.That(result.State.JumpPhase, Is.EqualTo(JumpPhase.None));
            Assert.That(result.State.JumpElapsed, Is.EqualTo(0f));
        }

        [Test]
        public void Simulate_JumpPressedWhileAirborne_DoesNotStartJump()
        {
            var result = Service().Simulate(
                new CharacterInput(Vector2.zero, 0f, true, true),
                Body(velocity: Vector3.down, grounded: false),
                CharacterSimulationState.Initial,
                0.02f);

            Assert.That(result.LinearVelocity, Is.EqualTo(Vector3.down).Using(VectorComparer));
            Assert.That(result.State.JumpPhase, Is.EqualTo(JumpPhase.None));
        }

        [Test]
        public void Simulate_GravityTargetUp_DrivesMovementPlaneAndRotation()
        {
            var targetUp = Vector3.right;
            var gravityState = new GravityState(new SurfaceId("planet-side"), targetUp);
            var provider = new FakeCharacterGravityProvider(Vector3.left * 9.81f, targetUp, gravityState);
            var result = Service(provider).Simulate(
                new CharacterInput(Vector2.up, 0f, false, false),
                Body(grounded: true),
                CharacterSimulationState.Initial,
                0.02f);

            Assert.That(result.LinearVelocity, Is.EqualTo(Vector3.forward * 6f).Using(VectorComparer));
            Assert.That(result.TargetRotation * Vector3.up, Is.EqualTo(targetUp).Using(VectorComparer));
            Assert.That(result.TargetRotation * Vector3.forward, Is.EqualTo(Vector3.forward).Using(VectorComparer));
        }

        [Test]
        public void Simulate_ForwardsExactReplayableGravityQuery_AndStoresReturnedState()
        {
            var bodyRotation = Quaternion.AngleAxis(90f, Vector3.forward);
            var previousGravity = new GravityState(new SurfaceId("old"), Vector3.up);
            var returnedGravity = new GravityState(new SurfaceId("new"), Vector3.right);
            var provider = new FakeCharacterGravityProvider(Vector3.left, Vector3.right, returnedGravity);
            var state = new CharacterSimulationState(previousGravity, JumpPhase.None, 0f, 12f);
            var body = new CharacterBodySnapshot(
                new Vector3(3f, 4f, 5f),
                bodyRotation,
                Vector3.zero,
                false,
                Vector3.up);

            var result = Service(provider).Simulate(
                new CharacterInput(Vector2.zero, 37f, false, false),
                body,
                state,
                0.025f);

            Assert.That(provider.Queries, Has.Count.EqualTo(1));
            var query = provider.Queries[0];
            Assert.That(query.Position, Is.EqualTo(body.Position).Using(VectorComparer));
            Assert.That(query.BodyUp, Is.EqualTo(bodyRotation * Vector3.up).Using(VectorComparer));
            Assert.That(query.PreviousState, Is.EqualTo(previousGravity));
            Assert.That(query.TickDelta, Is.EqualTo(0.025f));
            Assert.That(result.Acceleration, Is.EqualTo(Vector3.left).Using(VectorComparer));
            Assert.That(result.State.Gravity, Is.EqualTo(returnedGravity));
            Assert.That(result.State.ViewYaw, Is.EqualTo(37f));
        }

        [Test]
        public void Simulate_ReplaySequence_IsBitwiseStableWithinOneRuntime()
        {
            var inputs = new[]
            {
                new CharacterInput(new Vector2(0.2f, 1f), 10f, false, false),
                new CharacterInput(new Vector2(-1f, 0.5f), 25f, true, true),
                new CharacterInput(new Vector2(0.4f, -0.2f), 40f, false, true),
                new CharacterInput(Vector2.zero, 40f, false, false)
            };

            Assert.That(RunSequence(inputs), Is.EqualTo(RunSequence(inputs)));
        }

        [TestCase(0f)]
        [TestCase(-0.02f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void Simulate_InvalidTickDelta_Throws(float tickDelta)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Service().Simulate(
                CharacterInput.None,
                Body(),
                CharacterSimulationState.Initial,
                tickDelta));
        }

        [Test]
        public void Simulate_NonFiniteInput_Throws()
        {
            Assert.Throws<ArgumentException>(() => Service().Simulate(
                new CharacterInput(new Vector2(float.NaN, 0f), 0f, false, false),
                Body(),
                CharacterSimulationState.Initial,
                0.02f));
        }

        [Test]
        public void Simulate_GravityProviderReturnsInvalidState_ThrowsConfigurationError()
        {
            var provider = new FakeCharacterGravityProvider(
                Vector3.down,
                Vector3.up,
                new GravityState(SurfaceId.None, Vector3.zero));

            Assert.Throws<InvalidOperationException>(() => Service(provider).Simulate(
                CharacterInput.None,
                Body(),
                CharacterSimulationState.Initial,
                0.02f));
        }

        [TestCase(0f, 5f, 4f, 3f, 1f, 45f)]
        [TestCase(6f, 0f, 4f, 3f, 1f, 45f)]
        [TestCase(6f, 5f, -1f, 3f, 1f, 45f)]
        [TestCase(6f, 5f, 4f, -1f, 1f, 45f)]
        [TestCase(6f, 5f, 4f, 3f, 0f, 45f)]
        [TestCase(6f, 5f, 4f, 3f, 1f, 0f)]
        public void CharacterMovementSettings_InvalidValues_Throw(
            float moveSpeed,
            float initialJumpSpeed,
            float holdJumpAcceleration,
            float airAcceleration,
            float maxJumpHoldTime,
            float maxRotationDegreesPerTick)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new CharacterMovementSettings(
                moveSpeed,
                initialJumpSpeed,
                holdJumpAcceleration,
                airAcceleration,
                maxJumpHoldTime,
                maxRotationDegreesPerTick));
        }

        private static CharacterSimulationService Service(
            FakeCharacterGravityProvider provider = null,
            CharacterMovementSettings settings = null)
        {
            provider ??= new FakeCharacterGravityProvider(
                Vector3.down * 9.81f,
                Vector3.up,
                GravityState.Empty);
            return new CharacterSimulationService(provider, settings ?? Settings());
        }

        private static CharacterMovementSettings Settings(float maxJumpHoldTime = 1f)
        {
            return new CharacterMovementSettings(
                moveSpeed: 6f,
                initialJumpSpeed: 5f,
                holdJumpAcceleration: 4f,
                airAcceleration: 3f,
                maxJumpHoldTime: maxJumpHoldTime,
                maxRotationDegreesPerTick: 45f);
        }

        private static CharacterBodySnapshot Body(
            Vector3? velocity = null,
            bool grounded = false,
            Vector3? up = null)
        {
            var bodyUp = up ?? Vector3.up;
            return new CharacterBodySnapshot(
                Vector3.zero,
                Quaternion.FromToRotation(Vector3.up, bodyUp),
                velocity ?? Vector3.zero,
                grounded,
                bodyUp);
        }

        private static IReadOnlyList<CharacterStepResult> RunSequence(IEnumerable<CharacterInput> inputs)
        {
            var gravityState = new GravityState(new SurfaceId("planet-a"), Vector3.up);
            var provider = new FakeCharacterGravityProvider(Vector3.down * 9.81f, Vector3.up, gravityState);
            var service = Service(provider);
            var state = CharacterSimulationState.Initial;
            var velocity = Vector3.zero;
            var results = new List<CharacterStepResult>();
            var index = 0;

            foreach (var input in inputs)
            {
                var body = new CharacterBodySnapshot(
                    new Vector3(index * 0.1f, 2f, 0f),
                    Quaternion.identity,
                    velocity,
                    index <= 1,
                    Vector3.up);
                var result = service.Simulate(input, body, state, 0.02f);
                state = result.State;
                velocity = result.LinearVelocity;
                results.Add(result);
                index++;
            }

            return results;
        }
    }
}
