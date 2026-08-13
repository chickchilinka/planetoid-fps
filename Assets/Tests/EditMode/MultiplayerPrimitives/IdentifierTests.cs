using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace Modules.Multiplayer.Primitives.Tests
{
    public sealed class IdentifierTests
    {
        private static readonly Guid FirstValue =
            Guid.Parse("11111111-1111-1111-1111-111111111111");

        private static readonly Guid SecondValue =
            Guid.Parse("22222222-2222-2222-2222-222222222222");

        [Test]
        public void PlayerId_UsesGuidValueSemantics()
        {
            AssertIdentifier(
                new PlayerId(FirstValue),
                new PlayerId(FirstValue),
                new PlayerId(SecondValue),
                PlayerId.None,
                FirstValue.ToString("N"));
            Assert.That(new PlayerId(FirstValue) == new PlayerId(FirstValue), Is.True);
            Assert.That(new PlayerId(FirstValue) != new PlayerId(SecondValue), Is.True);
        }

        [Test]
        public void SessionId_UsesGuidValueSemantics()
        {
            AssertIdentifier(
                new SessionId(FirstValue),
                new SessionId(FirstValue),
                new SessionId(SecondValue),
                SessionId.None,
                FirstValue.ToString("N"));
            Assert.That(new SessionId(FirstValue) == new SessionId(FirstValue), Is.True);
            Assert.That(new SessionId(FirstValue) != new SessionId(SecondValue), Is.True);
        }

        [Test]
        public void MatchId_UsesGuidValueSemantics()
        {
            AssertIdentifier(
                new MatchId(FirstValue),
                new MatchId(FirstValue),
                new MatchId(SecondValue),
                MatchId.None,
                FirstValue.ToString("N"));
            Assert.That(new MatchId(FirstValue) == new MatchId(FirstValue), Is.True);
            Assert.That(new MatchId(FirstValue) != new MatchId(SecondValue), Is.True);
        }

        [Test]
        public void MapId_UsesGuidValueSemantics()
        {
            AssertIdentifier(
                new MapId(FirstValue),
                new MapId(FirstValue),
                new MapId(SecondValue),
                MapId.None,
                FirstValue.ToString("N"));
            Assert.That(new MapId(FirstValue) == new MapId(FirstValue), Is.True);
            Assert.That(new MapId(FirstValue) != new MapId(SecondValue), Is.True);
        }

        [Test]
        public void OperationId_UsesGuidValueSemantics()
        {
            AssertIdentifier(
                new OperationId(FirstValue),
                new OperationId(FirstValue),
                new OperationId(SecondValue),
                OperationId.None,
                FirstValue.ToString("N"));
            Assert.That(new OperationId(FirstValue) == new OperationId(FirstValue), Is.True);
            Assert.That(new OperationId(FirstValue) != new OperationId(SecondValue), Is.True);
        }

        [TestCase(typeof(PlayerId))]
        [TestCase(typeof(SessionId))]
        [TestCase(typeof(MatchId))]
        [TestCase(typeof(MapId))]
        [TestCase(typeof(OperationId))]
        public void Identifier_DoesNotDeclareImplicitConversions(Type identifierType)
        {
            var implicitConversions = identifierType
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(method => method.Name == "op_Implicit")
                .ToArray();

            Assert.That(implicitConversions, Is.Empty);
        }

        private static void AssertIdentifier<T>(
            T first,
            T equal,
            T different,
            T none,
            string expectedText)
            where T : struct, IEquatable<T>
        {
            Assert.That(first.Equals(equal), Is.True);
            Assert.That(first.Equals(different), Is.False);
            Assert.That(first.Equals((object)equal), Is.True);
            Assert.That(first.GetHashCode(), Is.EqualTo(equal.GetHashCode()));
            Assert.That(first.ToString(), Is.EqualTo(expectedText));

            var isValid = (bool)typeof(T).GetProperty("IsValid").GetValue(first);
            var noneIsValid = (bool)typeof(T).GetProperty("IsValid").GetValue(none);
            Assert.That(isValid, Is.True);
            Assert.That(noneIsValid, Is.False);
        }
    }
}
