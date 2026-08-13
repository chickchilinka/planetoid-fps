using System.Linq;
using NUnit.Framework;
using UnityEditor.Compilation;

namespace Modules.Multiplayer.Session.Tests
{
    public sealed class ArchitectureBoundaryTests
    {
        [Test]
        public void Primitives_DoesNotReferenceTransportOrSerializationAssemblies()
        {
            AssertDoesNotReference(
                "Multiplayer.Primitives",
                "FishNet.Runtime",
                "Network",
                "MessagePack");
        }

        [Test]
        public void Session_DoesNotReferenceTransportOrSerializationAssemblies()
        {
            AssertDoesNotReference(
                "Multiplayer.Session",
                "FishNet.Runtime",
                "Network",
                "MessagePack");
        }

        [Test]
        public void SessionNetworking_DoesNotReferenceFishNet()
        {
            AssertDoesNotReference("Multiplayer.Session.Networking", "FishNet.Runtime");
        }

        private static void AssertDoesNotReference(string assemblyName, params string[] forbiddenReferences)
        {
            var assembly = CompilationPipeline.GetAssemblies()
                .SingleOrDefault(candidate => candidate.name == assemblyName);

            Assert.That(assembly, Is.Not.Null, $"Assembly '{assemblyName}' was not compiled.");
            var references = assembly.assemblyReferences.Select(reference => reference.name).ToArray();

            foreach (var forbiddenReference in forbiddenReferences)
            {
                Assert.That(
                    references,
                    Does.Not.Contain(forbiddenReference),
                    $"Assembly '{assemblyName}' must not reference '{forbiddenReference}'.");
            }
        }
    }
}
