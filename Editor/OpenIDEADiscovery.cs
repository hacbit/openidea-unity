using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using Unity.CodeEditor;
using UnityEngine;
#if UNITY_EDITOR_WIN
using Microsoft.Win32;
#endif

namespace OpenIDEA.Editor
{
    public interface IDiscovery
    {
        CodeEditor.Installation[] PathCallback();
        bool TryGetInstallationForPath(string editorPath, out CodeEditor.Installation installation);
    }

    public sealed class OpenIDEADiscovery : IDiscovery
    {
        private readonly RuntimePlatform _platform;
        private readonly Func<IEnumerable<string>> _candidatePaths;
        private readonly Func<string> _selectedPath;

        public OpenIDEADiscovery()
            : this(Application.platform,
                () => GetCandidatePaths(Application.platform, Environment.GetEnvironmentVariable),
                () => CodeEditor.CurrentEditorPath)
        {
        }

        internal OpenIDEADiscovery(RuntimePlatform platform, Func<IEnumerable<string>> candidatePaths,
            Func<string> selectedPath)
        {
            _platform = platform;
            _candidatePaths = candidatePaths ?? throw new ArgumentNullException(nameof(candidatePaths));
            _selectedPath = selectedPath ?? throw new ArgumentNullException(nameof(selectedPath));
        }

        public CodeEditor.Installation[] PathCallback()
        {
            var installations = new List<CodeEditor.Installation>();
            var paths = new HashSet<string>(_platform == RuntimePlatform.WindowsEditor
                ? StringComparer.OrdinalIgnoreCase
                : StringComparer.Ordinal);

            foreach (var candidate in _candidatePaths())
                AddInstallation(candidate, paths, installations);

            // Unity persists paths chosen through External Script Editor > Browse.
            AddInstallation(_selectedPath(), paths, installations);

            if (installations.Count > 1)
            {
                for (var i = 0; i < installations.Count; i++)
                {
                    var installation = installations[i];
                    installation.Name = $"OpenIDEA ({installation.Path})";
                    installations[i] = installation;
                }
            }

            return installations.ToArray();
        }

        private void AddInstallation(string path, HashSet<string> paths, List<CodeEditor.Installation> installations)
        {
            if (TryGetInstallationForPath(path, out var installation) && paths.Add(installation.Path))
                installations.Add(installation);
        }

        public bool TryGetInstallationForPath(string editorPath, out CodeEditor.Installation installation)
        {
            installation = default;
            if (string.IsNullOrWhiteSpace(editorPath))
                return false;

            try
            {
                var path = editorPath.Trim().Trim('"');
                if (!Path.IsPathRooted(path))
                    return false;

                path = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var name = Path.GetFileName(path);
                bool exists;
                switch (_platform)
                {
                    case RuntimePlatform.WindowsEditor:
                        exists = name.Equals("openidea.exe", StringComparison.OrdinalIgnoreCase) && File.Exists(path);
                        break;
                    case RuntimePlatform.OSXEditor:
                        exists = name.Equals("OpenIDEA.app", StringComparison.OrdinalIgnoreCase)
                            ? Directory.Exists(path) && File.Exists(Path.Combine(path, "Contents/MacOS/openidea"))
                            : name == "openidea" && File.Exists(path);
                        break;
                    case RuntimePlatform.LinuxEditor:
                        exists = (name == "openidea" || IsAppImage(name)) && File.Exists(path);
                        break;
                    default:
                        return false;
                }

                if (!exists)
                    return false;

                installation = new CodeEditor.Installation { Name = "OpenIDEA", Path = path };
                return true;
            }
            catch (ArgumentException) { return false; }
            catch (IOException) { return false; }
            catch (NotSupportedException) { return false; }
            catch (SecurityException) { return false; }
        }

        private static bool IsAppImage(string name)
        {
            return name.Equals("OpenIDEA.AppImage", StringComparison.OrdinalIgnoreCase)
                || (name.StartsWith("OpenIDEA_", StringComparison.OrdinalIgnoreCase)
                    && name.EndsWith(".AppImage", StringComparison.OrdinalIgnoreCase));
        }

        internal static IEnumerable<string> GetCandidatePaths(RuntimePlatform platform, Func<string, string> environment)
        {
            var home = environment("HOME") ?? environment("USERPROFILE");
            switch (platform)
            {
                case RuntimePlatform.WindowsEditor:
                    yield return Combine(environment("LOCALAPPDATA"), "OpenIDEA/openidea.exe");
                    yield return Combine(environment("ProgramFiles"), "OpenIDEA/openidea.exe");
                    yield return Combine(environment("ProgramFiles(x86)"), "OpenIDEA/openidea.exe");
                    foreach (var path in GetWindowsRegistryPaths())
                        yield return path;
                    break;
                case RuntimePlatform.OSXEditor:
                    yield return "/Applications/OpenIDEA.app";
                    yield return Combine(home, "Applications/OpenIDEA.app");
                    break;
                case RuntimePlatform.LinuxEditor:
                    yield return "/usr/bin/openidea";
                    yield return "/usr/local/bin/openidea";
                    yield return "/opt/openidea/openidea";
                    yield return Combine(home, ".local/bin/openidea");
                    foreach (var path in FindAppImages(Combine(home, "Applications")))
                        yield return path;
                    break;
            }

            var pathValue = environment("PATH") ?? string.Empty;
            var separator = platform == RuntimePlatform.WindowsEditor ? ';' : ':';
            var executable = platform == RuntimePlatform.WindowsEditor ? "openidea.exe" : "openidea";
            foreach (var directory in pathValue.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries))
                yield return Combine(directory, executable);
        }

        private static string Combine(string directory, string relativePath)
        {
            return string.IsNullOrWhiteSpace(directory)
                ? null
                : directory.Trim().Trim('"').TrimEnd('/', '\\') + "/" + relativePath;
        }

        private static string[] FindAppImages(string directory)
        {
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
                return Array.Empty<string>();

            try
            {
                return Directory.GetFiles(directory, "OpenIDEA*.AppImage", SearchOption.TopDirectoryOnly);
            }
            catch (IOException) { return Array.Empty<string>(); }
            catch (UnauthorizedAccessException) { return Array.Empty<string>(); }
            catch (SecurityException) { return Array.Empty<string>(); }
        }

        private static IEnumerable<string> GetWindowsRegistryPaths()
        {
            var paths = new List<string>();
#if UNITY_EDITOR_WIN
            foreach (var hive in new[] { RegistryHive.CurrentUser, RegistryHive.LocalMachine })
            foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
            {
                try
                {
                    using (var registry = RegistryKey.OpenBaseKey(hive, view))
                    using (var uninstall = registry.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\OpenIDEA"))
                    using (var appPath = registry.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\App Paths\openidea.exe"))
                    {
                        paths.Add(Combine(uninstall?.GetValue("InstallLocation") as string, "openidea.exe"));
                        paths.Add(appPath?.GetValue(string.Empty) as string);
                    }
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
                catch (SecurityException) { }
            }
#endif
            return paths;
        }
    }
}
