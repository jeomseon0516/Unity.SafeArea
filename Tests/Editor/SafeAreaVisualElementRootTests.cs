using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using Jeomseon.Unity.SafeArea;
using Jeomseon.Unity.SafeArea.UIToolkit;

namespace Jeomseon.SafeArea.Tests
{
    public sealed class SafeAreaVisualElementRootTests
    {
        [Test]
        public void ApplyInsets_SetsAbsolutePositionAndInsetsOnFlaggedSides()
        {
            var target = new VisualElement();

            SafeAreaVisualElementRoot.ApplyInsets(
                target,
                insetLeft: 10f, insetRight: 20f, insetTop: 30f, insetBottom: 40f,
                useLeft: true, useRight: true, useTop: true, useBottom: true);

            Assert.AreEqual(Position.Absolute, target.style.position.value);
            Assert.AreEqual(10f, target.style.left.value.value);
            Assert.AreEqual(20f, target.style.right.value.value);
            Assert.AreEqual(30f, target.style.top.value.value);
            Assert.AreEqual(40f, target.style.bottom.value.value);
        }

        [Test]
        public void ApplyInsets_UnflaggedSideStaysZeroRegardlessOfInset()
        {
            var target = new VisualElement();

            SafeAreaVisualElementRoot.ApplyInsets(
                target,
                insetLeft: 999f, insetRight: 999f, insetTop: 999f, insetBottom: 999f,
                useLeft: false, useRight: false, useTop: true, useBottom: false);

            Assert.AreEqual(0f, target.style.left.value.value,
                "useLeft이 꺼져 있으면 inset과 무관하게 0(화면 가장자리)에 붙어야 함");
            Assert.AreEqual(0f, target.style.right.value.value);
            Assert.AreEqual(999f, target.style.top.value.value);
            Assert.AreEqual(0f, target.style.bottom.value.value);
        }

    }
}
