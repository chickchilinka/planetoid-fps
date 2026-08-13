using System;
using System.Collections.Generic;
using System.Linq;
using Modules.SurfaceGravity.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Utils;

namespace Modules.SurfaceGravity.Tests
{
    [TestFixture]
    public sealed class SurfaceGravitySolverTests
    {
        private static readonly Vector3ComparerWithEqualsOperator VectorComparer =
            Vector3ComparerWithEqualsOperator.Instance;

        [Test]
        public void Solve_SelectsNearestSurface_AndProducesAccelerationTowardIt()
        {
            var provider = new FakeGravitySurfaceProvider(
                Sample("planet-a", Vector3.up, 4f),
                Sample("planet-b", Vector3.down, 64f));
            var solver = new SurfaceGravitySolver(provider, SurfaceGravitySettings.Default);

            var result = solver.Solve(Input(Vector3.zero, GravityState.Empty));

            Assert.That(result.HasSurface, Is.True);
            Assert.That(result.State.ActiveSurface, Is.EqualTo(new SurfaceId("planet-a")));
            Assert.That(result.Acceleration, Is.EqualTo(Vector3.down * 9.81f).Using(VectorComparer));
        }

        [Test]
        public void Solve_EqualDistances_SelectsLowestOrdinalSurfaceId_RegardlessOfProviderOrder()
        {
            var first = SolveOnce(
                Sample("planet-b", Vector3.right, 4f),
                Sample("planet-a", Vector3.left, 4f));
            var second = SolveOnce(
                Sample("planet-a", Vector3.left, 4f),
                Sample("planet-b", Vector3.right, 4f));

            Assert.That(first.State.ActiveSurface, Is.EqualTo(new SurfaceId("planet-a")));
            Assert.That(second.State, Is.EqualTo(first.State));
            Assert.That(second.Acceleration, Is.EqualTo(first.Acceleration).Using(VectorComparer));
        }

        [Test]
        public void Solve_PreviousSurfaceWithinHysteresis_RemainsSelected()
        {
            var previous = new GravityState(new SurfaceId("planet-b"), Vector3.up);
            var provider = new FakeGravitySurfaceProvider(
                Sample("planet-a", Vector3.up, 1f),
                Sample("planet-b", Vector3.right, 1.2f));
            var settings = new SurfaceGravitySettings(9.81f, 6f, 0.25f);
            var solver = new SurfaceGravitySolver(provider, settings);

            var result = solver.Solve(Input(Vector3.zero, previous));

            Assert.That(result.State.ActiveSurface, Is.EqualTo(new SurfaceId("planet-b")));
            Assert.That(result.Acceleration, Is.EqualTo(Vector3.left * 9.81f).Using(VectorComparer));
        }

        [Test]
        public void Solve_PreviousSurfaceOutsideHysteresis_SelectsNearestSurface()
        {
            var previous = new GravityState(new SurfaceId("planet-b"), Vector3.up);
            var provider = new FakeGravitySurfaceProvider(
                Sample("planet-a", Vector3.up, 1f),
                Sample("planet-b", Vector3.right, 1.251f));
            var settings = new SurfaceGravitySettings(9.81f, 6f, 0.25f);
            var solver = new SurfaceGravitySolver(provider, settings);

            var result = solver.Solve(Input(Vector3.zero, previous));

            Assert.That(result.State.ActiveSurface, Is.EqualTo(new SurfaceId("planet-a")));
        }

        [Test]
        public void Solve_UsesExponentialNormalSmoothingFromReplayableState()
        {
            var previous = new GravityState(new SurfaceId("planet-a"), Vector3.up);
            var settings = new SurfaceGravitySettings(9.81f, 6f, 0.25f);
            var solver = new SurfaceGravitySolver(
                new FakeGravitySurfaceProvider(Sample("planet-a", Vector3.right, 1f)),
                settings);

            var result = solver.Solve(Input(Vector3.zero, previous, 0.02f));
            var blend = 1f - Mathf.Exp(-settings.NormalSharpness * 0.02f);
            var expected = Vector3.Slerp(Vector3.up, Vector3.right, blend).normalized;

            Assert.That(result.TargetUp, Is.EqualTo(expected).Using(VectorComparer));
            Assert.That(result.State.SmoothedUp, Is.EqualTo(expected).Using(VectorComparer));
        }

