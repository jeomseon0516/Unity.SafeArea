using Jeomseon.Unity.SafeArea.Editor;
using NUnit.Framework;
using UnityEngine;

namespace Jeomseon.SafeArea.Tests
{
    public sealed class SafeAreaPreviewWindowTests
    {
        [Test]
        public void CalculatePreviewSafeAreaRect_ConvertsBottomLeftOriginToEditorGuiCoordinates()
        {
            var previewRect = new Rect(10f, 20f, 540f, 960f);
            var screenSize = new Vector2(1080f, 1920f);
            var safeArea = new Rect(40f, 100f, 1000f, 1720f);

            var result = SafeAreaPreviewWindow.CalculatePreviewSafeAreaRect(
                previewRect,
                screenSize,
                safeArea);

            Assert.That(result.x, Is.EqualTo(30f).Within(0.001f));
            Assert.That(result.y, Is.EqualTo(70f).Within(0.001f));
            Assert.That(result.width, Is.EqualTo(500f).Within(0.001f));
            Assert.That(result.height, Is.EqualTo(860f).Within(0.001f));
        }

        [Test]
        public void CalculatePreviewSafeAreaRect_ClampsSafeAreaToScreenBounds()
        {
            var previewRect = new Rect(0f, 0f, 100f, 200f);
            var screenSize = new Vector2(1000f, 2000f);
            var safeArea = new Rect(-100f, -200f, 1300f, 2400f);

            var result = SafeAreaPreviewWindow.CalculatePreviewSafeAreaRect(
                previewRect,
                screenSize,
                safeArea);

            Assert.That(result, Is.EqualTo(previewRect));
        }
    }
}
