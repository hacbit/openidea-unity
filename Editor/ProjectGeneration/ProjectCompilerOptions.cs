using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace OpenIDEA.Editor
{
    internal static class ProjectCompilerOptions
    {
        internal static void Apply(ProjectAssembly assembly, IEnumerable<string> arguments, ProjectPathResolver paths)
        {
            foreach (var argument in arguments ?? Array.Empty<string>())
            {
                var option = argument.Trim().TrimStart('-', '/');
                var colon = option.IndexOf(':');
                var name = (colon < 0 ? option : option.Substring(0, colon)).ToLowerInvariant();
                var value = colon < 0 ? string.Empty : option.Substring(colon + 1).Trim().Trim('"');
                switch (name)
                {
                    case "define": case "d": assembly.Defines.AddRange(List(value)); break;
                    case "langversion": assembly.LanguageVersion = value; break;
                    case "unsafe": case "unsafe+": assembly.AllowUnsafe = true; break;
                    case "unsafe-": assembly.AllowUnsafe = false; break;
                    case "nullable": assembly.Nullable = value; break;
                    case "checked": case "checked+": assembly.CheckOverflow = "true"; break;
                    case "checked-": assembly.CheckOverflow = "false"; break;
                    case "warn": assembly.WarningLevel = value; break;
                    case "nowarn": assembly.NoWarn.AddRange(List(value)); break;
                    case "warnaserror": case "warnaserror+":
                        if (value.Length == 0) assembly.TreatWarningsAsErrors = "true";
                        else assembly.WarningsAsErrors.AddRange(List(value));
                        break;
                    case "warnaserror-":
                        if (value.Length == 0) assembly.TreatWarningsAsErrors = "false";
                        else assembly.WarningsNotAsErrors.AddRange(List(value));
                        break;
                    case "analyzer": case "a": assembly.Analyzers.AddRange(Paths(value, paths)); break;
                    case "additionalfile": assembly.AdditionalFiles.AddRange(Paths(value, paths)); break;
                    case "ruleset": assembly.Ruleset = paths.Resolve(value); break;
                    case "analyzerconfig": assembly.AnalyzerConfig = paths.Resolve(value); break;
                    case "reference": case "r":
                        var equals = value.IndexOf('=');
                        var aliases = "global";
                        if (equals > 0 && Regex.IsMatch(value.Substring(0, equals), @"^\w+(,\w+)*$"))
                        {
                            aliases = value.Substring(0, equals);
                            value = value.Substring(equals + 1).Trim('"');
                        }
                        assembly.BinaryReferences.Add(new ProjectBinaryReference(paths.Resolve(value), aliases));
                        break;
                }
            }
        }

        private static IEnumerable<string> List(string value) => value.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(v => v.Trim()).Where(v => v.Length != 0);

        private static IEnumerable<string> Paths(string value, ProjectPathResolver paths) =>
            value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(v => paths.Resolve(v.Trim().Trim('"')));
    }
}
