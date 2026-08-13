using System;
using NUnit.Framework;

namespace Modules.Multiplayer.Primitives.Tests
{
    public sealed class ResultTests
    {
        [Test]
        public void Success_ExposesOnlyValueBranch()
        {
            var result = Result<int, string>.Success(7);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.IsFailure, Is.False);
            Assert.That(result.Value, Is.EqualTo(7));
            Assert.Throws<InvalidOperationException>(() => _ = result.Error);
        }

        [Test]
        public void Failure_ExposesOnlyErrorBranch()
        {
            var result = Result<int, string>.Failure("rejected");

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error, Is.EqualTo("rejected"));
            Assert.Throws<InvalidOperationException>(() => _ = result.Value);
        }

        [Test]
        public void DefaultResult_IsFailureWithDefaultError()
        {
            var result = default(Result<int, string>);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.Throws<InvalidOperationException>(() => _ = result.Value);
        }

        [Test]
        public void Unit_AllValuesAreEqual()
        {
            Assert.That(Unit.Value.Equals(default(Unit)), Is.True);
            Assert.That(Unit.Value.Equals((object)default(Unit)), Is.True);
            Assert.That(Unit.Value == default(Unit), Is.True);
            Assert.That(Unit.Value != default(Unit), Is.False);
            Assert.That(Unit.Value.GetHashCode(), Is.Zero);
        }
    }
}
