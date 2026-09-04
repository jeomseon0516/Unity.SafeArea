# Jeomseon Unity Safe Area

UPM package for applying `Screen.safeArea` to uGUI layouts, with runtime canvas patching and an editor preview.

## Requirements

- Unity 6000.6.0f1 or newer
- uGUI 2.6.0 or newer

## Install via OpenUPM

Register the OpenUPM scoped registry once in your project's `Packages/manifest.json`.

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

## Install via Git URL

Enter the following URL in Unity Package Manager's `Install package from git URL`.

```text
https://github.com/jeomseon0516/Unity.SafeArea.git#v0.4.0
```

Requires Unity 6000.6.0f1 or newer and uGUI 2.6.0 or newer. RectTransform fitting uses the official
`UnityEngine.UI.SafeArea`; this package retains additive `LayoutGroup` padding, UI Toolkit support, canvas
patching, and isolated editor preview features.

`Jeomseon/Safe Area/Patch Active Scene` opens a hierarchy TreeView instead of modifying the Scene immediately.
Canvas checkboxes describe the desired state, validation explains excluded targets, and `Apply Changes` batches
component additions and removals with Undo support. `Safe Area` and `Runtime Ignore` are independent controls:
the latter manages the `SafeAreaIgnore` marker respected by runtime patching only.

Migration from 0.3.x: replace `Jeomseon.Unity.SafeArea.SafeAreaRoot` with `UnityEngine.UI.SafeArea`. The former
per-edge booleans map to the official component's `Edges` flags.
