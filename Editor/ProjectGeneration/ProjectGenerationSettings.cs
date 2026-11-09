using System;
using UnityEditor;
using UnityEditor.PackageManager;

namespace OpenIDEA.Editor
{
    [Flags]
    internal enum ProjectGenerationSources
    {
        None = 0,
        Embedded = 1,
        Local = 2,
        Registry = 4,
        Git = 8,
        BuiltIn = 16,
        LocalTarball = 32,
        Unknown = 64
    }

    internal static class ProjectGenerationSettings
    {
        private const string PreferenceKey = "openidea_project_generation_sources";
        internal const ProjectGenerationSources DefaultSources = ProjectGenerationSources.Embedded | ProjectGenerationSources.Local;

        internal static ProjectGenerationSources Sources
        {
            get => (ProjectGenerationSources)EditorPrefs.GetInt(PreferenceKey, (int)DefaultSources);
            set => EditorPrefs.SetInt(PreferenceKey, (int)value);
        }

        internal static bool Includes(ProjectGenerationSources selected, PackageSource? source)
        {
            if (!source.HasValue)
                return true;

            ProjectGenerationSources flag;
            switch (source.Value)
            {
                case PackageSource.Embedded: flag = ProjectGenerationSources.Embedded; break;
                case PackageSource.Local: flag = ProjectGenerationSources.Local; break;
                case PackageSource.Registry: flag = ProjectGenerationSources.Registry; break;
                case PackageSource.Git: flag = ProjectGenerationSources.Git; break;
                case PackageSource.BuiltIn: flag = ProjectGenerationSources.BuiltIn; break;
                case PackageSource.LocalTarball: flag = ProjectGenerationSources.LocalTarball; break;
                default: flag = ProjectGenerationSources.Unknown; break;
            }
            return (selected & flag) != 0;
        }
    }
}
