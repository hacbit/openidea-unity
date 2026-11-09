# OpenIDEA Editor for Unity

Unity Editor integration for [OpenIDEA](https://github.com/l0ng-ai/openidea), maintained as the UPM package `com.hacbit.ide.openidea`.

The package discovers OpenIDEA installations, supports manually selected copies, opens projects and scripts at the requested line and column, and generates and synchronizes C# solutions and projects for OpenIDEA.

## Target

- Unity 2022.3 LTS.
- Editor-only package; the assembly is excluded from player builds.

## Select OpenIDEA

Open **Preferences > External Tools > External Script Editor** and select an OpenIDEA installation from the list. If your copy is not listed, choose **Browse…** and select its executable or macOS app bundle.

| Platform | Automatic discovery | Manual selection |
| --- | --- | --- |
| Windows | `%LOCALAPPDATA%/OpenIDEA`, Program Files, OpenIDEA installer registry entries, and `PATH` | `openidea.exe`, including a local `target/debug` or `target/release` build |
| macOS | `/Applications/OpenIDEA.app`, `~/Applications/OpenIDEA.app`, and `PATH` | `OpenIDEA.app` or the `openidea` executable |
| Linux | `/usr/bin`, `/usr/local/bin`, `/opt/openidea`, `~/.local/bin`, `~/Applications` AppImages, and `PATH` | `openidea` or an `OpenIDEA*.AppImage` release |

Unity saves the selected path. Custom locations remain in the list while their executable exists; multiple installations display their paths so they can be distinguished. Discovery checks the filesystem again when Unity requests the installation list.

Select the IDE itself, not its installer, uninstaller, or `openidea-server` helper. A macOS app bundle must contain `Contents/MacOS/openidea`.

## Open projects and scripts

After selecting OpenIDEA, double-click a script or a source location in the Console to open it. **Assets > Open C# Project** opens the Unity project directory without a file argument. OpenIDEA's single-instance handling receives requests when the IDE is already running.

In External Tools, **External Script Editor Args** controls file-opening arguments. The default is:

```text
$(ProjectPath) --file $(File) --line $(Line) --column $(Column)
```

Paths are quoted automatically, including spaces and non-ASCII characters. Previously saved templates with quotes around the placeholders are accepted. Line and column values are one-based; unknown positions are sent as `0`. Project-only opening sends just the project directory.

**Extensions handled** is a semicolon-separated list. Defaults include Unity's project-generation extensions and `cs`, `json`, `asmdef`, `asmref`, `rsp`, `log`, and `txt`. Other assets remain available to Unity's normal handlers. No `.cs` file association in Windows is required.

The selected OpenIDEA build must support the `--file`, `--line`, and `--column` arguments. If launching the editor fails, the package reports the error in the Unity Console.

## Generate C# project files

In **Preferences > External Tools**, select OpenIDEA and choose the package sources under **Generate .csproj files for**. Project files synchronize automatically while OpenIDEA is selected. **Regenerate project files** remains available for an immediate manual refresh when Unity is idle; the panel reports the number of projects generated and files updated.

Assets assemblies are always included. **Embedded packages** and **Local packages** are enabled by default. Registry, Git, built-in, local tarball, and unknown package sources can be enabled individually. These preferences belong to OpenIDEA and do not change another editor's generation settings. Changing an option schedules synchronization automatically.

Synchronization runs after script compilation, assembly reload, relevant file changes, package registration, and build-target changes. Consecutive notifications are combined, and generation waits until importing, compiling, and player builds have finished. On the first valid project/script open in a domain, pending synchronization runs before launching when Unity is idle. Later opens reuse the files, repairing missing solutions or projects when needed. If Unity is busy or generation fails, opening still proceeds; queued work resumes when Unity is idle, and failures are reported in the Console without retrying every frame.

The package writes `<project directory name>.sln` and one SDK-style `.csproj` per selected Unity assembly in the Unity project root. Source references, defines, language version, unsafe settings, analyzers, and response-file options come from Unity's Editor compilation graph. Excluded packages remain available through DLL references; include their source projects when you want source analysis and navigation. Missing compiled DLLs require Unity to compile those dependencies first.

OpenIDEA uses `csharp-ls` to load these files. A compatible .NET SDK and language server must be available in OpenIDEA. Generated projects use explicit Unity references and put design-time outputs under `Temp/OpenIDEA/`.

Generation replaces the standard solution and matching project files. Unchanged contents are not rewritten, so ordinary script-body edits do not change project-file timestamps. Previously generated projects that are no longer selected are removed from the solution but their `.csproj` files are retained. Player-only projects and AssetPostprocessor generation callbacks are not implemented yet.

## Local development

The repository root is the package root. Keep this checkout at:

```text
<Unity project>/Packages/com.hacbit.ide.openidea/
```

Unity imports it as an embedded package. Commit changes from this directory; the surrounding Unity project and its generated files are outside this repository.

```text
Editor/          Editor registration, discovery, launching, and project generation
Tests/Editor/    Discovery, launcher, generation, and Unity integration tests
Documentation~/  Package documentation
package.json     UPM package manifest
```

Commit Unity `.meta` files alongside their assets. See the [contributing guide](CONTRIBUTING.md) for enabling package tests.

## Git installation

Once the package has been pushed to the repository's default branch, use **Add package from git URL** in Unity's Package Manager:

```text
https://github.com/hacbit/openidea-unity.git
```

## Planned integration

- Support generation callbacks, Player configurations, and cleanup of obsolete generated projects.

See [the integration notes](Documentation~/index.md) and [contributing guide](CONTRIBUTING.md).

## License

Copyright (c) 2026 hacbit. Licensed under the [Apache License 2.0](LICENSE.md).
