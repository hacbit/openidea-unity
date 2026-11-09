using System;
using System.IO;
using NUnit.Framework;
using UnityEditor.PackageManager;

namespace OpenIDEA.Editor.Tests
{
    public class ProjectPathResolverTests
    {
        [Test]
        public void ResolvesPackageCacheAndExternalSourcesToTheirPhysicalFiles()
        {
            var root = Path.Combine(Path.GetTempPath(), "Unity Game");
            var local = Path.Combine(Path.GetTempPath(), "External package");
            var resolver = new ProjectPathResolver(root, new[]
            {
                new ProjectPackage { AssetPath = "Packages/com.example.cache", ResolvedPath = Path.Combine(root, "Library/PackageCache/com.example.cache@1.0"), Source = PackageSource.Registry },
                new ProjectPackage { AssetPath = "Packages/com.example.local", ResolvedPath = local, Source = PackageSource.Local },
                new ProjectPackage { AssetPath = "Packages/com.example.embedded", ResolvedPath = Path.Combine(root, "Packages/com.example.embedded"), Source = PackageSource.Embedded }
            });
            Assert.That(resolver.Resolve("Assets/My Scripts/角色.cs"), Is.EqualTo("Assets/My Scripts/角色.cs"));
            Assert.That(resolver.Resolve("Packages/com.example.cache/Source.cs"), Is.EqualTo("Library/PackageCache/com.example.cache@1.0/Source.cs"));
            Assert.That(resolver.Resolve("Packages/com.example.local/Source.cs"), Is.EqualTo(Path.Combine(local, "Source.cs").Replace('\\', '/')));
            Assert.That(resolver.Resolve("Packages/com.example.embedded/Source.cs"), Is.EqualTo("Packages/com.example.embedded/Source.cs"));
            Assert.That(resolver.SourceOf(Path.Combine(local, "Source.cs")), Is.EqualTo(PackageSource.Local));
            Assert.That(resolver.SourceOf("Packages/com.example.cache/Source.cs"), Is.EqualTo(PackageSource.Registry));
            Assert.That(resolver.SourceOf("Assets/Game.cs"), Is.Null);
            Assert.That(resolver.SourceOf("Packages/com.example.cache.other/Source.cs"), Is.EqualTo(PackageSource.Unknown));
            Assert.That(resolver.SourceOf(Path.Combine(root + " other", "File.cs")), Is.Null);
        }

        [Test]
        public void MissingPackageMappingIsReportedInsteadOfEmittingAnInvalidPath()
        {
            var resolver = new ProjectPathResolver(Path.GetTempPath(), Array.Empty<ProjectPackage>());
            Assert.Throws<IOException>(() => resolver.Resolve("Packages/" + Guid.NewGuid().ToString("N") + "/Source.cs"));
        }

        [Test]
        public void MapsAbsoluteLogicalPathsAndKeepsAssemblyOutputsRelative()
        {
            var root = Path.Combine(Path.GetTempPath(), "Game");
            var resolver = new ProjectPathResolver(root, new[]
            {
                new ProjectPackage { AssetPath = "Packages/com.test", ResolvedPath = Path.Combine(root, "Library/PackageCache/com.test@1"), Source = PackageSource.Registry }
            });
            Assert.That(resolver.Resolve(Path.Combine(root, "Packages/com.test/Code.cs")), Is.EqualTo("Library/PackageCache/com.test@1/Code.cs"));
            Assert.That(resolver.Resolve("Library/ScriptAssemblies/Game.dll"), Is.EqualTo("Library/ScriptAssemblies/Game.dll"));
            Assert.That(resolver.SourceOf(Path.Combine(root, "Library/PackageCache/com.test@1/Code.cs")), Is.EqualTo(PackageSource.Registry));
        }

        [Test]
        public void AssetsRemainIncludedWhenALocalPackagePointsInsideAssets()
        {
            var root = Path.Combine(Path.GetTempPath(), "Game");
            var resolver = new ProjectPathResolver(root, new[]
            {
                new ProjectPackage { AssetPath = "Packages/com.local", ResolvedPath = Path.Combine(root, "Assets/Shared"), Source = PackageSource.Local }
            });
            Assert.That(resolver.SourceOf("Assets/Shared/Code.cs"), Is.Null);
            Assert.That(resolver.SourceOf("Packages/com.local/Code.cs"), Is.EqualTo(PackageSource.Local));
        }
    }

    public class ProjectCompilerOptionsTests
    {
        [Test]
        public void CarriesResponseFileOptionsIntoTheDesignTimeModel()
        {
            var model = new ProjectAssembly();
            var paths = new ProjectPathResolver(Path.GetTempPath(), Array.Empty<ProjectPackage>());
            ProjectCompilerOptions.Apply(model, new[]
            {
                "/define:FIRST;SECOND", "-langversion:8.0", "-unsafe+", "-nullable:enable", "-checked+", "-warn:3",
                "-nowarn:0169,0649", "-warnaserror+", "-warnaserror:CS0219", "-warnaserror-:CS0168",
                "-analyzer:\"Analyzers/My Generator.dll\"", "-additionalfile:Assets/Input.txt",
                "-ruleset:Assets/Rules.ruleset", "-analyzerconfig:Assets/Global.config", "-r:external=\"Plugins/My Library.dll\""
            }, paths);
            Assert.That(model.Defines, Is.EqualTo(new[] { "FIRST", "SECOND" }));
            Assert.That(model.LanguageVersion, Is.EqualTo("8.0"));
            Assert.That(model.AllowUnsafe, Is.True);
            Assert.That(model.Nullable, Is.EqualTo("enable"));
            Assert.That(model.CheckOverflow, Is.EqualTo("true"));
            Assert.That(model.WarningLevel, Is.EqualTo("3"));
            Assert.That(model.NoWarn, Is.EqualTo(new[] { "0169", "0649" }));
            Assert.That(model.TreatWarningsAsErrors, Is.EqualTo("true"));
            Assert.That(model.WarningsAsErrors, Is.EqualTo(new[] { "CS0219" }));
            Assert.That(model.WarningsNotAsErrors, Is.EqualTo(new[] { "CS0168" }));
            Assert.That(model.Analyzers, Is.EqualTo(new[] { "Analyzers/My Generator.dll" }));
            Assert.That(model.AdditionalFiles, Is.EqualTo(new[] { "Assets/Input.txt" }));
            Assert.That(model.Ruleset, Is.EqualTo("Assets/Rules.ruleset"));
            Assert.That(model.AnalyzerConfig, Is.EqualTo("Assets/Global.config"));
            Assert.That(model.BinaryReferences[0].Path, Is.EqualTo("Plugins/My Library.dll"));
            Assert.That(model.BinaryReferences[0].Aliases, Is.EqualTo("external"));
            ProjectCompilerOptions.Apply(model, new[] { "-unsafe-", "-checked-", "-warnaserror-" }, paths);
            Assert.That(model.AllowUnsafe, Is.False);
            Assert.That(model.CheckOverflow, Is.EqualTo("false"));
            Assert.That(model.TreatWarningsAsErrors, Is.EqualTo("false"));
        }
    }
}
