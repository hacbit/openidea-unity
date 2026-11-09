using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace OpenIDEA.Editor.Tests
{
    public class ProjectSynchronizationTests
    {
        private string _root;
        private bool _selected;
        private bool _busy;
        private double _time;
        private int _runs;
        private Action _duringGeneration;
        private List<Exception> _errors;
        private ProjectSynchronization _synchronization;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "OpenIDEA.SynchronizationTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
            _selected = true;
            _busy = false;
            _time = 0;
            _runs = 0;
            _duringGeneration = null;
            _errors = new List<Exception>();
            _synchronization = new ProjectSynchronization(() => _selected, () => _busy, () => _time, () =>
            {
                _runs++;
                _duringGeneration?.Invoke();
                var assembly = new ProjectAssembly { Name = "Game", GenerateProject = true };
                assembly.Sources.Add("Assets/Game.cs");
                return OpenIDEAProjectGeneration.Write(_root, new[] { assembly }).GeneratedFiles;
            }, _errors.Add);
        }

        [TearDown]
        public void TearDown() => Directory.Delete(_root, true);

        [Test]
        public void ConsecutiveNotificationsGenerateOnceAfterTheLastChangeSettles()
        {
            _synchronization.Request();
            _time = 0.3;
            _synchronization.Request();
            _time = 0.7;
            _synchronization.Update();
            Assert.That(_runs, Is.Zero);
            _time = 0.9;
            _synchronization.Update();
            _time = 100;
            _synchronization.Update();
            Assert.That(_runs, Is.EqualTo(1));
        }

        [Test]
        public void ImportOrCompilationDefersWorkUntilTheEditorHasSettled()
        {
            _busy = true;
            _synchronization.Request();
            _time = 10;
            _synchronization.Update();
            _busy = false;
            _time = 10.1;
            _synchronization.Update();
            Assert.That(_runs, Is.Zero);
            _time = 11;
            _synchronization.Update();
            Assert.That(_runs, Is.EqualTo(1));
        }

        [Test]
        public void ManualRegenerationRunsImmediatelyAndConsumesQueuedWork()
        {
            _synchronization.Request();
            _synchronization.SyncNow();
            Assert.That(_runs, Is.EqualTo(1));
            _time = 10;
            _synchronization.Update();
            Assert.That(_runs, Is.EqualTo(1));
        }

        [Test]
        public void ManualRegenerationWhileBusyIsQueuedWithoutReportingAnError()
        {
            _busy = true;
            _synchronization.SyncNow();
            Assert.That(_runs, Is.Zero);
            Assert.That(_errors, Is.Empty);
            _busy = false;
            _time = 1;
            _synchronization.Update();
            Assert.That(_runs, Is.EqualTo(1));
        }

        [Test]
        public void OtherSelectedEditorsDoNotGenerateOrReceiveDeferredWrites()
        {
            _synchronization.Request();
            _selected = false;
            _time = 10;
            _synchronization.Update();
            _synchronization.Request();
            _synchronization.SyncNow();
            _synchronization.EnsureGenerated();
            Assert.That(_runs, Is.Zero);
            _selected = true;
            _synchronization.Update();
            Assert.That(_runs, Is.Zero);
            // Unity calls Initialize on the newly selected adapter.
            _synchronization.Request();
            _time = 11;
            _synchronization.Update();
            Assert.That(_runs, Is.EqualTo(1));
        }

        [Test]
        public void FirstOpenGeneratesImmediatelyAndSubsequentOpensReuseTheFiles()
        {
            _synchronization.EnsureGenerated();
            Assert.That(_runs, Is.EqualTo(1));
            Assert.That(Directory.GetFiles(_root), Has.Length.EqualTo(2));
            _synchronization.EnsureGenerated();
            Assert.That(_runs, Is.EqualTo(1));
            _synchronization.Request();
            _synchronization.EnsureGenerated();
            Assert.That(_runs, Is.EqualTo(2));
        }

        [TestCase(".sln")]
        [TestCase(".csproj")]
        public void OpeningRepairsMissingGeneratedFiles(string extension)
        {
            _synchronization.EnsureGenerated();
            var missing = Directory.GetFiles(_root, "*" + extension)[0];
            File.Delete(missing);
            _synchronization.EnsureGenerated();
            Assert.That(_runs, Is.EqualTo(2));
            Assert.That(File.Exists(missing), Is.True);
        }

        [Test]
        public void OpeningDuringCompilationKeepsTheGenerationRequestForLater()
        {
            _busy = true;
            Assert.DoesNotThrow(() => _synchronization.EnsureGenerated());
            Assert.That(_runs, Is.Zero);
            _busy = false;
            _time = 1;
            _synchronization.Update();
            Assert.That(_runs, Is.EqualTo(1));
        }

        [Test]
        public void FailedGenerationReportsOnceAndCanRetryOnTheNextOpen()
        {
            _synchronization.SyncNow();
            _duringGeneration = () => throw new IOException("Files are locked");
            _synchronization.SyncNow();
            for (var i = 0; i < 10; i++)
            {
                _time++;
                _synchronization.Update();
            }
            Assert.That(_runs, Is.EqualTo(2));
            Assert.That(_errors, Has.Count.EqualTo(1));
            _duringGeneration = null;
            _synchronization.EnsureGenerated();
            Assert.That(_runs, Is.EqualTo(3));
        }

        [Test]
        public void RequestsDuringGenerationDoNotReenterOrLoseTheFollowUpPass()
        {
            _duringGeneration = () =>
            {
                _duringGeneration = null;
                _synchronization.SyncNow();
                _synchronization.Update();
                Assert.That(_runs, Is.EqualTo(1));
            };
            _synchronization.SyncNow();
            _time = 1;
            _synchronization.Update();
            Assert.That(_runs, Is.EqualTo(2));
        }

        [Test]
        public void ANewDomainCanRegenerateWithoutRewritingUnchangedFiles()
        {
            _synchronization.SyncNow();
            var project = Path.Combine(_root, "Game.csproj");
            var oldTime = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            File.SetLastWriteTimeUtc(project, oldTime);
            var afterReload = new ProjectSynchronization(() => true, () => false, () => _time,
                () =>
                {
                    var assembly = new ProjectAssembly { Name = "Game", GenerateProject = true };
                    assembly.Sources.Add("Assets/Game.cs");
                    return OpenIDEAProjectGeneration.Write(_root, new[] { assembly }).GeneratedFiles;
                }, _errors.Add);
            afterReload.Request();
            _time = 1;
            afterReload.Update();
            Assert.That(File.GetLastWriteTimeUtc(project), Is.EqualTo(oldTime));
            Assert.That(_errors, Is.Empty);
        }
    }

    public class ProjectGenerationChangesTests
    {
        [TestCase("Assets/Deleted.cs")]
        [TestCase("Assets/OldName.CS")]
        [TestCase("Packages/com.example/Runtime.asmdef")]
        [TestCase("Assets/Scripts.asmref")]
        [TestCase("Assets/csc.rsp")]
        [TestCase("Assets/Plugins/Dependency.dll")]
        [TestCase("Assets/Plugins/Dependency.dll.meta")]
        [TestCase("Assets/Analyzer.ruleset")]
        [TestCase("Assets/.editorconfig")]
        [TestCase("Packages/com.example/Options.globalconfig")]
        [TestCase("Packages/manifest.json")]
        [TestCase("Packages/packages-lock.json")]
        [TestCase("Packages/com.example/package.json")]
        [TestCase("Assets/OldFolder")]
        [TestCase("Packages\\com.example\\Script.cs")]
        public void CompilationInputsAreRelevantEvenWhenTheirFilesNoLongerExist(string path)
        {
            Assert.That(ProjectGenerationChanges.AffectsProjects(new[] { path }), Is.True);
        }

        [TestCase("Assets/Texture.png")]
        [TestCase("Assets/Scene.unity")]
        [TestCase("Assets/Data.json")]
        [TestCase("Assets/Readme.md")]
        [TestCase("Game.csproj")]
        [TestCase("Game.csproj.meta")]
        [TestCase("Game.sln")]
        [TestCase("Game.sln.openidea-123.tmp")]
        [TestCase("")]
        [TestCase(null)]
        public void UnrelatedAssetsAndGeneratedFilesDoNotRequestSynchronization(string path)
        {
            Assert.That(ProjectGenerationChanges.AffectsProjects(new[] { path }), Is.False);
        }

        [Test]
        public void EveryChangeListIncludingMoveOriginsAndImportsIsInspected()
        {
            for (var index = 0; index < 5; index++)
            {
                var changes = new string[5][];
                changes[index] = new[] { "Assets/OldName.cs" };
                Assert.That(ProjectGenerationChanges.AffectsProjects(changes), Is.True);
            }
            Assert.That(ProjectGenerationChanges.AffectsProjects(null, Array.Empty<string>()), Is.False);
        }
    }
}
