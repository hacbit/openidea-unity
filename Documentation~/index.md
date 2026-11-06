# OpenIDEA Editor integration

Package: `com.hacbit.ide.openidea`

Editor assembly: `Hacbit.OpenIDEA.Editor`

## Current state

This package contains the initial Editor assembly and package metadata. It does not yet register an external editor or generate project files.

## Unity-side responsibilities

The first implementation should use Unity's `Unity.CodeEditor.IExternalCodeEditor` interface to:

1. Identify and discover OpenIDEA installations.
2. Open a Unity project in OpenIDEA.
3. Open a script at a requested source position.
4. Generate and synchronize `.sln` and `.csproj` files from Unity's compilation data.

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
