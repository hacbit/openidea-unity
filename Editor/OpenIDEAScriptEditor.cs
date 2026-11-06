using System;
using UnityEditor;
using UnityEngine;
using Unity.CodeEditor;

namespace OpenIDEA.Editor
{
    [InitializeOnLoad]
    public sealed class OpenIDEAScriptEditor : IExternalCodeEditor
    {
        private const string OPENIDEA_ARGUMENT = "openidea_arguments";
        private static readonly GUIContent _resetArguments = EditorGUIUtility.TrTextContent("Reset argument");
        private readonly IDiscovery _discovery;

        private static string DefaultArgument { get; } = "\"$(ProjectPath)\" --file \"$(File)\" --line $(Line) --column $(Column)";

        private string _arguments;
        private string Arguments
        {
            get => _arguments ?? (_arguments = EditorPrefs.GetString(OPENIDEA_ARGUMENT, DefaultArgument));
            set
            {
                _arguments = value;
                EditorPrefs.SetString(OPENIDEA_ARGUMENT, value);
            }
        }

        public CodeEditor.Installation[] Installations => _discovery.PathCallback();

        static OpenIDEAScriptEditor()
        {
            CodeEditor.Register(new OpenIDEAScriptEditor(new OpenIDEADiscovery()));
        }

        public OpenIDEAScriptEditor(IDiscovery discovery)
        {
            _discovery = discovery ?? throw new ArgumentNullException(nameof(discovery));
        }

        public void Initialize(string editorInstallationPath)
        {
        }

        public void OnGUI()
        {
            EditorGUILayout.HelpBox("OpenIDEA discovery is available. Opening scripts and generating project files are not available in this version.", MessageType.Info);
            using (new EditorGUI.DisabledScope(true))
            {
                Arguments = EditorGUILayout.TextField("External Script Editor Args", Arguments);
                if (GUILayout.Button(_resetArguments, GUILayout.Width(120)))
                {
                    Arguments = DefaultArgument;
                }
            }
        }

        public bool OpenProject(string filePath = "", int line = -1, int column = -1)
        {
            return false;
        }

        public void SyncAll()
        {
        }

        public void SyncIfNeeded(string[] addedFiles, string[] deletedFiles, string[] movedFiles, string[] movedFromFiles, string[] importedFiles)
        {
        }

        public bool TryGetInstallationForPath(string editorPath, out CodeEditor.Installation installation)
        {
            return _discovery.TryGetInstallationForPath(editorPath, out installation);
        }
    }
}
