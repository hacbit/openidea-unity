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

Solution and project file generation are not implemented yet.
