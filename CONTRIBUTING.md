# Contributing

Use `main` as the base branch and keep each change focused on one behavior or fix.

## Development

1. Check out this repository into a Unity 2022.3 project's `Packages/com.hacbit.ide.openidea` directory.
2. Keep integration code in `Editor/`, under the `OpenIDEA.Editor` namespace.
3. Commit Unity-generated `.meta` files with new assets. Preserve existing GUIDs when moving assets.
4. Follow `.editorconfig` and update the changelog for user-visible changes.

## Verification

- Import the package in Unity and check the Console for compilation and import errors.
- Add focused Editor tests when implementing behavior, especially argument handling and project generation.
- Exercise changed external-editor behavior with OpenIDEA and state the versions used.
- In pull requests, describe the resulting behavior and the verification performed. Identify checks that could not be run.

Keep third-party copyright and license notices when reusing code.

## Package tests

Install Unity Test Framework in the host project and add the package to the project's `Packages/manifest.json` testables list:

```json
"testables": ["com.hacbit.ide.openidea"]
```

Run `OpenIDEA.Editor.Tests` from the Test Runner's EditMode tab. The test assembly is Editor-only and uses `TestAssemblies`; the integration assembly does not reference NUnit.
