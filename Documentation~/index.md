# OpenIDEA Editor integration

Package: `com.hacbit.ide.openidea`

Editor assembly: `OpenIDEA.Editor`

## Current state

`OpenIDEAScriptEditor` registers with Unity's `Unity.CodeEditor` API. `OpenIDEADiscovery` supplies installation candidates and validates paths selected through Unity's **External Script Editor > Browse…** control.

Discovery is available on Windows, macOS, and Linux. File opening and project generation are not implemented yet: opening reports that it was not handled, and synchronization callbacks do no work. Argument controls are disabled until file opening is implemented.

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
2. Open a Unity project in OpenIDEA (planned).
3. Open a script at a requested source position (planned).
4. Generate and synchronize `.sln` and `.csproj` files from Unity's compilation data (planned).

All integration code belongs in the Editor assembly. Add tests alongside each implemented behavior.

## OpenIDEA launch arguments

The OpenIDEA source inspected when creating this scaffold accepts a positional project directory and optional file and source coordinates:

```text
openidea "<project directory>" --file "<script path>" --line 42 --column 7
```

- A relative `--file` path is resolved against the project directory.
- Line and column arguments are one-based.
- A coordinate of `0` or `-1` means the position is unknown.
- The project directory and file must exist.

Verify the contract against the targeted OpenIDEA version when implementing the launcher. The relevant OpenIDEA sources are `src-tauri/src/windows/cli.rs` and `src-tauri/src/windows.rs`.
