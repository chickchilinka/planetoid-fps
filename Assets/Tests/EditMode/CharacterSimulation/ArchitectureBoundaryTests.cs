using System.Linq;
using NUnit.Framework;
using UnityEditor.Compilation;

namespace Modules.Character.Simulation.Tests
{
    public sealed class ArchitectureBoundaryTests
    {
        [TestCase("SurfaceGravity.Core")]
        [TestCase("SurfaceGravity.UnityRuntime")]
        [TestCase("Character.Simulation")]
        [TestCase("Character.UnityRuntime")]
        public void RuntimeCharacterAssemblies_DoNotReferenceFishNet(string assemblyName)
        {
            var assembly = CompilationPipeline.GetAssemblies()
                .SingleOrDefault(candidate => candidate.name == assemblyName);

            Assert.That(assembly, Is.Not.Null, $"Assembly '{assemblyName}' was not compiled.");
            Assert.That(
                assembly.assemblyReferences.Select(reference => reference.name),
                Does.Not.Contain("FishNet.Runtime"),
                $"Assembly '{assemblyName}' must remain transport-agnostic.");
        }
    }
}
