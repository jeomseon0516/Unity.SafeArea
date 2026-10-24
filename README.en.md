# Jeomseon Unity Safe Area

[한국어](README.md) | English

Provides mobile safe-area padding plus scene patching and isolated preview tools. uGUI root resizing uses the official SafeArea component.

## Quick start

For uGUI choose official SafeArea or SafeAreaPadding; for UI Toolkit attach a Root/Padding adapter to UIDocument. Explicitly enable runtime patching in Settings; Editor Patcher applies on Apply Changes.

## Contracts and limitations

Root shrinks the whole box; Padding shifts only content. SafeAreaIgnore opts out of runtime patching. Preview renders clones, not the source Scene. Automatic runtime Canvas mutation is disabled by default.

## Requirements

- Unity `6000.6.0f1`
- Package: `com.jeomseon.unity.safe-area` `0.4.0`
- `com.unity.ugui`: `2.6.0`
- `com.unity.modules.ui`: `1.0.0`
- `com.unity.modules.uielements`: `1.0.0`

## Installation

Merge this into your project's `Packages/manifest.json`; preserve existing dependencies and registries.

```json
{
  "scopedRegistries": [
    {
      "name": "OpenUPM",
      "url": "https://package.openupm.com",
      "scopes": [
        "com.jeomseon"
      ]
    }
  ],
  "dependencies": {
    "com.jeomseon.unity.safe-area": "0.4.0"
  }
}
```

Or use **Install package from git URL** in Package Manager:

```text
https://github.com/jeomseon0516/Unity.SafeArea.git#v0.4.0
```

## Samples

Import the desired entry from **Package Manager > Samples**. Use the included Scene/Editor tool and complete its Setup step first, if required.

- [Basic Usage](Samples~/BasicUsage/README.md)
  - [SafeAreaBasicUsageSample](Samples~/BasicUsage/SafeAreaBasicUsageSample.unity)
- [UI Toolkit Usage](Samples~/UIToolkitUsage/README.md)
  - [SafeAreaUIToolkitSample](Samples~/UIToolkitUsage/SafeAreaUIToolkitSample.unity)

## Tests

[Test sources](Tests)

Add the package ID to manifest `testables`, then execute its relevant EditMode/PlayMode assemblies in Unity Test Runner.

## Further reading

[Complete source map and package metadata](Documentation~/Overview.en.md)
- [Changelog](CHANGELOG.md)

## License

[License](LICENSE.md)
