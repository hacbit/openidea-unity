using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OpenIDEA.Editor
{
    internal static class ProjectGenerationChanges
    {
        private static readonly HashSet<string> CompilationExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".cs", ".asmdef", ".asmref", ".rsp", ".dll", ".ruleset", ".globalconfig"
        };

        internal static bool AffectsProjects(params string[][] changes)
        {
            return changes.Any(files => files != null && files.Any(IsRelevant));
        }

        private static bool IsRelevant(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            path = path.Replace('\\', '/').TrimEnd('/');
            if (path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                path = path.Substring(0, path.Length - ".meta".Length);

            if (CompilationExtensions.Contains(Path.GetExtension(path))) return true;
            if (string.Equals(Path.GetFileName(path), ".editorconfig", StringComparison.OrdinalIgnoreCase)) return true;
            if (path.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase)
                && (path.EndsWith("/package.json", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(path, "Packages/manifest.json", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(path, "Packages/packages-lock.json", StringComparison.OrdinalIgnoreCase))) return true;

            // Folder notifications can stand in for a batch of script moves or deletions.
            return string.IsNullOrEmpty(Path.GetExtension(path))
                && (path == "Assets" || path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase)
                    || path == "Packages" || path.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase));
        }
    }
}
