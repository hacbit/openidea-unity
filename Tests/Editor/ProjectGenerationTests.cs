using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using NUnit.Framework;
using UnityEditor.PackageManager;

namespace OpenIDEA.Editor.Tests
{
    public class ProjectGenerationTests
    {
        private string _root;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "OpenIDEA.GenerationTests", Guid.NewGuid().ToString("N"), "Game with 空格");
            Directory.CreateDirectory(_root);
        }

        [TearDown]
        public void TearDown() => Directory.Delete(Directory.GetParent(_root).FullName, true);

        private static ProjectAssembly Assembly(string name, bool selected = true)
        {
            var assembly = new ProjectAssembly { Name = name, OutputPath = "Library/ScriptAssemblies/" + name + ".dll", GenerateProject = selected };
            assembly.Sources.Add("Assets/" + name + ".cs");
            return assembly;
        }

        private XElement Project(string name) => XElement.Load(Path.Combine(_root, name + ".csproj"));
        private static string Property(XElement project, string name) => project.Elements("PropertyGroup").Elements(name).Single().Value;
        private static XElement[] Items(XElement project, string name) => project.Elements("ItemGroup").Elements(name).ToArray();

        [Test]
        public void EmitsSdkProjectsWithExplicitUnityInputsAndIsolatedOutputs()
        {
            var assembly = Assembly("Game");
            assembly.RootNamespace = "Example.Game";
            assembly.Defines.AddRange(new[] { "UNITY_EDITOR", "GAME_FEATURE", "UNITY_EDITOR" });
            assembly.AllowUnsafe = true;
            assembly.BinaryReferences.Add(new ProjectBinaryReference("Unity/UnityEngine.CoreModule.dll"));
            var result = OpenIDEAProjectGeneration.Write(_root, new[] { assembly });
            Assert.That(result.GeneratedFiles, Is.EquivalentTo(new[] { result.SolutionPath, Path.Combine(_root, "Game.csproj") }));
            var project = Project("Game");
            Assert.That(result.ProjectCount, Is.EqualTo(1));
            Assert.That(result.ChangedFiles, Is.EqualTo(2));
            Assert.That(Property(project, "TargetFramework"), Is.EqualTo("netstandard2.1"));
            Assert.That(Property(project, "LangVersion"), Is.EqualTo("9.0"));
            Assert.That(Property(project, "RootNamespace"), Is.EqualTo("Example.Game"));
            Assert.That(Property(project, "DefineConstants"), Is.EqualTo("GAME_FEATURE;UNITY_EDITOR"));
            Assert.That(Property(project, "AllowUnsafeBlocks"), Is.EqualTo("true"));
            Assert.That(Property(project, "EnableDefaultItems"), Is.EqualTo("false"));
            Assert.That(Property(project, "NoStdLib"), Is.EqualTo("true"));
            Assert.That(Property(project, "DisableImplicitFrameworkReferences"), Is.EqualTo("true"));
            Assert.That(Property(project, "GenerateAssemblyInfo"), Is.EqualTo("false"));
            Assert.That(Property(project, "GenerateTargetFrameworkAttribute"), Is.EqualTo("false"));
            Assert.That(Property(project, "BaseIntermediateOutputPath"), Is.EqualTo("Temp/OpenIDEA/obj/Game/"));
            Assert.That(Property(project, "OutputPath"), Does.StartWith("Temp/OpenIDEA/bin/"));
            Assert.That(project.Elements("Import").Select(e => (string)e.Attribute("Project")), Is.EqualTo(new[] { "Sdk.props", "Sdk.targets" }));
            Assert.That(Items(project, "Compile").Select(e => (string)e.Attribute("Include")), Is.EqualTo(assembly.Sources));
            Assert.That(Items(project, "Reference").Single().Element("HintPath").Value, Is.EqualTo("Unity/UnityEngine.CoreModule.dll"));
        }

        [Test]
        public void PackageSelectionSwitchesBetweenSourceAndBinaryDependencies()
        {
            var game = Assembly("Game");
            var package = Assembly("Package", false);
            game.AssemblyReferences.Add(package.Name);
            var result = OpenIDEAProjectGeneration.Write(_root, new[] { game, package });
            Assert.That(Items(Project("Game"), "ProjectReference"), Is.Empty);
            Assert.That(Items(Project("Game"), "Reference").Single().Element("HintPath").Value, Is.EqualTo(package.OutputPath));
            Assert.That(File.Exists(Path.Combine(_root, "Package.csproj")), Is.False);
            Assert.That(File.ReadAllText(result.SolutionPath), Does.Not.Contain("Package.csproj"));

            package.GenerateProject = true;
            OpenIDEAProjectGeneration.Write(_root, new[] { game, package });
            Assert.That(Items(Project("Game"), "ProjectReference").Single().Attribute("Include").Value, Is.EqualTo("Package.csproj"));
            Assert.That(Items(Project("Game"), "Reference"), Is.Empty);
            Assert.That(File.ReadAllText(result.SolutionPath), Does.Contain("Package.csproj"));

            package.GenerateProject = false;
            OpenIDEAProjectGeneration.Write(_root, new[] { game, package });
            Assert.That(File.ReadAllText(result.SolutionPath), Does.Not.Contain("Package.csproj"));
            // This first generation step retains unused files; only solution membership changes.
            Assert.That(File.Exists(Path.Combine(_root, "Package.csproj")), Is.True);
        }

        [Test]
        public void RenderOrderDoesNotChangeFilesTimestampsOrProjectIdentities()
        {
            var game = Assembly("Game");
            game.Sources.AddRange(new[] { "Assets/Z.cs", "Assets/A.cs" });
            var editor = Assembly("Game.Editor");
            editor.AssemblyReferences.Add("Game");
            var result = OpenIDEAProjectGeneration.Write(_root, new[] { editor, game });
            var paths = Directory.GetFiles(_root);
            var timestamp = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            foreach (var path in paths) File.SetLastWriteTimeUtc(path, timestamp);
            var solution = File.ReadAllText(result.SolutionPath);
            game.Sources.Reverse();
            result = OpenIDEAProjectGeneration.Write(_root, new[] { game, editor });
            Assert.That(result.ChangedFiles, Is.Zero);
            Assert.That(paths.Select(File.GetLastWriteTimeUtc), Is.All.EqualTo(timestamp));
            Assert.That(File.ReadAllText(result.SolutionPath), Is.EqualTo(solution));
            var ids = Regex.Matches(solution, @"Game(?:\.Editor)?\.csproj"", ""\{([^}]+)\}").Cast<Match>().Select(m => m.Groups[1].Value).ToArray();
            Assert.That(ids, Has.Length.EqualTo(2));
            Assert.That(ids.Distinct().Count(), Is.EqualTo(2));

            var clone = Path.Combine(_root, "Other checkout");
            Directory.CreateDirectory(clone);
            var copied = OpenIDEAProjectGeneration.Write(clone, new[] { game, editor });
            Assert.That(File.ReadAllText(copied.SolutionPath), Is.EqualTo(solution));
        }

        [Test]
        public void MissingAssemblyDataDoesNotReplaceExistingFiles()
        {
            var path = Path.Combine(_root, "A.csproj");
            File.WriteAllText(path, "old project");
            var first = Assembly("A");
            var second = Assembly("Z");
            second.AssemblyReferences.Add("Missing");
            Assert.Throws<InvalidDataException>(() => OpenIDEAProjectGeneration.Write(_root, new[] { first, second }));
            Assert.That(File.ReadAllText(path), Is.EqualTo("old project"));
            Assert.That(Directory.GetFiles(_root), Has.Length.EqualTo(1));
        }

        [Test]
        public void EmptyCompilationGraphProducesAnEmptySolution()
        {
            var result = OpenIDEAProjectGeneration.Write(_root, Array.Empty<ProjectAssembly>());
            Assert.That(result.ProjectCount, Is.Zero);
            Assert.That(File.ReadAllText(result.SolutionPath), Does.Not.Contain("Project(\""));
        }

        [TestCase("../Outside")]
        [TestCase("Game\"Name")]
        public void InvalidAssemblyNamesCannotEscapeTheOutputDirectory(string name)
        {
            Assert.Throws<InvalidDataException>(() => OpenIDEAProjectGeneration.Write(_root, new[] { Assembly(name) }));
            Assert.That(Directory.GetFiles(_root), Is.Empty);
        }

        [Test]
        public void EscapesXmlAndMsBuildPathsWithoutCreatingGlobsOrProperties()
        {
            var assembly = Assembly("Game");
            assembly.Sources.Clear();
            assembly.Sources.Add("Assets/A&B/角色;$(File)%25.cs");
            OpenIDEAProjectGeneration.Write(_root, new[] { assembly });
            Assert.That(Items(Project("Game"), "Compile").Single().Attribute("Include").Value,
                Is.EqualTo("Assets/A&B/角色%3B%24(File)%2525.cs"));
            Assert.That(File.ReadAllText(Path.Combine(_root, "Game.csproj")), Does.Contain("A&amp;B"));
        }

        [Test]
        public void PreservesAnalyzersAdditionalFilesRulesAndReferenceAliases()
        {
            var assembly = Assembly("Game");
            assembly.Analyzers.AddRange(new[] { "Analyzers/Generator.dll", "Analyzers/Generator.dll" });
            assembly.AdditionalFiles.Add("Assets/Generator Input.txt");
            assembly.Ruleset = "Assets/Rules.ruleset";
            assembly.AnalyzerConfig = "Library/Compiler.config";
            assembly.BinaryReferences.Add(new ProjectBinaryReference("Plugins/External.dll", "external"));
            OpenIDEAProjectGeneration.Write(_root, new[] { assembly });
            var project = Project("Game");
            Assert.That(Items(project, "Analyzer"), Has.Length.EqualTo(1));
            Assert.That(Items(project, "AdditionalFiles").Single().Attribute("Include").Value, Is.EqualTo("Assets/Generator Input.txt"));
            Assert.That(Property(project, "CodeAnalysisRuleSet"), Is.EqualTo(assembly.Ruleset));
            Assert.That(Items(project, "GlobalAnalyzerConfigFiles").Single().Attribute("Include").Value, Is.EqualTo(assembly.AnalyzerConfig));
            Assert.That(Items(project, "Reference").Single().Element("Aliases").Value, Is.EqualTo("external"));
        }

        [Test]
        public void UpdatingAnExistingProjectLeavesNoTemporaryFiles()
        {
            var assembly = Assembly("Game");
            OpenIDEAProjectGeneration.Write(_root, new[] { assembly });
            assembly.Sources.Add("Assets/Another.cs");
            var result = OpenIDEAProjectGeneration.Write(_root, new[] { assembly });
            Assert.That(result.ChangedFiles, Is.EqualTo(1));
            Assert.That(Items(Project("Game"), "Compile"), Has.Length.EqualTo(2));
            Assert.That(Directory.GetFiles(_root, "*.tmp"), Is.Empty);
        }
    }

    public class ProjectGenerationSourceTests
    {
        [Test]
        public void DefaultSelectionIncludesEditablePackagesAndAssets()
        {
            Assert.That(ProjectGenerationSettings.Includes(ProjectGenerationSources.None, null), Is.True);
            Assert.That(ProjectGenerationSettings.Includes(ProjectGenerationSettings.DefaultSources, PackageSource.Embedded), Is.True);
            Assert.That(ProjectGenerationSettings.Includes(ProjectGenerationSettings.DefaultSources, PackageSource.Local), Is.True);
            Assert.That(ProjectGenerationSettings.Includes(ProjectGenerationSettings.DefaultSources, PackageSource.Registry), Is.False);
        }

        [TestCase(PackageSource.Embedded, (int)ProjectGenerationSources.Embedded)]
        [TestCase(PackageSource.Local, (int)ProjectGenerationSources.Local)]
        [TestCase(PackageSource.Registry, (int)ProjectGenerationSources.Registry)]
        [TestCase(PackageSource.Git, (int)ProjectGenerationSources.Git)]
        [TestCase(PackageSource.BuiltIn, (int)ProjectGenerationSources.BuiltIn)]
        [TestCase(PackageSource.LocalTarball, (int)ProjectGenerationSources.LocalTarball)]
        [TestCase(PackageSource.Unknown, (int)ProjectGenerationSources.Unknown)]
        public void EachSourceCanBeEnabledIndependently(PackageSource source, int flag)
        {
            Assert.That(ProjectGenerationSettings.Includes(ProjectGenerationSources.None, source), Is.False);
            Assert.That(ProjectGenerationSettings.Includes((ProjectGenerationSources)flag, source), Is.True);
            Assert.That(ProjectGenerationSettings.Includes((ProjectGenerationSources)(127 ^ flag), source), Is.False);
        }
    }
}
