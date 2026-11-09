using System;
using System.Runtime.InteropServices;
using NUnit.Framework;

namespace OpenIDEA.Editor.Tests
{
    public class OpenIDEAArgumentsTests
    {
        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr CommandLineToArgvW(string commandLine, out int count);

        [DllImport("kernel32.dll")]
        private static extern IntPtr LocalFree(IntPtr memory);

        internal static string[] ParseWindowsArguments(string arguments)
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
                Assert.Ignore("This argv round-trip uses the Windows command-line parser.");

            var memory = CommandLineToArgvW("openidea.exe " + arguments, out var count);
            Assert.That(memory, Is.Not.EqualTo(IntPtr.Zero));
            try
            {
                var result = new string[count - 1];
                for (var i = 1; i < count; i++)
                    result[i - 1] = Marshal.PtrToStringUni(Marshal.ReadIntPtr(memory, i * IntPtr.Size));
                return result;
            }
            finally
            {
                LocalFree(memory);
            }
        }

        [TestCase("")]
        [TestCase(@"C:\Game With Spaces\")]
        [TestCase(@"C:\项目\角色.cs")]
        [TestCase("text with \"quotes\" and \\\"escapes")]
        [TestCase("literal $(File) & %PATH%")]
        public void WindowsArgumentsRoundTripWithoutSplittingOrChangingCharacters(string value)
        {
            Assert.That(ParseWindowsArguments(OpenIDEAArguments.Quote(value, true)), Is.EqualTo(new[] { value }));
        }

        [TestCase("$(ProjectPath) --file $(File) --line $(Line) --column $(Column)")]
        [TestCase("\"$(ProjectPath)\" --file \"$(File)\" --line $(Line) --column $(Column)")]
        [TestCase("$(ProjectPath) --file \"$(File)\" --line $(Line) --column $(Column)")]
        [TestCase("'$(ProjectPath)' --file '$(File)' --line '$(Line)' --column '$(Column)'")]
        public void ExistingTemplatesQuoteEachPathOnce(string template)
        {
            const string project = @"C:\Game With Spaces\";
            const string file = @"C:\Game With Spaces\Assets\角色.cs";
            var arguments = OpenIDEAArguments.Expand(template, project, file, 42, 7, true);
            Assert.That(ParseWindowsArguments(arguments), Is.EqualTo(new[] { project, "--file", file, "--line", "42", "--column", "7" }));
        }

        [Test]
        public void ValuesContainingPlaceholderNamesAreNotExpandedAgain()
        {
            const string project = @"C:\$(File)\My Game";
            const string file = @"C:\$(File)\My Game\$(Line).cs";
            var arguments = OpenIDEAArguments.Expand(null, project, file, 12, 3, true);
            Assert.That(ParseWindowsArguments(arguments), Is.EqualTo(new[] { project, "--file", file, "--line", "12", "--column", "3" }));
        }

        [TestCase(-1, -1, "0", "0")]
        [TestCase(5, -1, "5", "0")]
        [TestCase(0, 7, "0", "7")]
        [TestCase(-50, -9, "0", "0")]
        public void UnknownCoordinatesUseOpenIDEAZeroSentinel(int line, int column, string expectedLine, string expectedColumn)
        {
            var arguments = OpenIDEAArguments.Expand("--line $(Line) --column $(Column)", "/project", "/file.cs", line, column, false);
            Assert.That(arguments, Is.EqualTo("--line " + expectedLine + " --column " + expectedColumn));
        }

        [Test]
        public void UnixArgumentsPreserveApostrophesQuotesAndBackslashes()
        {
            Assert.That(OpenIDEAArguments.Quote("/home/user's game/a\"b\\c.cs", false),
                Is.EqualTo("\"/home/user's game/a\\\"b\\\\c.cs\""));
        }
    }
}
