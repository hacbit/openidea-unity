using System.Collections.Generic;

namespace OpenIDEA.Editor
{
    internal sealed class ProjectAssembly
    {
        internal string Name;
        internal string OutputPath;
        internal string RootNamespace = string.Empty;
        internal string LanguageVersion = "9.0";
        internal bool GenerateProject;
        internal bool AllowUnsafe;
        internal bool Optimize;
        internal string Nullable;
        internal string CheckOverflow;
        internal string WarningLevel;
        internal string TreatWarningsAsErrors;
        internal string Ruleset;
        internal string AnalyzerConfig;
        internal readonly List<string> Sources = new List<string>();
        internal readonly List<string> Defines = new List<string>();
        internal readonly List<string> AssemblyReferences = new List<string>();
        internal readonly List<ProjectBinaryReference> BinaryReferences = new List<ProjectBinaryReference>();
        internal readonly List<string> Analyzers = new List<string>();
        internal readonly List<string> AdditionalFiles = new List<string>();
        internal readonly List<string> NoWarn = new List<string>();
        internal readonly List<string> WarningsAsErrors = new List<string>();
        internal readonly List<string> WarningsNotAsErrors = new List<string>();
    }

    internal sealed class ProjectBinaryReference
    {
        internal readonly string Path;
        internal readonly string Aliases;

        internal ProjectBinaryReference(string path, string aliases = "global")
        {
            Path = path;
            Aliases = aliases;
        }
    }

    internal sealed class ProjectGenerationResult
    {
        internal readonly string SolutionPath;
        internal readonly int ProjectCount;
        internal readonly int ChangedFiles;

        internal ProjectGenerationResult(string solutionPath, int projectCount, int changedFiles)
        {
            SolutionPath = solutionPath;
            ProjectCount = projectCount;
            ChangedFiles = changedFiles;
        }
    }
}
