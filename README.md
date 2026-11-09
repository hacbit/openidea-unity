# OpenIDEA Editor for Unity

Unity Editor integration for [OpenIDEA](https://github.com/l0ng-ai/openidea), maintained as the UPM package `com.hacbit.ide.openidea`.

The package discovers OpenIDEA installations, supports manually selected copies, and opens Unity projects and scripts at the requested line and column. Solution and project file generation are not implemented yet.

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

## Local development

The repository root is the package root. Keep this checkout at:

```text
<Unity project>/Packages/com.hacbit.ide.openidea/
```

Unity imports it as an embedded package. Commit changes from this directory; the surrounding Unity project and its generated files are outside this repository.

```text
Editor/          Editor registration, discovery, and launching
Tests/Editor/    Discovery, argument, launcher, and integration tests
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

- Generate and synchronize C# solutions and projects.

See [the integration notes](Documentation~/index.md) and [contributing guide](CONTRIBUTING.md).

## License

Copyright (c) 2026 hacbit. Licensed under the [Apache License 2.0](LICENSE.md).