        [Test]
        public void Solve_FirstStepWithEmptyState_StartsSmoothingFromBodyUp()
        {
            var bodyUp = Vector3.down;
            var settings = new SurfaceGravitySettings(9.81f, 6f, 0.25f);
            var solver = new SurfaceGravitySolver(
                new FakeGravitySurfaceProvider(Sample("planet-a", Vector3.right, 1f)),
                settings);

            var result = solver.Solve(new GravityStepInput(
                Vector3.zero,
                bodyUp,
                GravityState.Empty,
                0.02f));
            var blend = 1f - Mathf.Exp(-settings.NormalSharpness * 0.02f);
            var expected = Vector3.Slerp(bodyUp, Vector3.right, blend).normalized;
            var incorrectWorldUpResult = Vector3.Slerp(Vector3.up, Vector3.right, blend).normalized;

            Assert.That(result.TargetUp, Is.EqualTo(expected).Using(VectorComparer));
            Assert.That(result.TargetUp, Is.Not.EqualTo(incorrectWorldUpResult).Using(VectorComparer));
        }

        [Test]
        public void Solve_ProviderReturnsDefaultSample_ThrowsConfigurationError()
        {
            var solver = new SurfaceGravitySolver(
                new FakeGravitySurfaceProvider(default(GravitySurfaceSample)),
                SurfaceGravitySettings.Default);

            Assert.Throws<InvalidOperationException>(() =>
                solver.Solve(Input(Vector3.zero, GravityState.Empty)));
        }

        [Test]
        public void Solve_NoSamples_ReturnsNoAccelerationAndPreservesState()
        {
            var previous = new GravityState(new SurfaceId("planet-a"), Vector3.right);
            var solver = new SurfaceGravitySolver(
                new FakeGravitySurfaceProvider(),
                SurfaceGravitySettings.Default);

            var result = solver.Solve(new GravityStepInput(
                Vector3.zero,
                Vector3.forward,
                previous,
                0.02f));

            Assert.That(result.HasSurface, Is.False);
            Assert.That(result.Acceleration, Is.EqualTo(Vector3.zero).Using(VectorComparer));
            Assert.That(result.TargetUp, Is.EqualTo(Vector3.forward).Using(VectorComparer));
            Assert.That(result.State, Is.EqualTo(previous));
        }

        [Test]
        public void Solve_RepeatingSameSequence_ProducesSameState()
        {
            var positions = Enumerable.Range(0, 30)
                .Select(index => new Vector3(index * 0.01f, 2f, 0f))
                .ToArray();

            Assert.That(Run(positions), Is.EqualTo(Run(positions)));
        }

        [Test]
        public void SurfaceId_UsesOrdinalEquality()
        {
            Assert.That(new SurfaceId("planet-a"), Is.EqualTo(new SurfaceId("planet-a")));
            Assert.That(new SurfaceId("PLANET-A"), Is.Not.EqualTo(new SurfaceId("planet-a")));
            Assert.That(default(SurfaceId), Is.EqualTo(SurfaceId.None));
            Assert.That(SurfaceId.None.IsValid, Is.False);
        }

        [TestCase(0f)]
        [TestCase(-0.02f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void GravityStepInput_InvalidTickDelta_Throws(float tickDelta)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new GravityStepInput(Vector3.zero, Vector3.up, GravityState.Empty, tickDelta));
        }

        [Test]
        public void GravityStepInput_NonFinitePosition_Throws()
        {
            Assert.Throws<ArgumentException>(() =>
                new GravityStepInput(
                    new Vector3(float.NaN, 0f, 0f),
                    Vector3.up,
                    GravityState.Empty,
                    0.02f));
        }

        [TestCase(0f, 6f)]
        [TestCase(-1f, 6f)]
        [TestCase(9.81f, 0f)]
        [TestCase(9.81f, -1f)]
        public void SurfaceGravitySettings_NonPositiveAccelerationOrSharpness_Throws(
            float acceleration,
            float sharpness)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SurfaceGravitySettings(acceleration, sharpness, 0.25f));
        }

        private static GravityStepResult SolveOnce(params GravitySurfaceSample[] samples)
        {
            return new SurfaceGravitySolver(
                    new FakeGravitySurfaceProvider(samples),
                    SurfaceGravitySettings.Default)
                .Solve(Input(Vector3.zero, GravityState.Empty));
        }

        private static GravitySurfaceSample Sample(string id, Vector3 normal, float sqrDistance)
        {
            return new GravitySurfaceSample(new SurfaceId(id), Vector3.zero, normal, sqrDistance);
        }

        private static GravityStepInput Input(
            Vector3 position,
            GravityState previousState,
            float tickDelta = 0.02f)
        {
            return new GravityStepInput(position, Vector3.up, previousState, tickDelta);
        }

        private static IReadOnlyList<GravityState> Run(IEnumerable<Vector3> positions)
        {
            var solver = new SurfaceGravitySolver(
                new FakeGravitySurfaceProvider(
                    Sample("planet-b", Vector3.right, 4f),
                    Sample("planet-a", Vector3.up, 4f)),
                SurfaceGravitySettings.Default);
            var state = GravityState.Empty;
            var states = new List<GravityState>();

            foreach (var position in positions)
            {
                var result = solver.Solve(Input(position, state));
                state = result.State;
                states.Add(state);
            }

            return states;
        }
    }
}
