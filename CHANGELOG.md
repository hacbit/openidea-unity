# Changelog

## [Unreleased]

- Establish the `com.hacbit.ide.openidea` package scaffold for Unity 2022.3.
- Add an Editor-only assembly, package documentation, and repository configuration.
- Adopt the repository's Apache License 2.0 for this package.
- Register OpenIDEA as an external editor and discover installations on Windows, macOS, and Linux.
- Recognize manually selected editor paths, including development builds, and retain valid custom selections in the installation list.
- Add Editor tests for discovery, invalid paths, installation changes, and Unity's editor registration.
- Open projects and scripts in the selected OpenIDEA installation, including Console line and column locations.
- Quote paths for process arguments, accept previously saved quoted templates, and omit file arguments when opening only the project.
- Add configurable handled extensions, launch-error reporting, and tests for Windows argument round-trips and platform launch commands.
- Add manual solution and SDK-style C# project generation from Unity's Editor compilation graph.
- Add a Regenerate project files button and independent package-source options in External Tools, with Embedded and Local packages enabled by default.
- Preserve Unity references, defines, compiler settings, analyzers, and response-file inputs; map package paths to their physical locations and keep excluded package dependencies as DLL references.
- Keep generated output deterministic, avoid rewriting unchanged files, and isolate design-time outputs under `Temp/OpenIDEA/`.
- Automatically synchronize after compilation, assembly reload, relevant asset and package changes, build-target changes, and package-source option changes while OpenIDEA is selected.
- Combine consecutive requests and defer generation during imports, compilation, and player builds; preserve the manual regeneration button.
- Synchronize before valid open requests when needed, repair missing generated files, and continue launching even if generation is busy or fails.
- Add synchronization tests for deferral, editor selection, file-change filtering, failure recovery, and unchanged output timestamps.

Generation callbacks, Player-only projects, and obsolete project cleanup are not implemented yet.
