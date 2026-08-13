using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using Jeomseon.Unity.SafeArea;
using Jeomseon.Unity.SafeArea.UIToolkit;

namespace Jeomseon.SafeArea.Tests
{
    public sealed class SafeAreaVisualElementPaddingTests
    {
        [Test]
        public void ApplyInsets_OnlyAddsInsetOnFlaggedSides()
        {
            var target = new VisualElement();

            SafeAreaVisualElementPadding.ApplyInsets(
                target,
                insetLeft: 10f, insetRight: 20f, insetTop: 30f, insetBottom: 40f,
                useLeft: false, useRight: false, useTop: true, useBottom: false,
                basePaddingLeft: 0f, basePaddingRight: 0f, basePaddingTop: 0f, basePaddingBottom: 0f);

            Assert.AreEqual(0f, target.resolvedStyle.paddingLeft);
            Assert.AreEqual(0f, target.resolvedStyle.paddingRight);
            Assert.AreEqual(30f, target.resolvedStyle.paddingTop);
            Assert.AreEqual(0f, target.resolvedStyle.paddingBottom);
        }

        [Test]
        public void ApplyInsets_AddsInsetOnTopOfBasePadding()
        {
            var target = new VisualElement();

            SafeAreaVisualElementPadding.ApplyInsets(
                target,
                insetLeft: 0f, insetRight: 0f, insetTop: 30f, insetBottom: 0f,
                useLeft: false, useRight: false, useTop: true, useBottom: false,
                basePaddingLeft: 0f, basePaddingRight: 0f, basePaddingTop: 16f, basePaddingBottom: 0f);

            Assert.AreEqual(46f, target.resolvedStyle.paddingTop,
                "베이스 padding(16) + inset(30)이 합산되어야 함");
        }

        [Test]
        public void ApplyInsets_UnflaggedSideKeepsBasePaddingRegardlessOfInset()
        {
            var target = new VisualElement();

            SafeAreaVisualElementPadding.ApplyInsets(
                target,
                insetLeft: 0f, insetRight: 0f, insetTop: 999f, insetBottom: 0f,
                useLeft: false, useRight: false, useTop: false, useBottom: false,
                basePaddingLeft: 0f, basePaddingRight: 0f, basePaddingTop: 16f, basePaddingBottom: 0f);

            Assert.AreEqual(16f, target.resolvedStyle.paddingTop,
                "useTop이 꺼져 있으면 inset과 무관하게 베이스 padding만 유지해야 함");
        }

        [Test]
        public void MissingPanelSettings_DoesNotThrowOnEnableOrDisable()
        {
            var go = new GameObject("SafeAreaVisualElementPaddingTestTarget", typeof(UIDocument));
            try
            {
                var component = go.AddComponent<SafeAreaVisualElementPadding>();
                Assert.DoesNotThrow(() => component.enabled = false);
            }
            finally
            {
                Object.DestroyImmediate(go);
                SafeAreaWatcher.ResetStaticStateForNewSession();
            }
        }
    }
}
