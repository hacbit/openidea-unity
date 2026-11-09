using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor.Compilation;
using UnityEngine;

namespace OpenIDEA.Editor.Tests
{
    public class UnityProjectGenerationIntegrationTests
    {
        [Test]
        public void SnapshotMatchesTheImportedPackageCompilationAndResolvesItsSources()
        {
            var root = Directory.GetParent(Application.dataPath).FullName;
            var native = CompilationPipeline.GetAssemblies(AssembliesType.Editor).Single(a => a.name == "OpenIDEA.Editor");
            var package = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.hacbit.ide.openidea");
            var snapshot = UnityProjectSnapshot.Collect(root, ProjectGenerationSettings.DefaultSources);
            var project = snapshot.Single(a => a.Name == native.name);

            Assert.That(project.GenerateProject, Is.EqualTo(ProjectGenerationSettings.Includes(ProjectGenerationSettings.DefaultSources, package.source)));
            Assert.That(project.Sources.Count, Is.EqualTo(native.sourceFiles.Length));
            Assert.That(project.Sources.All(path => File.Exists(Path.Combine(root, path))), Is.True);
            Assert.That(project.LanguageVersion, Is.EqualTo(native.compilerOptions.LanguageVersion));
            Assert.That(project.AssemblyReferences, Is.EquivalentTo(native.assemblyReferences.Select(a => a.name)));
            CollectionAssert.IsSubsetOf(native.defines, project.Defines);
        }
    }
}
