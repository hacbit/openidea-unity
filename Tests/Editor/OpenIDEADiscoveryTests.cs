using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace OpenIDEA.Editor.Tests
{
    public class OpenIDEADiscoveryTests
    {
        private string _root;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "OpenIDEA.DiscoveryTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
        }

        [TearDown]
        public void TearDown()
        {
            Directory.Delete(_root, true);
        }

        private string CreateFile(string relativePath)
        {
            var path = Path.Combine(_root, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, string.Empty);
            return Path.GetFullPath(path);
        }

        private static OpenIDEADiscovery Discovery(RuntimePlatform platform, string[] candidates = null,
            Func<string> selectedPath = null)
        {
            return new OpenIDEADiscovery(platform, () => candidates ?? Array.Empty<string>(),
                selectedPath ?? (() => string.Empty));
        }

        [Test]
        public void RecognizesManualDevelopmentBuildOutsideAutomaticLocations()
        {
            var path = CreateFile("My IDE 空间/target/debug/openidea.exe");
            var discovery = Discovery(RuntimePlatform.WindowsEditor);

            Assert.That(discovery.PathCallback(), Is.Empty);
            Assert.That(discovery.TryGetInstallationForPath(path, out var installation), Is.True);
            Assert.That(installation.Path, Is.EqualTo(path));
            Assert.That(installation.Name, Is.EqualTo("OpenIDEA"));
        }

        [TestCase(RuntimePlatform.WindowsEditor, "openidea.exe", true)]
        [TestCase(RuntimePlatform.WindowsEditor, "OpenIDEA.EXE", true)]
        [TestCase(RuntimePlatform.WindowsEditor, "openidea-server.exe", false)]
        [TestCase(RuntimePlatform.WindowsEditor, "OpenIDEA_0.1.37_x64-setup.exe", false)]
        [TestCase(RuntimePlatform.WindowsEditor, "uninstall.exe", false)]
        [TestCase(RuntimePlatform.WindowsEditor, "Code.exe", false)]
        [TestCase(RuntimePlatform.WindowsEditor, "openidea", false)]
        [TestCase(RuntimePlatform.OSXEditor, "openidea", true)]
        [TestCase(RuntimePlatform.LinuxEditor, "openidea", true)]
        [TestCase(RuntimePlatform.LinuxEditor, "OpenIDEA.AppImage", true)]
        [TestCase(RuntimePlatform.LinuxEditor, "OpenIDEA_0.1.37_amd64.AppImage", true)]
        [TestCase(RuntimePlatform.LinuxEditor, "OpenIDEA_0.1.37_aarch64.AppImage", true)]
        [TestCase(RuntimePlatform.LinuxEditor, "openidea.exe", false)]
        [TestCase(RuntimePlatform.LinuxEditor, "openidea-server", false)]
        [TestCase(RuntimePlatform.LinuxEditor, "OpenIDEATools.AppImage", false)]
        public void RecognizesOnlySupportedExecutableNames(RuntimePlatform platform, string filename, bool expected)
        {
            var path = CreateFile(filename);
            Assert.That(Discovery(platform).TryGetInstallationForPath(path, out _), Is.EqualTo(expected));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("openidea.exe")]
        [TestCase("bad\0path/openidea.exe")]
        public void InvalidInputIsNotAnInstallation(string path)
        {
            Assert.That(Discovery(RuntimePlatform.WindowsEditor).TryGetInstallationForPath(path, out _), Is.False);
        }

        [Test]
        public void MissingExecutableAndDirectoryNamedLikeExecutableAreRejected()
        {
            var path = Path.Combine(_root, "openidea.exe");
            var discovery = Discovery(RuntimePlatform.WindowsEditor);
            Assert.That(discovery.TryGetInstallationForPath(path, out _), Is.False);
            Directory.CreateDirectory(path);
            Assert.That(discovery.TryGetInstallationForPath(path, out _), Is.False);
        }

        [Test]
        public void MacBundleMustContainTheOpenIDEAExecutable()
        {
            var bundle = Path.Combine(_root, "OpenIDEA.app");
            Directory.CreateDirectory(bundle);
            var discovery = Discovery(RuntimePlatform.OSXEditor);
            Assert.That(discovery.TryGetInstallationForPath(bundle, out _), Is.False);

            CreateFile("OpenIDEA.app/Contents/MacOS/openidea");
            Assert.That(discovery.TryGetInstallationForPath(bundle + Path.DirectorySeparatorChar, out var installation), Is.True);
            Assert.That(installation.Path, Is.EqualTo(bundle));

            CreateFile("Other.app/Contents/MacOS/openidea");
            Assert.That(discovery.TryGetInstallationForPath(Path.Combine(_root, "Other.app"), out _), Is.False);
        }

        [Test]
        public void NormalizesQuotedPathsAndRemovesDuplicatesIncludingTheSelectedPath()
        {
            var path = CreateFile("My IDE/openidea.exe");
            var equivalentPath = Path.Combine(_root, "My IDE", "..", "My IDE", "openidea.exe");
            var discovery = Discovery(RuntimePlatform.WindowsEditor, new[] { path, "\"" + equivalentPath + "\"" }, () => path);

            var installations = discovery.PathCallback();
            Assert.That(installations, Has.Length.EqualTo(1));
            Assert.That(installations[0].Path, Is.EqualTo(path));
        }

        [Test]
        public void SelectedCustomLocationIsIncludedAndUpdatesWithoutReloading()
        {
            var installed = CreateFile("installed/openidea.exe");
            var custom = CreateFile("custom/openidea.exe");
            var selected = custom;
            var discovery = Discovery(RuntimePlatform.WindowsEditor, new[] { installed }, () => selected);

            var installations = discovery.PathCallback();
            Assert.That(installations.Select(item => item.Path), Is.EquivalentTo(new[] { installed, custom }));
            Assert.That(installations.Select(item => item.Name).Distinct().Count(), Is.EqualTo(2));

            selected = CreateFile("other/Code.exe");
            Assert.That(discovery.PathCallback().Select(item => item.Path), Is.EqualTo(new[] { installed }));
        }

        [Test]
        public void InstallationChangesAreObservedWithoutStaleCaching()
        {
            var path = Path.Combine(_root, "openidea.exe");
            var discovery = Discovery(RuntimePlatform.WindowsEditor, new[] { path });
            Assert.That(discovery.PathCallback(), Is.Empty);
            CreateFile("openidea.exe");
            Assert.That(discovery.PathCallback(), Has.Length.EqualTo(1));
            File.Delete(path);
            Assert.That(discovery.PathCallback(), Is.Empty);
        }

        [Test]
        public void WindowsCandidatesUseTauriLocationsAndPathEntries()
        {
            var environment = new Dictionary<string, string>
            {
                ["LOCALAPPDATA"] = "C:/Users/Test/AppData/Local",
                ["ProgramFiles"] = "C:/Program Files",
                ["ProgramFiles(x86)"] = "C:/Program Files (x86)",
                ["PATH"] = "\"D:/My Tools\";;D:/Tools/bin"
            };
            var candidates = OpenIDEADiscovery.GetCandidatePaths(RuntimePlatform.WindowsEditor,
                key => environment.TryGetValue(key, out var value) ? value : null).ToArray();

            Assert.That(candidates, Does.Contain("C:/Users/Test/AppData/Local/OpenIDEA/openidea.exe"));
            Assert.That(candidates, Does.Contain("C:/Program Files/OpenIDEA/openidea.exe"));
            Assert.That(candidates, Does.Contain("C:/Program Files (x86)/OpenIDEA/openidea.exe"));
            Assert.That(candidates, Does.Contain("D:/My Tools/openidea.exe"));
            Assert.That(candidates, Does.Contain("D:/Tools/bin/openidea.exe"));
            Assert.That(candidates, Does.Not.Contain("openidea.exe"));
        }

        [Test]
        public void LinuxFindsAppImagesOnlyInTheUserApplicationsDirectory()
        {
            var appImage = CreateFile("Applications/OpenIDEA_0.1.37_amd64.AppImage");
            CreateFile("Applications/nested/OpenIDEA_0.1.37_amd64.AppImage");
            CreateFile("Downloads/OpenIDEA_0.1.37_amd64.AppImage");
            var candidates = OpenIDEADiscovery.GetCandidatePaths(RuntimePlatform.LinuxEditor,
                key => key == "HOME" ? _root : null)
                .Where(path => path != null && Path.GetFullPath(path).StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                .ToArray();
            var discovery = Discovery(RuntimePlatform.LinuxEditor, candidates);

            Assert.That(discovery.PathCallback().Select(item => item.Path), Is.EqualTo(new[] { appImage }));
        }
    }
}
