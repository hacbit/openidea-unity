using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Security;
using UnityEngine;

namespace OpenIDEA.Editor
{
    internal sealed class OpenIDEALauncher
    {
        private readonly IDiscovery _discovery;
        private readonly RuntimePlatform _platform;
        private readonly Func<ProcessStartInfo, bool> _start;
        private readonly Action<string> _reportError;

        internal OpenIDEALauncher(IDiscovery discovery, RuntimePlatform platform, Action<string> reportError,
            Func<ProcessStartInfo, bool> start = null)
        {
            _discovery = discovery ?? throw new ArgumentNullException(nameof(discovery));
            _platform = platform;
            _reportError = reportError ?? throw new ArgumentNullException(nameof(reportError));
            _start = start ?? StartProcess;
        }

        internal bool Open(string editorPath, string projectDirectory, string filePath, int line, int column,
            string arguments, IEnumerable<string> extensions)
        {
            try
            {
                var project = Path.GetFullPath(projectDirectory);
                if (!Directory.Exists(project))
                {
                    _reportError("OpenIDEA: the Unity project directory no longer exists: " + project);
                    return false;
                }

                string file = null;
                if (!string.IsNullOrEmpty(filePath))
                {
                    file = Path.GetFullPath(Path.IsPathRooted(filePath) ? filePath : Path.Combine(project, filePath));
                    var comparison = _platform == RuntimePlatform.WindowsEditor
                        ? StringComparison.OrdinalIgnoreCase
                        : StringComparison.Ordinal;
                    if (string.Equals(file.TrimEnd('/', '\\'), project.TrimEnd('/', '\\'), comparison))
                        file = null;
                    else if (!File.Exists(file) || !SupportsExtension(file, extensions))
                        return false;
                }

                if (!_discovery.TryGetInstallationForPath(editorPath, out var installation))
                {
                    _reportError("OpenIDEA: the selected executable was not found or recognized. Select OpenIDEA in Preferences > External Tools.");
                    return false;
                }

                var windows = _platform == RuntimePlatform.WindowsEditor;
                var startInfo = new ProcessStartInfo
                {
                    FileName = installation.Path,
                    Arguments = OpenIDEAArguments.Expand(arguments, project, file, line, column, windows),
                    WorkingDirectory = project,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                if (_platform == RuntimePlatform.OSXEditor && Directory.Exists(installation.Path))
                {
                    // A new process delivers arguments to Tauri's existing single instance as well.
                    startInfo.FileName = "/usr/bin/open";
                    startInfo.Arguments = "-n -a " + OpenIDEAArguments.Quote(installation.Path, false)
                        + " --args " + startInfo.Arguments;
                }

                if (_start(startInfo))
                    return true;

                _reportError("OpenIDEA: the operating system did not start the selected editor: " + installation.Path);
                return false;
            }
            catch (Exception exception) when (exception is Win32Exception || exception is InvalidOperationException
                || exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException
                || exception is NotSupportedException || exception is SecurityException)
            {
                _reportError("OpenIDEA: could not open the project or script. " + exception.Message);
                return false;
            }
        }

        private static bool SupportsExtension(string file, IEnumerable<string> extensions)
        {
            var extension = Path.GetExtension(file).TrimStart('.');
            if (extension.Length == 0)
                return false;

            foreach (var supported in extensions)
            {
                if (string.Equals(extension, supported.Trim().TrimStart('*', '.'), StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static bool StartProcess(ProcessStartInfo startInfo)
        {
            using (var process = Process.Start(startInfo))
                return process != null;
        }
    }
}
