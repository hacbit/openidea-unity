using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Unity.CodeEditor;

namespace OpenIDEA.Editor
{
    [InitializeOnLoad]
    public sealed class OpenIDEAScriptEditor : IExternalCodeEditor
    {
        private const string OPENIDEA_ARGUMENT = "openidea_arguments";
        private const string OPENIDEA_EXTENSION = "openidea_userExtensions";
        private static readonly GUIContent _resetArguments = EditorGUIUtility.TrTextContent("Reset argument");
        private readonly IDiscovery _discovery;
        private readonly OpenIDEALauncher _launcher;
        private string _generationMessage;
        private MessageType _generationMessageType;

        private static string DefaultArgument => OpenIDEAArguments.DefaultTemplate;

        private string _arguments;
        private string Arguments
        {
            get => _arguments ?? (_arguments = OpenIDEAArguments.NormalizeTemplate(EditorPrefs.GetString(OPENIDEA_ARGUMENT, DefaultArgument)));
            set
            {
                _arguments = value;
                EditorPrefs.SetString(OPENIDEA_ARGUMENT, value);
            }
        }

        private static string HandledExtensionsString
        {
            get => EditorPrefs.GetString(OPENIDEA_EXTENSION, string.Join(";",
                EditorSettings.projectGenerationBuiltinExtensions
                    .Concat(EditorSettings.projectGenerationUserExtensions)
                    .Concat(new[] { "cs", "json", "asmdef", "asmref", "rsp", "log", "txt" })
                    .Distinct(StringComparer.OrdinalIgnoreCase)));
            set => EditorPrefs.SetString(OPENIDEA_EXTENSION, value);
        }

        public CodeEditor.Installation[] Installations => _discovery.PathCallback();

        static OpenIDEAScriptEditor()
        {
            CodeEditor.Register(new OpenIDEAScriptEditor(new OpenIDEADiscovery()));
        }

        public OpenIDEAScriptEditor(IDiscovery discovery)
        {
            _discovery = discovery ?? throw new ArgumentNullException(nameof(discovery));
            _launcher = new OpenIDEALauncher(discovery, Application.platform, message => Debug.LogError(message));
        }

        public void Initialize(string editorInstallationPath)
        {
        }

        public void OnGUI()
        {
            EditorGUI.BeginChangeCheck();
            var arguments = EditorGUILayout.TextField(new GUIContent("External Script Editor Args",
                "File-opening arguments. ProjectPath and File placeholders are quoted automatically."), Arguments);
            if (EditorGUI.EndChangeCheck())
                Arguments = OpenIDEAArguments.NormalizeTemplate(arguments);
            if (GUILayout.Button(_resetArguments, GUILayout.Width(120)))
            {
                Arguments = DefaultArgument;
            }

            EditorGUI.BeginChangeCheck();
            var extensions = EditorGUILayout.TextField("Extensions handled", HandledExtensionsString);
            if (EditorGUI.EndChangeCheck())
                HandledExtensionsString = extensions;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Generate .csproj files for:", EditorStyles.boldLabel);
            var sources = ProjectGenerationSettings.Sources;
            EditorGUI.BeginChangeCheck();
            sources = SourceToggle(sources, ProjectGenerationSources.Embedded, "Embedded packages");
            sources = SourceToggle(sources, ProjectGenerationSources.Local, "Local packages");
            sources = SourceToggle(sources, ProjectGenerationSources.Registry, "Registry packages");
            sources = SourceToggle(sources, ProjectGenerationSources.Git, "Git packages");
            sources = SourceToggle(sources, ProjectGenerationSources.BuiltIn, "Built-in packages");
            sources = SourceToggle(sources, ProjectGenerationSources.LocalTarball, "Local tarball packages");
            sources = SourceToggle(sources, ProjectGenerationSources.Unknown, "Unknown package sources");
            if (EditorGUI.EndChangeCheck())
            {
                ProjectGenerationSettings.Sources = sources;
                _generationMessage = null;
            }
            EditorGUILayout.HelpBox("Assets are always included. Apply these options with Regenerate project files. Automatic synchronization is not available yet.", MessageType.Info);
            using (new EditorGUI.DisabledScope(EditorApplication.isCompiling || EditorApplication.isUpdating))
            {
                if (GUILayout.Button("Regenerate project files"))
                    SyncAll();
            }
            if (!string.IsNullOrEmpty(_generationMessage))
                EditorGUILayout.HelpBox(_generationMessage, _generationMessageType);
        }

        private static ProjectGenerationSources SourceToggle(ProjectGenerationSources selected, ProjectGenerationSources source, string label)
        {
            return EditorGUILayout.Toggle(label, (selected & source) != 0) ? selected | source : selected & ~source;
        }

        public bool OpenProject(string filePath = "", int line = -1, int column = -1)
        {
            return _launcher.Open(CodeEditor.CurrentEditorPath, Directory.GetParent(Application.dataPath).FullName,
                filePath, line, column, Arguments,
                HandledExtensionsString.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
        }

        public void SyncAll()
        {
            try
            {
                if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                    throw new InvalidOperationException("Wait for Unity to finish importing and compiling, then regenerate project files.");
                var result = OpenIDEAProjectGeneration.Generate(Directory.GetParent(Application.dataPath).FullName, ProjectGenerationSettings.Sources);
                _generationMessage = $"Generated {result.ProjectCount} C# projects and {Path.GetFileName(result.SolutionPath)}. Updated {result.ChangedFiles} files.";
                _generationMessageType = MessageType.Info;
            }
            catch (Exception exception)
            {
                _generationMessage = "Project generation failed: " + exception.Message;
                _generationMessageType = MessageType.Error;
                Debug.LogError("OpenIDEA: " + _generationMessage);
            }
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
