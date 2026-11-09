# OpenIDEA Editor integration

Package: `com.hacbit.ide.openidea`

Editor assembly: `OpenIDEA.Editor`

## Current state

`OpenIDEAScriptEditor` registers with Unity's `Unity.CodeEditor` API. `OpenIDEADiscovery` supplies installation candidates and validates paths selected through Unity's **External Script Editor > Browse…** control.

Discovery and project/script opening are implemented for Windows, macOS, and Linux. Solution and project file generation are not implemented yet; synchronization callbacks do no work.

## Discovery

- Windows uses Tauri's `%LOCALAPPDATA%/OpenIDEA/openidea.exe` installation layout, Program Files, the OpenIDEA NSIS uninstall entry's `InstallLocation`, and `App Paths/openidea.exe`. Both user and machine registry entries and both registry views are checked.
- macOS checks the system and user Applications directories. A recognized `OpenIDEA.app` must contain `Contents/MacOS/openidea`.
- Linux checks common executable locations and OpenIDEA AppImages immediately inside `~/Applications`.
- Each platform also checks `PATH`. Discovery does not recursively scan disks or start candidate programs.
- A valid path currently selected in Unity is included even when it is outside the automatic locations. The same validation is used for automatic and manual paths.
- Paths must be absolute and exist. Normalized duplicates are removed, with case-insensitive comparison on Windows. The list is rebuilt on each request so removed or newly installed copies are reflected.

The Windows executable name, macOS bundle name, and installation layout follow OpenIDEA's `mainBinaryName: openidea` and `productName: OpenIDEA` in its Tauri configuration. Installer and helper executables are not accepted as IDEs.

## Unity-side responsibilities

The adapter implements Unity's `Unity.CodeEditor.IExternalCodeEditor` interface. Its responsibilities are:

1. Identify and discover OpenIDEA installations (implemented).
2. Open a Unity project in OpenIDEA (implemented).
3. Open a script at a requested source position (implemented).
4. Generate and synchronize `.sln` and `.csproj` files from Unity's compilation data (planned).

All integration code belongs in the Editor assembly. Add tests alongside each implemented behavior.

## OpenIDEA launch arguments

`OpenIDEAScriptEditor.OpenProject` passes the current Unity editor selection and project root to `OpenIDEALauncher`. The launcher validates the executable and target file, expands the argument template, and starts the chosen executable directly with the project as its working directory. macOS bundles use `/usr/bin/open -n -a <bundle> --args ...` so a second launch can forward arguments through OpenIDEA's single-instance handling.

`OpenIDEAArguments` performs platform-specific quoting without invoking a shell. Placeholder replacement runs once, so text such as `$(Line)` inside a real filename stays literal. Existing quotes around individual placeholders are normalized before replacement.

An empty target or the project directory opens only the project. Script paths may be relative to the Unity project or absolute, including paths to external packages. Unhandled extensions and missing files return `false` without launching. Process-start failures are reported to the Console; a successfully started process returns `true`, preventing fallback to the operating system's file association. This acknowledges process launch, not an application-level file-open response.

The OpenIDEA source inspected when creating this scaffold accepts a positional project directory and optional file and source coordinates:

```text
openidea "<project directory>" --file "<script path>" --line 42 --column 7
```

- A relative `--file` path is resolved against the project directory.
- Line and column arguments are one-based.
- A coordinate of `0` or `-1` means the position is unknown.
- The project directory and file must exist.

The argument template is used for file requests. Project-only requests pass the quoted project directory alone. Supported file extensions are configurable in External Tools, independently of the argument template.

When updating launch behavior, verify the contract against the targeted OpenIDEA version. The relevant OpenIDEA sources are `src-tauri/src/windows/cli.rs` and `src-tauri/src/windows.rs`.
