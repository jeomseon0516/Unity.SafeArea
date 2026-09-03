using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Jeomseon.Unity.SafeArea.Editor
{
    internal sealed class SafeAreaPatchValidator
    {
        private readonly SafeAreaSettings _settings;

        public SafeAreaPatchValidator(SafeAreaSettings settings)
        {
            _settings = settings;
        }

        public SafeAreaPatchValidation Validate(Canvas canvas)
        {
            if (canvas == null || !canvas.gameObject.scene.IsValid() || !canvas.gameObject.scene.isLoaded)
            {
                return new SafeAreaPatchValidation(
                    SafeAreaPatchValidation.Status.InvalidScene,
                    "Canvas is not part of a loaded Scene.");
            }

            if (canvas.transform is not RectTransform)
            {
                return new SafeAreaPatchValidation(
                    SafeAreaPatchValidation.Status.MissingRectTransform,
                    "Canvas requires a RectTransform.");
            }

            if (_settings.SkipWorldSpaceCanvases && canvas.renderMode == RenderMode.WorldSpace)
            {
                return new SafeAreaPatchValidation(
                    SafeAreaPatchValidation.Status.WorldSpace,
                    "World Space Canvas is excluded by SafeAreaSettings.");
            }

            var ownedSafeAreas = FindOwnedSafeAreas(canvas).ToArray();
            if (ownedSafeAreas.Length > 1)
            {
                return new SafeAreaPatchValidation(
                    SafeAreaPatchValidation.Status.Ambiguous,
                    $"Canvas owns {ownedSafeAreas.Length} SafeArea components. Resolve duplicates manually.");
            }

            return ownedSafeAreas.Length == 1
                ? new SafeAreaPatchValidation(
                    SafeAreaPatchValidation.Status.Applied,
                    "SafeArea is already applied.",
                    ownedSafeAreas[0])
                : new SafeAreaPatchValidation(
                    SafeAreaPatchValidation.Status.Available,
                    $"Will add SafeArea under '{_settings.RootName}'.");
        }

        private static IEnumerable<UnityEngine.UI.SafeArea> FindOwnedSafeAreas(Canvas canvas)
        {
            return canvas
                .GetComponentsInChildren<UnityEngine.UI.SafeArea>(true)
                .Where(safeArea => safeArea.GetComponentInParent<Canvas>(true) == canvas);
        }
    }
}
