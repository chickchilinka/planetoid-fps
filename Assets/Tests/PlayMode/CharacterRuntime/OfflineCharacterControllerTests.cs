using System.Collections;
using Modules.Character.Simulation;
using Modules.SurfaceGravity.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Modules.Character.UnityRuntime.Tests
{
    public sealed class OfflineCharacterControllerTests
    {
        [UnityTest]
        public IEnumerator JumpPressedBetweenFixedTicks_IsConsumedExactlyOnce()
        {
            var source = new UnityCharacterInputSource(new ConstantViewYawProvider(10f));

            source.Sample(Vector2.zero, 10f, true);
            var first = source.ConsumeForTick();
            var second = source.ConsumeForTick();

            Assert.That(first.JumpPressed, Is.True);
            Assert.That(first.JumpHeld, Is.True);
            Assert.That(second.JumpPressed, Is.False);
            Assert.That(second.JumpHeld, Is.True);
            yield return null;
        }

        [UnityTest]
        public IEnumerator JumpPressedAndReleasedBetweenFixedTicks_RemainsBuffered()
        {
            var source = new UnityCharacterInputSource(new ConstantViewYawProvider(0f));

            source.Sample(Vector2.zero, 0f, true);
            source.Sample(Vector2.zero, 0f, false);
            var input = source.ConsumeForTick();

            Assert.That(input.JumpPressed, Is.True);
            Assert.That(input.JumpHeld, Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator FixedUpdate_AppliesOneSimulationStep()
        {
            var gameObject = new GameObject("offline-character-controller-test");
            try
            {
                var rigidbody = gameObject.AddComponent<Rigidbody>();
                var controller = gameObject.AddComponent<OfflineCharacterController>();
                var simulation = new CharacterSimulationSpy();
                controller.Construct(
                    simulation,
                    new ConstantInputSource(CharacterInput.None),
                    new ConstantGroundProbe(),
                    CharacterMovementSettings.Default);

                yield return new WaitForFixedUpdate();

                Assert.That(simulation.CallCount, Is.EqualTo(1));
                Assert.That(simulation.LastTickDelta, Is.EqualTo(Time.fixedDeltaTime));
                Assert.That(rigidbody.useGravity, Is.False);
                Assert.That(rigidbody.interpolation, Is.EqualTo(RigidbodyInterpolation.Interpolate));
            }
            finally
            {
                Object.Destroy(gameObject);
            }

            yield return null;
        }

        private sealed class ConstantViewYawProvider : ICharacterViewYawProvider
        {
            public ConstantViewYawProvider(float yaw)
            {
                Yaw = yaw;
            }

            public float Yaw { get; }
        }

        private sealed class ConstantInputSource : ICharacterInputSource
        {
            private readonly CharacterInput _input;

            public ConstantInputSource(CharacterInput input)
            {
                _input = input;
            }

            public CharacterInput ConsumeForTick()
            {
                return _input;
            }
        }

        private sealed class ConstantGroundProbe : ICharacterGroundProbe
        {
            public bool IsGrounded(Vector3 position, Vector3 up, out Vector3 normal)
            {
                normal = up;
                return false;
            }
        }

        private sealed class CharacterSimulationSpy : ICharacterSimulationService
        {
            public int CallCount { get; private set; }

            public float LastTickDelta { get; private set; }

            public CharacterStepResult Simulate(
                in CharacterInput input,
                in CharacterBodySnapshot body,
                in CharacterSimulationState state,
                float tickDelta)
            {
                CallCount++;
                LastTickDelta = tickDelta;
                return new CharacterStepResult(
                    body.LinearVelocity,
                    Vector3.zero,
                    body.Rotation,
                    new CharacterSimulationState(
                        new GravityState(SurfaceId.None, body.Rotation * Vector3.up),
                        JumpPhase.None,
                        0f,
                        input.ViewYaw,
                        body.Rotation * Vector3.forward));
            }
        }
    }
}
