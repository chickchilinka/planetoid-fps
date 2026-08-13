using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Modules.SurfaceGravity.Core;
using Modules.SurfaceGravity.UnityRuntime;
using NUnit.Framework;
using UnityEngine;

namespace Modules.SurfaceGravity.Tests
{
    public sealed class SceneGravitySurfaceProviderTests
    {
        private readonly List<GameObject> _createdObjects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            for (var index = _createdObjects.Count - 1; index >= 0; index--)
                UnityEngine.Object.DestroyImmediate(_createdObjects[index]);

            _createdObjects.Clear();
        }

        [Test]
        public void Constructor_DuplicateSurfaceId_ThrowsConfigurationException()
        {
            var first = CreateView("planet-same", Vector3.left * 10f);
            var second = CreateView("planet-same", Vector3.right * 10f);

            Assert.Throws<InvalidOperationException>(
                () => new SceneGravitySurfaceProvider(new[] { first, second }, 300f, 16));
        }

        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_BlankSurfaceId_ThrowsConfigurationException(string surfaceId)
        {
            var view = CreateView(surfaceId, Vector3.zero);

            Assert.Throws<InvalidOperationException>(
                () => new SceneGravitySurfaceProvider(new[] { view }, 300f, 16));
        }

        [Test]
        public void GetSamples_OrdersEqualDistancesBySurfaceId()
        {
            var planetB = CreateView("planet-b", Vector3.left * 10f);
            var planetA = CreateView("planet-a", Vector3.right * 10f);
            var provider = new SceneGravitySurfaceProvider(new[] { planetB, planetA }, 300f, 16);
            var samples = new List<GravitySurfaceSample>();
            Physics.SyncTransforms();

            provider.GetSamples(Vector3.zero, samples);

            Assert.That(
                samples.Select(sample => sample.SurfaceId.Value),
                Is.EqualTo(new[] { "planet-a", "planet-b" }));
        }

        [Test]
        public void GetSamples_CreatesClosestPointAndOutwardNormal()
        {
            var view = CreateView("planet-a", Vector3.zero);
            var provider = new SceneGravitySurfaceProvider(new[] { view }, 300f, 16);
            var samples = new List<GravitySurfaceSample>();
            Physics.SyncTransforms();

            provider.GetSamples(Vector3.right * 3f, samples);

            Assert.That(samples, Has.Count.EqualTo(1));
            Assert.That(samples[0].ClosestPoint, Is.EqualTo(Vector3.right).Using(Vector3Comparer.Instance));
            Assert.That(samples[0].OutwardNormal, Is.EqualTo(Vector3.right).Using(Vector3Comparer.Instance));
            Assert.That(samples[0].SqrDistance, Is.EqualTo(4f).Within(0.0001f));
        }

        [Test]
        public void GetSamples_WhenAuthoredSurfaceMoves_ThrowsConfigurationException()
        {
            var view = CreateView("planet-a", Vector3.zero);
            var provider = new SceneGravitySurfaceProvider(new[] { view }, 300f, 16);
            view.transform.position = Vector3.right;
            Physics.SyncTransforms();

            Assert.Throws<InvalidOperationException>(
                () => provider.GetSamples(Vector3.zero, new List<GravitySurfaceSample>()));
        }

        [TestCase(0f, 16)]
        [TestCase(-1f, 16)]
        [TestCase(float.NaN, 16)]
        [TestCase(300f, 0)]
        public void Constructor_InvalidQuerySettings_Throws(float searchRadius, int maxOverlaps)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new SceneGravitySurfaceProvider(
                    Array.Empty<GravitySurfaceView>(),
                    searchRadius,
                    maxOverlaps));
        }

        private GravitySurfaceView CreateView(string surfaceId, Vector3 position)
        {
            var gameObject = new GameObject(surfaceId);
            _createdObjects.Add(gameObject);
            gameObject.transform.position = position;

            var collider = gameObject.AddComponent<SphereCollider>();
            collider.radius = 1f;
            var view = gameObject.AddComponent<GravitySurfaceView>();
            SetField(view, "_surfaceId", surfaceId);
            SetField(view, "_gravityCollider", collider);
            return view;
        }

        private static void SetField<T>(GravitySurfaceView view, string fieldName, T value)
        {
            var field = typeof(GravitySurfaceView).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing serialized field {fieldName}.");
            field.SetValue(view, value);
        }

        private sealed class Vector3Comparer : IEqualityComparer<Vector3>
        {
            public static readonly Vector3Comparer Instance = new Vector3Comparer();

            public bool Equals(Vector3 left, Vector3 right)
            {
                return (left - right).sqrMagnitude <= 0.000001f;
            }

            public int GetHashCode(Vector3 value)
            {
                return value.GetHashCode();
            }
        }
    }
}
