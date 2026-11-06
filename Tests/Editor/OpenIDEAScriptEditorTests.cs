using System;
using System.IO;
using NUnit.Framework;
using Unity.CodeEditor;
using UnityEngine;

namespace OpenIDEA.Editor.Tests
{
    public class OpenIDEAScriptEditorTests
    {
        [Test]
        public void UnityRecognizesABrowsedPathWithoutChangingTheSelectedEditor()
        {
            var root = Path.Combine(Path.GetTempPath(), "OpenIDEA.EditorTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            var path = Path.Combine(root, Application.platform == RuntimePlatform.WindowsEditor ? "openidea.exe" : "openidea");
            File.WriteAllText(path, string.Empty);
            var selected = CodeEditor.CurrentEditorPath;
            try
            {
                var editor = new OpenIDEAScriptEditor(new OpenIDEADiscovery());
                Assert.That(editor.TryGetInstallationForPath(path, out var installation), Is.True);
                Assert.That(installation.Path, Is.EqualTo(path));
                Assert.That(CodeEditor.Editor.GetCodeEditorForPath(path), Is.TypeOf<OpenIDEAScriptEditor>());
                Assert.That(CodeEditor.Editor.GetInstallationForPath(path).Name, Is.EqualTo("OpenIDEA"));
                Assert.That(CodeEditor.CurrentEditorPath, Is.EqualTo(selected));
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }
    }
}
