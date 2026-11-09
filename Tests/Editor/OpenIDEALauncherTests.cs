using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace OpenIDEA.Editor.Tests
{
    public class OpenIDEALauncherTests
    {
        private string _root;
        private string _project;
        private string _editor;
        private ProcessStartInfo _started;
        private List<string> _errors;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "OpenIDEA.LaunchTests", Guid.NewGuid().ToString("N"));
            _project = Path.Combine(_root, "Game with 空格");
            Directory.CreateDirectory(_project);
            _editor = CreateFile("IDE with spaces/openidea.exe");
            _errors = new List<string>();
            _started = null;
        }

        [TearDown]
        public void TearDown()
        {
            Directory.Delete(_root, true);
        }

        private string CreateFile(string path)
        {
            path = Path.GetFullPath(Path.Combine(_root, path));
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, string.Empty);
            return path;
        }

        private OpenIDEALauncher Launcher(RuntimePlatform platform = RuntimePlatform.WindowsEditor,
            Func<ProcessStartInfo, bool> start = null)
        {
            var discovery = new OpenIDEADiscovery(platform, () => Array.Empty<string>(), () => string.Empty);
            return new OpenIDEALauncher(discovery, platform, _errors.Add, start ?? (info =>
            {
                _started = info;
                return true;
            }));
        }

        private bool Open(OpenIDEALauncher launcher, string file = "", string[] extensions = null)
        {
            return launcher.Open(_editor, _project, file, 42, 7, OpenIDEAArguments.DefaultTemplate, extensions ?? new[] { "cs" });
        }

        [Test]
        public void ConsoleLocationLaunchesSelectedEditorWithProjectAndExactCoordinates()
        {
            var file = CreateFile("Game with 空格/Assets/Game Scripts/角色.cs");
            Assert.That(Open(Launcher(), "Assets/Game Scripts/角色.cs"), Is.True);
            Assert.That(_started.FileName, Is.EqualTo(_editor));
            Assert.That(_started.WorkingDirectory, Is.EqualTo(_project));
            Assert.That(_started.UseShellExecute, Is.False);
            Assert.That(_started.CreateNoWindow, Is.True);
            Assert.That(OpenIDEAArgumentsTests.ParseWindowsArguments(_started.Arguments),
                Is.EqualTo(new[] { _project, "--file", file, "--line", "42", "--column", "7" }));
            Assert.That(_errors, Is.Empty);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(".")]
        public void OpeningOnlyTheProjectDoesNotSendAnEmptyFileArgument(string file)
        {
            Assert.That(Open(Launcher(), file), Is.True);
            Assert.That(OpenIDEAArgumentsTests.ParseWindowsArguments(_started.Arguments), Is.EqualTo(new[] { _project }));
        }

        [Test]
        public void AbsoluteProjectDirectoryAlsoOpensWithoutFileArguments()
        {
            Assert.That(Open(Launcher(), _project + Path.DirectorySeparatorChar), Is.True);
            Assert.That(OpenIDEAArgumentsTests.ParseWindowsArguments(_started.Arguments), Is.EqualTo(new[] { _project }));
        }

        [Test]
        public void SourceOutsideTheProjectKeepsTheExplicitUnityProject()
        {
            var file = CreateFile("external package/Source.cs");
            Assert.That(Open(Launcher(), file), Is.True);
            Assert.That(OpenIDEAArgumentsTests.ParseWindowsArguments(_started.Arguments),
                Is.EqualTo(new[] { _project, "--file", file, "--line", "42", "--column", "7" }));
        }

        [TestCase("Scene.unity")]
        [TestCase("Image.png")]
        [TestCase("Prefab.prefab")]
        [TestCase("Source.cs.meta")]
        public void UnhandledAssetsAreLeftToUnity(string name)
        {
            Assert.That(Open(Launcher(), CreateFile("Game with 空格/Assets/" + name)), Is.False);
            Assert.That(_started, Is.Null);
            Assert.That(_errors, Is.Empty);
        }

        [Test]
        public void MissingScriptIsNotReportedAsOpened()
        {
            Assert.That(Open(Launcher(), "Assets/Missing.cs"), Is.False);
            Assert.That(_started, Is.Null);
        }

        [TestCase("*.LUA")]
        [TestCase(" .lua ")]
        public void CustomExtensionsAreNormalized(string extension)
        {
            Assert.That(Open(Launcher(), CreateFile("Game with 空格/Assets/Test.lua"), new[] { extension }), Is.True);
        }

        [Test]
        public void MissingEditorProducesAnActionableErrorWithoutStartingAProcess()
        {
            _editor = Path.Combine(_root, "missing", "openidea.exe");
            Assert.That(Open(Launcher()), Is.False);
            Assert.That(_started, Is.Null);
            Assert.That(_errors, Has.Count.EqualTo(1));
            Assert.That(_errors[0], Does.Contain("External Tools"));
        }

        [Test]
        public void ProcessStartFailureReturnsFalseAndReportsTheReason()
        {
            Assert.That(Open(Launcher(start: _ => throw new Win32Exception("Launch denied"))), Is.False);
            Assert.That(_errors, Has.Count.EqualTo(1));
            Assert.That(_errors[0], Does.Contain("Launch denied"));
        }

        [Test]
        public void NoProcessIsNotReportedAsSuccess()
        {
            Assert.That(Open(Launcher(start: _ => false)), Is.False);
            Assert.That(_errors, Has.Count.EqualTo(1));
        }

        [Test]
        public void MacBundleLaunchDeliversArgumentsToTheSingleInstance()
        {
            CreateFile("OpenIDEA.app/Contents/MacOS/openidea");
            _editor = Path.Combine(_root, "OpenIDEA.app");
            var file = CreateFile("Game with 空格/Assets/Test.cs");
            Assert.That(Open(Launcher(RuntimePlatform.OSXEditor), file), Is.True);
            Assert.That(_started.FileName, Is.EqualTo("/usr/bin/open"));
            Assert.That(_started.Arguments, Does.StartWith("-n -a " + OpenIDEAArguments.Quote(_editor, false) + " --args "));
            Assert.That(_started.Arguments, Does.Contain("--file " + OpenIDEAArguments.Quote(file, false)));
        }

        [Test]
        public void ProjectPreparationRunsBeforeTheEditorProcessStarts()
        {
            var prepared = false;
            var launcher = Launcher(start: _ =>
            {
                Assert.That(prepared, Is.True);
                return true;
            });
            Assert.That(launcher.Open(_editor, _project, "", 0, 0, OpenIDEAArguments.DefaultTemplate,
                new[] { "cs" }, () => prepared = true), Is.True);
        }

        [TestCase("Image.png")]
        [TestCase("Missing.cs")]
        public void RejectedFileRequestsDoNotGenerateProjectFiles(string file)
        {
            if (file.EndsWith(".png")) CreateFile(Path.Combine(_project, file));
            Assert.That(Launcher().Open(_editor, _project, file, 0, 0, OpenIDEAArguments.DefaultTemplate,
                new[] { "cs" }, () => Assert.Fail("Rejected requests must not generate project files")), Is.False);
        }

        [Test]
        public void GenerationFailureDoesNotSendTheScriptToTheSystemFileAssociation()
        {
            var errors = new List<Exception>();
            var synchronization = new ProjectSynchronization(() => true, () => false, () => 0,
                () => throw new IOException("Generation failed"), errors.Add);
            Assert.That(Launcher().Open(_editor, _project, "", 0, 0, OpenIDEAArguments.DefaultTemplate,
                new[] { "cs" }, synchronization.EnsureGenerated), Is.True);
            Assert.That(errors, Has.Count.EqualTo(1));
            Assert.That(_started, Is.Not.Null);
        }

        [Test]
        public void LinuxAppImageIsExecutedDirectly()
        {
            _editor = CreateFile("OpenIDEA_0.1.37_amd64.AppImage");
            Assert.That(Open(Launcher(RuntimePlatform.LinuxEditor)), Is.True);
            Assert.That(_started.FileName, Is.EqualTo(_editor));
            Assert.That(_started.UseShellExecute, Is.False);
        }
    }
}
