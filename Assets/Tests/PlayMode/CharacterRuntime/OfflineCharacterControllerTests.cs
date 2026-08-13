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
        public IEnumerator PresentationAnchor_ChildSmoothsRootCorrectionAndPreservesOffset()
        {
            var physicsRoot = new GameObject("physics-root");
            var anchorObject = new GameObject("presentation-anchor");
            var authoredOffset = new Vector3(1f, 2f, -3f);
            var authoredRotationOffset = Quaternion.Euler(10f, 20f, 30f);
            anchorObject.transform.SetParent(physicsRoot.transform, false);
            anchorObject.transform.SetLocalPositionAndRotation(
                authoredOffset,
                authoredRotationOffset);
            anchorObject.SetActive(false);
            var anchor = anchorObject.AddComponent<CharacterPresentationAnchor>();
            anchor.ConfigureForTests(physicsRoot.transform, 1f, 1f);

            try
            {
                anchorObject.SetActive(true);
                var oldPosition = anchorObject.transform.position;
                physicsRoot.transform.SetPositionAndRotation(
                    new Vector3(10f, 5f, -2f),
                    Quaternion.Euler(0f, 90f, 0f));
                var targetPosition = physicsRoot.transform.TransformPoint(authoredOffset);

                anchor.UpdatePresentation(0.1f);

                Assert.That(Vector3.Distance(anchorObject.transform.position, oldPosition), Is.GreaterThan(0f));
                Assert.That(Vector3.Distance(anchorObject.transform.position, targetPosition), Is.GreaterThan(0f));

                anchor.UpdatePresentation(100f);

                Assert.That(Vector3.Distance(anchorObject.transform.position, targetPosition), Is.LessThan(0.0001f));
                Assert.That(
                    Quaternion.Angle(
                        anchorObject.transform.rotation,
                        physicsRoot.transform.rotation * authoredRotationOffset),
                    Is.LessThan(0.0001f));
            }
            finally
            {
                Object.Destroy(anchorObject);
                Object.Destroy(physicsRoot);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator PresentationAnchor_PreservesAuthoredRelativePose()
        {
            var physicsRoot = new GameObject("physics-root");
            var anchorObject = new GameObject("presentation-anchor");
            physicsRoot.transform.SetPositionAndRotation(
                new Vector3(3f, 2f, 1f),
                Quaternion.Euler(0f, 30f, 0f));
            var authoredOffset = new Vector3(1f, 2f, -3f);
            var authoredRotationOffset = Quaternion.Euler(10f, 20f, 30f);
            anchorObject.transform.SetPositionAndRotation(
                physicsRoot.transform.TransformPoint(authoredOffset),
                physicsRoot.transform.rotation * authoredRotationOffset);
            anchorObject.SetActive(false);
            var anchor = anchorObject.AddComponent<CharacterPresentationAnchor>();
            anchor.ConfigureForTests(physicsRoot.transform, 100000f, 100000f);

            try
            {
                anchorObject.SetActive(true);
                physicsRoot.transform.SetPositionAndRotation(
                    new Vector3(-4f, 8f, 2f),
                    Quaternion.Euler(35f, -40f, 15f));

                yield return null;

                Assert.That(
                    Vector3.Distance(
                        anchorObject.transform.position,
                        physicsRoot.transform.TransformPoint(authoredOffset)),
                    Is.LessThan(0.0001f));
                Assert.That(
                    Quaternion.Angle(
                        anchorObject.transform.rotation,
                        physicsRoot.transform.rotation * authoredRotationOffset),
                    Is.LessThan(0.0001f));
            }
            finally
            {
                Object.Destroy(anchorObject);
                Object.Destroy(physicsRoot);
            }

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
                Assert.That(
                    rigidbody.constraints & RigidbodyConstraints.FreezeRotation,
                    Is.EqualTo(RigidbodyConstraints.None));
            }
            finally
            {
                Object.Destroy(gameObject);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator FixedUpdate_CommandedRotationProgressesWithoutFreezeConstraints()
        {
            var gameObject = new GameObject("offline-character-rotation-test");
            try
            {
                var rigidbody = gameObject.AddComponent<Rigidbody>();
                var controller = gameObject.AddComponent<OfflineCharacterController>();
                controller.Construct(
                    new TargetRotationSimulation(Quaternion.Euler(0f, 90f, 0f)),
                    new ConstantInputSource(CharacterInput.None),
                    new ConstantGroundProbe(),
                    CharacterMovementSettings.Default);

                yield return new WaitForFixedUpdate();
                yield return new WaitForFixedUpdate();

                Assert.That(Quaternion.Angle(Quaternion.identity, rigidbody.rotation), Is.GreaterThan(1f));
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

        private sealed class TargetRotationSimulation : ICharacterSimulationService
        {
            private readonly Quaternion _targetRotation;

            public TargetRotationSimulation(Quaternion targetRotation)
            {
                _targetRotation = targetRotation;
            }

            public CharacterStepResult Simulate(
                in CharacterInput input,
                in CharacterBodySnapshot body,
                in CharacterSimulationState state,
                float tickDelta)
            {
                return new CharacterStepResult(
                    body.LinearVelocity,
                    Vector3.zero,
                    _targetRotation,
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
