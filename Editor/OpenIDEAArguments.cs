using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace OpenIDEA.Editor
{
    internal static class OpenIDEAArguments
    {
        internal const string DefaultTemplate = "$(ProjectPath) --file $(File) --line $(Line) --column $(Column)";
        private static readonly Regex Placeholder = new Regex(@"\$\((ProjectPath|File|Line|Column)\)");

        internal static string NormalizeTemplate(string template)
        {
            if (string.IsNullOrWhiteSpace(template))
                return DefaultTemplate;

            foreach (var name in new[] { "ProjectPath", "File", "Line", "Column" })
            {
                var token = "$(" + name + ")";
                template = template.Replace("\"" + token + "\"", token).Replace("'" + token + "'", token);
            }

            return template;
        }

        internal static string Expand(string template, string project, string file, int line, int column, bool windows)
        {
            if (string.IsNullOrEmpty(file))
                return Quote(project, windows);

            // Substitute in one pass: a path may itself contain text such as $(File).
            return Placeholder.Replace(NormalizeTemplate(template), match =>
            {
                switch (match.Groups[1].Value)
                {
                    case "ProjectPath": return Quote(project, windows);
                    case "File": return Quote(file, windows);
                    case "Line": return Math.Max(0, line).ToString(CultureInfo.InvariantCulture);
                    default: return Math.Max(0, column).ToString(CultureInfo.InvariantCulture);
                }
            });
        }

        internal static string Quote(string value, bool windows)
        {
            if (!windows)
                return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

            var result = new StringBuilder("\"");
            var backslashes = 0;
            foreach (var character in value)
            {
                if (character == '\\')
                {
                    backslashes++;
                    continue;
                }

                // Windows argv parsing consumes escaped quotes and trailing backslashes.
                result.Append('\\', character == '"' ? backslashes * 2 + 1 : backslashes);
                result.Append(character);
                backslashes = 0;
            }

            result.Append('\\', backslashes * 2);
            result.Append('"');
            return result.ToString();
        }
    }
}
