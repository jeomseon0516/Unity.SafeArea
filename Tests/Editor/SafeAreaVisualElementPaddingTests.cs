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

            Assert.AreEqual(0f, target.style.paddingLeft.value.value);
            Assert.AreEqual(0f, target.style.paddingRight.value.value);
            Assert.AreEqual(30f, target.style.paddingTop.value.value);
            Assert.AreEqual(0f, target.style.paddingBottom.value.value);
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

            Assert.AreEqual(46f, target.style.paddingTop.value.value,
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

            Assert.AreEqual(16f, target.style.paddingTop.value.value,
                "useTop이 꺼져 있으면 inset과 무관하게 베이스 padding만 유지해야 함");
        }

    }
}
