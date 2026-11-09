using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.PackageManager;

namespace OpenIDEA.Editor
{
    internal sealed class ProjectPackage
    {
        internal string AssetPath;
        internal string ResolvedPath;
        internal PackageSource Source;
    }

    internal sealed class ProjectPathResolver
    {
        private readonly string _root;
        private readonly ProjectPackage[] _logicalPackages;
        private readonly ProjectPackage[] _physicalPackages;
        private readonly Dictionary<string, string> _resolved = new Dictionary<string, string>(Path.DirectorySeparatorChar == '\\'
            ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        private readonly StringComparison _comparison = Path.DirectorySeparatorChar == '\\'
            ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        internal ProjectPathResolver(string root, ProjectPackage[] packages)
        {
            _root = Path.GetFullPath(root);
            packages = packages ?? Array.Empty<ProjectPackage>();
            _logicalPackages = packages.OrderByDescending(p => p.AssetPath.Length).ToArray();
            _physicalPackages = packages.OrderByDescending(p => p.ResolvedPath.Length).ToArray();
        }

        internal PackageSource? SourceOf(string path)
        {
            var absolute = Path.GetFullPath(Path.Combine(_root, path));
            var logical = RelativeToRoot(absolute);
            if (IsWithin(logical, "Assets")) return null;
            var package = FindLogicalPackage(logical) ?? _physicalPackages.FirstOrDefault(p => IsWithin(absolute, p.ResolvedPath));
            return package != null ? package.Source
                : IsWithin(logical, "Packages") ? PackageSource.Unknown : (PackageSource?)null;
        }

        internal string Resolve(string path)
        {
            if (_resolved.TryGetValue(path, out var resolved)) return resolved;
            var absolute = Path.GetFullPath(Path.Combine(_root, path));
            var logical = RelativeToRoot(absolute);
            var package = FindLogicalPackage(logical);
            if (package != null)
            {
                var suffix = logical.Substring(Normalize(package.AssetPath).TrimEnd('/').Length).TrimStart('/');
                absolute = Path.GetFullPath(Path.Combine(package.ResolvedPath, suffix));
            }
            else if (IsWithin(logical, "Packages") && !File.Exists(absolute))
            {
                throw new IOException("Cannot resolve package path: " + path);
            }
            resolved = RelativeToRoot(absolute);
            _resolved.Add(path, resolved);
            return resolved;
        }

        private ProjectPackage FindLogicalPackage(string logical)
        {
            return IsWithin(logical, "Packages")
                ? _logicalPackages.FirstOrDefault(p => IsWithin(logical, p.AssetPath))
                : null;
        }

        private string RelativeToRoot(string absolute)
        {
            return IsWithin(absolute, _root)
                ? Normalize(absolute).Substring(Normalize(_root).TrimEnd('/').Length).TrimStart('/')
                : Normalize(absolute);
        }

        private bool IsWithin(string path, string directory)
        {
            path = Normalize(path);
            directory = Normalize(directory).TrimEnd('/');
            return path.Equals(directory, _comparison) || path.StartsWith(directory + "/", _comparison);
        }

        private static string Normalize(string path) => path.Replace('\\', '/');
    }
}
