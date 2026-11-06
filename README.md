# OpenIDEA Editor for Unity

Unity Editor integration for [OpenIDEA](https://github.com/l0ng-ai/openidea), maintained as the UPM package `com.hacbit.ide.openidea`.

**Status: initial scaffold.** Editor discovery, script opening, and solution/project generation are not implemented yet. Installing this version does not register OpenIDEA as an external script editor.

## Target

- Unity 2022.3 LTS.
- Editor-only package; the assembly is excluded from player builds.

## Local development

The repository root is the package root. Keep this checkout at:

```text
<Unity project>/Packages/com.hacbit.ide.openidea/
```

Unity imports it as an embedded package. Commit changes from this directory; the surrounding Unity project and its generated files are outside this repository.

```text
Editor/          Editor assembly and future integration code
Documentation~/  Package documentation
package.json     UPM package manifest
```

Commit Unity `.meta` files alongside their assets. Add an Editor test assembly when introducing testable integration behavior.

## Git installation

Once the package has been pushed to the repository's default branch, use **Add package from git URL** in Unity's Package Manager:

```text
https://github.com/hacbit/openidea-unity.git
```

## Planned integration

- Register OpenIDEA in Unity's external script editor settings.
- Discover OpenIDEA installations.
- Open projects and scripts at the requested line and column.
- Generate and synchronize C# solutions and projects.

See [the integration notes](Documentation~/index.md) and [contributing guide](CONTRIBUTING.md).

## License

Copyright (c) 2026 hacbit. Licensed under the [Apache License 2.0](LICENSE.md).

The starting directory was copied from Unity's Rider editor package. Its implementation has been removed from this scaffold; the original MIT license notices are preserved in [Third Party Notices](Third%20Party%20Notices.md).
