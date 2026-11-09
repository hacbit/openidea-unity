# OpenIDEA Editor integration

Package: `com.hacbit.ide.openidea`

Editor assembly: `OpenIDEA.Editor`

## Current state

`OpenIDEAScriptEditor` registers with Unity's `Unity.CodeEditor` API. `OpenIDEADiscovery` supplies installation candidates and validates paths selected through Unity's **External Script Editor > Browse…** control.

Discovery and project/script opening are implemented for Windows, macOS, and Linux. Manual solution/project generation is available through External Tools and `SyncAll`. `SyncIfNeeded` remains a no-op until automatic synchronization is implemented.

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
4. Generate `.sln` and `.csproj` files from Unity's compilation data (manual generation implemented; automatic synchronization planned).

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

## Manual project generation

`OpenIDEAScriptEditor.SyncAll` collects the current `CompilationPipeline.GetAssemblies(AssembliesType.Editor)` graph and calls the generator. The External Tools button uses this same entry point. Generation is disabled while Unity imports or compiles. It does not start OpenIDEA, request compilation, or register an asset-change listener.

The implementation is split between:

- `ProjectGenerationSettings`: OpenIDEA-specific package-source preferences, defaulting to Embedded and Local. Assets always participate. A selected source makes its entire assembly a source project, preserving compilation membership.
- `UnityProjectSnapshot`: Unity assembly data, compiler options, analyzers, additional files, and parsed response files.
- `ProjectPathResolver`: maps package asset paths to `PackageInfo.resolvedPath`, including package-cache and external local files.
- `ProjectCompilerOptions`: maps relevant additional and response-file arguments to design-time settings.
- `OpenIDEAProjectGeneration`: deterministic SDK project and solution output with stable assembly identities, explicit source lists, project references for included assemblies, and DLL references for excluded assemblies.

Projects use `netstandard2.1` as the design-time target framework with implicit framework references disabled. Unity's explicit reference assemblies determine the API surface; the installed .NET SDK does not choose the C# language version or replace Unity's references. SDK source globs, implicit usings, default .NET analyzers, and generated assembly/framework attributes are disabled. Unity/project analyzers are emitted explicitly. Intermediate and output paths are isolated per assembly under `Temp/OpenIDEA/`.

All files are rendered before writing. Contents are compared before replacement, each changed file is replaced through a neighboring temporary file, and the solution is written last. This is atomic per file, not a transaction across the complete solution. A failed collection or render preserves all existing files; an I/O failure is reported and can be retried.

The generator owns the standard root solution and matching assembly project filenames during generation. It does not delete unselected project files or preserve custom solution entries. The first step does not invoke AssetPostprocessor generation callbacks, create Player-only variants, or automatically regenerate when a script opens or compilation inputs change. Those behaviors belong to the subsequent synchronization work.

Validation should include loading the generated solution in OpenIDEA's actual `csharp-ls`, not just parsing XML. Pure generation tests cover graph references, option filtering, paths, escaping, repeatability, and failed renders. `UnityProjectGenerationIntegrationTests` checks the real imported package against Unity's compilation API and must run in the Unity Editor.
