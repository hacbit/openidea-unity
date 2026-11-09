# OpenIDEA Editor for Unity

Use [OpenIDEA](https://github.com/l0ng-ai/openidea) as your script editor in **Unity 2022.3 LTS**, with script navigation and automatic `.sln` / `.csproj` synchronization.

## Installation

In Unity's **Package Manager**, choose **+ > Add package from git URL** and enter:

```text
https://github.com/hacbit/openidea-unity.git
```

Install OpenIDEA separately. C# language features require a compatible .NET SDK and `csharp-ls` available in OpenIDEA.

## Usage

1. Open **Preferences > External Tools > External Script Editor** and select **OpenIDEA**. If it is not listed, choose **Browse…** and select its executable or macOS app bundle.
2. Double-click a script or a Console source location to open it in OpenIDEA.
3. Use **Assets > Open C# Project** to open the whole project.

Project files synchronize automatically after compilation and relevant project changes while OpenIDEA is selected. Generation waits for Unity to finish importing or compiling, and unchanged files are not rewritten.

## Settings

In **Preferences > External Tools**:

- **Generate .csproj files for** selects which package sources to include. Assets are always included; Embedded and Local packages are enabled by default. Changes apply automatically.
- **Regenerate project files** refreshes the solution and projects manually when Unity is idle.
- **External Script Editor Args** and **Extensions handled** customize file opening. The defaults are ready to use.

## Documentation

See the [integration notes](Documentation~/index.md) for technical details and current limitations, the [changelog](CHANGELOG.md) for release history, and the [contributing guide](CONTRIBUTING.md) for local development.

## License

Copyright (c) 2026 hacbit. Licensed under the [Apache License 2.0](LICENSE.md).
