using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace OpenIDEA.Editor
{
    internal static class UnityProjectSnapshot
    {
        internal static ProjectAssembly[] Collect(string root, ProjectGenerationSources selected)
        {
            var paths = new ProjectPathResolver(root, PackageInfo.GetAllRegisteredPackages().Select(p => new ProjectPackage
            {
                AssetPath = p.assetPath,
                ResolvedPath = p.resolvedPath,
                Source = p.source
            }).ToArray());

            return CompilationPipeline.GetAssemblies(AssembliesType.Editor).Select(assembly =>
            {
                var options = assembly.compilerOptions;
                var model = new ProjectAssembly
                {
                    Name = assembly.name,
                    OutputPath = paths.Resolve(assembly.outputPath),
                    RootNamespace = string.IsNullOrEmpty(assembly.rootNamespace) ? EditorSettings.projectGenerationRootNamespace : assembly.rootNamespace,
                    LanguageVersion = options.LanguageVersion,
                    AllowUnsafe = options.AllowUnsafeCode,
                    Optimize = options.CodeOptimization == CodeOptimization.Release,
                    GenerateProject = assembly.sourceFiles.Any(file => ProjectGenerationSettings.Includes(selected, paths.SourceOf(file))),
                    Ruleset = OptionalPath(options.RoslynAnalyzerRulesetPath, paths),
                    AnalyzerConfig = OptionalPath(options.AnalyzerConfigPath, paths)
                };
                model.Sources.AddRange(assembly.sourceFiles.Select(paths.Resolve));
                model.Defines.AddRange(assembly.defines);
                model.AssemblyReferences.AddRange(assembly.assemblyReferences.Select(a => a.name));
                model.BinaryReferences.AddRange(assembly.compiledAssemblyReferences.Select(p => new ProjectBinaryReference(paths.Resolve(p))));
                model.Analyzers.AddRange((options.RoslynAnalyzerDllPaths ?? Array.Empty<string>()).Select(paths.Resolve));
                model.AdditionalFiles.AddRange((options.RoslynAdditionalFilePaths ?? Array.Empty<string>()).Select(paths.Resolve));
                if (PlayerSettings.suppressCommonWarnings)
                    model.NoWarn.AddRange(new[] { "0169", "0649" });

                foreach (var response in options.ResponseFiles ?? Array.Empty<string>())
                {
                    var data = CompilationPipeline.ParseResponseFile(paths.Resolve(response), root,
                        CompilationPipeline.GetSystemAssemblyDirectories(options.ApiCompatibilityLevel));
                    if (data.Errors != null && data.Errors.Length != 0)
                        throw new InvalidDataException(response + ": " + string.Join("; ", data.Errors));
                    model.Defines.AddRange(data.Defines ?? Array.Empty<string>());
                    model.BinaryReferences.AddRange((data.FullPathReferences ?? Array.Empty<string>()).Select(p => new ProjectBinaryReference(paths.Resolve(p))));
                    model.AllowUnsafe |= data.Unsafe;
                    ProjectCompilerOptions.Apply(model, data.OtherArguments, paths);
                }
                ProjectCompilerOptions.Apply(model, options.AdditionalCompilerArguments, paths);
                return model;
            }).ToArray();
        }

        private static string OptionalPath(string value, ProjectPathResolver paths) =>
            string.IsNullOrEmpty(value) ? null : paths.Resolve(value);
    }
}
