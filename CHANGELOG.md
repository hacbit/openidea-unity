# Changelog

## [Unreleased]

## [0.1.0] - 2026-11-09

Initial OpenIDEA integration for Unity 2022.3 LTS, distributed as the Editor-only UPM package `com.hacbit.ide.openidea` under Apache-2.0.

### IDE discovery and opening

- Discover OpenIDEA installations on Windows, macOS, and Linux, and support manually selected executables or macOS app bundles, including development builds.
- Open Unity projects, scripts, and Console source locations in the selected installation, with file, line, and column arguments supported by OpenIDEA's single-instance handling.
- Provide configurable launch arguments and handled file extensions in Preferences > External Tools. Quote paths automatically and open scripts directly without requiring an operating-system `.cs` file association.
- Report launch failures in the Unity Console.

### Project generation

- Generate a solution and SDK-style C# projects from Unity's Editor compilation graph, preserving source membership, assembly references, defines, compiler options, analyzers, and response-file settings.
- Add independent **Generate .csproj files for** options for Embedded, Local, Registry, Git, built-in, local tarball, and unknown package sources. Assets are always included; Embedded and Local packages are enabled by default. Excluded package assemblies remain available through DLL references.
- Add **Regenerate project files** in External Tools for manual generation.
- Keep project identities and output deterministic, preserve timestamps when contents are unchanged, and isolate design-time outputs under `Temp/OpenIDEA/`.

### Automatic synchronization

- Synchronize after selecting OpenIDEA, script compilation, assembly reload, relevant asset and package changes, build-target changes, and package-source option changes.
- Run automatic synchronization only while OpenIDEA is the selected external editor. Combine consecutive requests and defer generation during imports, compilation, and player builds.
- Synchronize before opening when needed and restore missing solution or project files. Busy or failed generation still allows the launcher to attempt to open the requested project or script.
- Report generation failures without retrying every frame; retry on another relevant change, manual regeneration, or open request.

### Known limitations

- Player-only project variants and AssetPostprocessor project-generation callbacks are not supported yet.
- Projects that are no longer selected are removed from the solution, but their `.csproj` files are retained on disk. Generation replaces the standard solution and matching project files without preserving custom solution entries.
