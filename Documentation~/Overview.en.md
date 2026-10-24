# Jeomseon Unity Safe Area

[한국어](Overview.md) · [Installation](../README.en.md)

## Purpose

Provides mobile safe-area padding plus scene patching and isolated preview tools. uGUI root resizing uses the official SafeArea component.

## Usage flow

For uGUI choose official SafeArea or SafeAreaPadding; for UI Toolkit attach a Root/Padding adapter to UIDocument. Explicitly enable runtime patching in Settings; Editor Patcher applies on Apply Changes.

## Contracts and limitations

Root shrinks the whole box; Padding shifts only content. SafeAreaIgnore opts out of runtime patching. Preview renders clones, not the source Scene. Automatic runtime Canvas mutation is disabled by default.

## Package requirements

- Package ID: `com.jeomseon.unity.safe-area`
- Version: `0.4.0`
- Unity: `6000.6` / `0f1`
- `com.unity.ugui`: `2.6.0`
- `com.unity.modules.ui`: `1.0.0`
- `com.unity.modules.uielements`: `1.0.0`

## Samples and verification

Import the following from Package Manager > Samples. Open the included Scene or Editor tool; follow any Setup step in its sample README.

- [Basic Usage](../Samples~/BasicUsage/README.md)
  - [SafeAreaBasicUsageSample](../Samples~/BasicUsage/SafeAreaBasicUsageSample.unity)
- [UI Toolkit Usage](../Samples~/UIToolkitUsage/README.md)
  - [SafeAreaUIToolkitSample](../Samples~/UIToolkitUsage/SafeAreaUIToolkitSample.unity)

[Test sources](../Tests)

Add the package ID to manifest `testables`, then run the relevant Unity Test Runner assemblies. These links describe available tests, not a new successful test run.

## Source map

- [SafeAreaIgnore](../Runtime/SafeAreaIgnore.cs)
- [SafeAreaPadding](../Runtime/SafeAreaPadding.cs)
- [SafeAreaRuntimeApplier](../Runtime/SafeAreaRuntimeApplier.cs)
- [SafeAreaSettings](../Runtime/SafeAreaSettings.cs)
- [SafeAreaUtility](../Runtime/SafeAreaUtility.cs)
- [SafeAreaWatcher](../Runtime/SafeAreaWatcher.cs)
- [SafeAreaVisualElementPadding](../Runtime/UIToolkit/SafeAreaVisualElementPadding.cs)
- [SafeAreaVisualElementRoot](../Runtime/UIToolkit/SafeAreaVisualElementRoot.cs)
- [SafeAreaPatcherWindow](../Editor/SafeAreaPatcherWindow.cs)
- [SafeAreaPreviewWindow](../Editor/SafeAreaPreviewWindow.cs)
- [SafeAreaScenePatcher](../Editor/SafeAreaScenePatcher.cs)
